using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class MeleeHitMessage
{
    public readonly GameObject attacker;
    public readonly GameObject target;
    public readonly Vector3 point;
    public readonly Vector3 direction;
    public readonly float damage;
    public readonly float stunDuration;

    public MeleeHitMessage(GameObject attacker, GameObject target, Vector3 point, Vector3 direction, float damage, float stunDuration)
    {
        this.attacker = attacker;
        this.target = target;
        this.point = point;
        this.direction = direction;
        this.damage = damage;
        this.stunDuration = stunDuration;
    }
}

public class PlayerMeleeCombat : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Key meleeKey = Key.F;
    [SerializeField] private float cooldown = 0.8f;

    [Header("Aim")]
    [SerializeField] private Camera aimCamera;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private float range = 2.2f;
    [SerializeField] private float radius = 0.45f;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Hit")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float knockbackDistance = 1.6f;
    [SerializeField] private float knockbackDuration = 0.18f;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private float upwardForce = 1.5f;
    [SerializeField] private float stunDuration = 1.2f;
    [SerializeField] private float navMeshSampleRadius = 2f;

    [Header("Attack State")]
    [SerializeField] private float attackLockDuration = 0.35f;
    [SerializeField] private bool disableWeaponsDuringAttack = true;

    [Header("Enemy Stun Animation")]
    [SerializeField] private string stunStateName = "Stun";
    [SerializeField] private string stunTriggerName = "Stun";
    [SerializeField] private int animatorLayer = 0;
    [SerializeField] private bool disableRootMotionDuringStun = true;

    [Header("Enemy Scripts To Stun")]
    [SerializeField] private string[] stunBehaviourTypeNames =
    {
        "MutantAI",
        "RoboDroneAI",
        "SoldierRangedAI",
        "EnemyShoot"
    };

    private readonly Dictionary<GameObject, Coroutine> activeStuns = new Dictionary<GameObject, Coroutine>();
    private readonly List<Behaviour> temporarilyDisabledWeapons = new List<Behaviour>();

    private float nextAttackTime;

    private void Awake()
    {
        if (aimCamera == null)
            aimCamera = GetComponentInChildren<Camera>();

        if (aimCamera == null)
            aimCamera = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        KeyControl keyControl = Keyboard.current[meleeKey];
        if (keyControl != null && keyControl.wasPressedThisFrame)
            TryAttack();
    }

    public void TryAttack()
    {
        if (Time.time < nextAttackTime || aimCamera == null)
            return;

        nextAttackTime = Time.time + cooldown;
        gameObject.SendMessage("PlayerMeleeAttackStarted", SendMessageOptions.DontRequireReceiver);

        if (disableWeaponsDuringAttack)
            StartCoroutine(DisableWeaponsBriefly());

        if (TryFindTarget(out RaycastHit hit, out Health health))
            ApplyHit(hit, health);
    }

    private bool TryFindTarget(out RaycastHit bestHit, out Health bestHealth)
    {
        bestHit = default;
        bestHealth = null;

        Vector3 origin = aimCamera.transform.position;
        Vector3 direction = aimCamera.transform.forward;
        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction, range, hitMask, triggerInteraction);
        if (hits == null || hits.Length == 0)
            return false;

        Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || hitCollider.transform.root == transform.root)
                continue;

            Health health = hitCollider.GetComponentInParent<Health>();
            if (health == null || health.transform.root == transform.root || health.GetHealth() <= 0)
                continue;

            bestHit = hits[i];
            bestHealth = health;
            return true;
        }

        return false;
    }

    private void ApplyHit(RaycastHit hit, Health health)
    {
        GameObject target = health.gameObject;
        Vector3 direction = GetKnockbackDirection(target.transform.position);

        health.Change(-damage);

        MeleeHitMessage message = new MeleeHitMessage(gameObject, target, hit.point, direction, damage, stunDuration);
        target.SendMessage("PlayerMeleeHit", message, SendMessageOptions.DontRequireReceiver);

        Rigidbody targetRigidbody = health.GetComponentInParent<Rigidbody>();
        NavMeshAgent targetAgent = health.GetComponentInParent<NavMeshAgent>();

        if (targetRigidbody != null && !targetRigidbody.isKinematic)
            targetRigidbody.AddForce(direction * knockbackForce + Vector3.up * upwardForce, ForceMode.Impulse);

        GameObject targetRoot = health.transform.root.gameObject;
        if (activeStuns.TryGetValue(targetRoot, out Coroutine activeStun))
            StopCoroutine(activeStun);

        activeStuns[targetRoot] = StartCoroutine(StunAndMoveTarget(targetRoot, health, targetAgent, targetRigidbody, direction));
    }

    private Vector3 GetKnockbackDirection(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            direction = aimCamera != null ? aimCamera.transform.forward : transform.forward;

        direction.y = 0f;
        return direction.normalized;
    }

    private IEnumerator StunAndMoveTarget(GameObject targetRoot, Health health, NavMeshAgent agent, Rigidbody targetRigidbody, Vector3 direction)
    {
        MonoBehaviour[] disabledBehaviours = DisableEnemyBehaviours(targetRoot);
        AnimatorRootMotionState[] rootMotionStates = PlayStunAnimation(targetRoot);

        bool hadAgent = agent != null && agent.enabled;
        bool oldAgentStopped = false;
        bool oldUpdatePosition = false;
        bool oldUpdateRotation = false;

        if (hadAgent)
        {
            oldAgentStopped = agent.isStopped;
            oldUpdatePosition = agent.updatePosition;
            oldUpdateRotation = agent.updateRotation;
            agent.isStopped = true;
            agent.ResetPath();
            agent.updatePosition = false;
            agent.updateRotation = false;
        }

        if (targetRigidbody == null || targetRigidbody.isKinematic)
            yield return MoveTransformKnockback(targetRoot.transform, agent, direction);

        float remainingStun = Mathf.Max(0f, stunDuration - knockbackDuration);
        if (remainingStun > 0f)
            yield return new WaitForSeconds(remainingStun);

        bool targetIsAlive = health != null && health.GetHealth() > 0;

        if (targetIsAlive && hadAgent && agent != null && agent.enabled)
        {
            Vector3 position = targetRoot.transform.position;
            if (NavMesh.SamplePosition(position, out NavMeshHit navHit, navMeshSampleRadius, agent.areaMask))
                agent.Warp(navHit.position);

            agent.updatePosition = oldUpdatePosition;
            agent.updateRotation = oldUpdateRotation;
            agent.isStopped = oldAgentStopped;
        }

        if (targetIsAlive)
        {
            RestoreRootMotion(rootMotionStates);
            EnableEnemyBehaviours(disabledBehaviours);
        }

        activeStuns.Remove(targetRoot);
    }

    private AnimatorRootMotionState[] PlayStunAnimation(GameObject targetRoot)
    {
        Animator[] animators = targetRoot.GetComponentsInChildren<Animator>(true);
        List<AnimatorRootMotionState> rootMotionStates = new List<AnimatorRootMotionState>();

        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null)
                continue;

            if (disableRootMotionDuringStun)
            {
                rootMotionStates.Add(new AnimatorRootMotionState(animator, animator.applyRootMotion));
                animator.applyRootMotion = false;
            }

            if (HasAnimatorTrigger(animator, stunTriggerName))
                animator.SetTrigger(stunTriggerName);
            else
                PlayAnimatorStateIfExists(animator, stunStateName);
        }

        return rootMotionStates.ToArray();
    }

    private bool HasAnimatorTrigger(Animator animator, string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == triggerName && parameters[i].type == AnimatorControllerParameterType.Trigger)
                return true;
        }

        return false;
    }

    private bool PlayAnimatorStateIfExists(Animator animator, string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(animatorLayer, stateHash))
            return false;

        animator.Play(stateHash, animatorLayer, 0f);
        return true;
    }

    private void RestoreRootMotion(AnimatorRootMotionState[] rootMotionStates)
    {
        if (rootMotionStates == null)
            return;

        for (int i = 0; i < rootMotionStates.Length; i++)
        {
            if (rootMotionStates[i].animator != null)
                rootMotionStates[i].animator.applyRootMotion = rootMotionStates[i].applyRootMotion;
        }
    }

    private IEnumerator MoveTransformKnockback(Transform target, NavMeshAgent agent, Vector3 direction)
    {
        float duration = Mathf.Max(0.01f, knockbackDuration);
        Vector3 start = target.position;
        Vector3 end = start + direction * knockbackDistance;

        if (agent != null && NavMesh.SamplePosition(end, out NavMeshHit navHit, navMeshSampleRadius, agent.areaMask))
            end = navHit.position;

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        target.position = end;
    }

    private MonoBehaviour[] DisableEnemyBehaviours(GameObject targetRoot)
    {
        MonoBehaviour[] behaviours = targetRoot.GetComponentsInChildren<MonoBehaviour>();
        List<MonoBehaviour> disabledBehaviours = new List<MonoBehaviour>();

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled)
                continue;

            if (!ShouldStunBehaviour(behaviour.GetType().Name))
                continue;

            behaviour.enabled = false;
            disabledBehaviours.Add(behaviour);
        }

        return disabledBehaviours.ToArray();
    }

    private bool ShouldStunBehaviour(string behaviourTypeName)
    {
        if (stunBehaviourTypeNames == null || string.IsNullOrWhiteSpace(behaviourTypeName))
            return false;

        for (int i = 0; i < stunBehaviourTypeNames.Length; i++)
        {
            if (behaviourTypeName == stunBehaviourTypeNames[i])
                return true;
        }

        return false;
    }

    private void EnableEnemyBehaviours(MonoBehaviour[] behaviours)
    {
        if (behaviours == null)
            return;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] != null)
                behaviours[i].enabled = true;
        }
    }

    private IEnumerator DisableWeaponsBriefly()
    {
        temporarilyDisabledWeapons.Clear();
        DisableWeaponBehaviours(GetComponentsInChildren<FpsWeaponController>(true));
        DisableWeaponBehaviours(GetComponentsInChildren<Shoot>(true));

        yield return new WaitForSeconds(attackLockDuration);

        for (int i = 0; i < temporarilyDisabledWeapons.Count; i++)
        {
            if (temporarilyDisabledWeapons[i] != null)
                temporarilyDisabledWeapons[i].enabled = true;
        }

        temporarilyDisabledWeapons.Clear();
    }

    private void DisableWeaponBehaviours(Behaviour[] behaviours)
    {
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] == null || !behaviours[i].enabled)
                continue;

            behaviours[i].enabled = false;
            temporarilyDisabledWeapons.Add(behaviours[i]);
        }
    }

    private readonly struct AnimatorRootMotionState
    {
        public readonly Animator animator;
        public readonly bool applyRootMotion;

        public AnimatorRootMotionState(Animator animator, bool applyRootMotion)
        {
            this.animator = animator;
            this.applyRootMotion = applyRootMotion;
        }
    }
}
