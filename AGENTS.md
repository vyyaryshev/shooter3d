# AGENTS.md

## Project

- Unity learning project: `shooter3d`.
- Unity Editor version: `6000.2.1f1`.
- Main gameplay scripts are in `Assets/Scripts`.
- The project uses URP, Unity Input System, AI Navigation, UGUI, Timeline, and Visual Scripting packages.

## Working Rules

- Communicate with the user in Russian unless they ask for another language.
- Before editing code, inspect the relevant scene, prefab, script, and serialized references when possible.
- Keep changes small and explain the reason for non-obvious Unity decisions.
- Treat this as a learning project: when fixing bugs or writing scripts, briefly explain the important Unity concepts involved.
- Do not overwrite unrelated scene, prefab, material, or ProjectSettings changes.
- The working tree may already contain user changes. Never revert them unless the user explicitly asks.
- After completing any task, create a git commit that contains only the files changed for that task.
- Before committing, check `git status --short` and stage only the intended files.

## Unity Practices

- Prefer simple, readable `MonoBehaviour` scripts over premature abstractions.
- Keep serialized fields private with `[SerializeField]` where practical, instead of making fields public only for Inspector access.
- Validate required references in `Awake`, `Start`, or `OnValidate` when a missing Inspector reference would break gameplay.
- Use `TryGetComponent` for optional component lookups.
- Prefer `CompareTag` over direct tag string comparisons in runtime collision/trigger code.
- Use `Time.deltaTime` for frame-rate-independent movement, timers, and cooldowns.
- Avoid expensive repeated lookups such as `FindObjectOfType`, `GameObject.Find`, and `Camera.main` inside `Update`.
- Keep physics work in `FixedUpdate` when using Rigidbody movement or force-based logic.
- When adding new assets or scripts in Unity, include the `.meta` files in commits.

## Testing and Verification

- For script changes, run a compile check through Unity when feasible.
- If Unity cannot be run from the terminal, state that explicitly in the final response.
- For gameplay changes, describe the manual scene test that should be performed in the Editor.
- Watch for common Unity issues: missing serialized references, tag/layer mismatches, prefab overrides, NavMesh data changes, and scene objects renamed outside scripts.

## Git

- Use focused commits with short messages, for example `Add enemy health script` or `Fix bullet collision damage`.
- Do not include generated IDE files, `Library`, `Temp`, `Obj`, `Logs`, or unrelated imported asset changes.
- If the repo is dirty before starting, preserve that state and commit only the new task changes.
