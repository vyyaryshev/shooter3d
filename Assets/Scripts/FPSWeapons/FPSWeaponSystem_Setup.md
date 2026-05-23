# FPS Weapon System Setup

## Hierarchy

Player
- Main Camera
- Weapon Camera
- WeaponRoot
  - Weapon Model
  - MuzzlePoint
  - ShellEjectionPoint

## Cameras

- Put all weapon meshes on the `Weapon` layer.
- Main Camera must exclude `Weapon`.
- Weapon Camera must render only `Weapon`.
- Weapon Camera near clip plane: `0.01`.
- Weapon Camera depth must be higher than Main Camera.
- In URP, use camera stacking if your renderer setup requires it.

## Weapon Components

Add to the weapon object:
- `FpsWeaponController`
- `WeaponRecoil`
- `WeaponImpactSystem`
- `ShellEjector`

Optional:
- `FpsCameraShake` on camera/root.
- `WeaponCameraSetup` for one-click layer/camera setup.

## Surface Effects

Create `Surface Effect Profile` from:
`Create > FPS Weapons > Surface Effect Profile`

Recommended mappings:
- Metal: sparks particles, scorch decal.
- Concrete: dust/debris particles, bullet hole decal.
- Flesh: blood particles, blood decal.
- Mechanical: sparks + smoke particles.

Prefer Physic Materials for stable surface detection. Tags are supported as fallback.

## Best Practices

- Use pooled particle prefabs with short lifetimes.
- Keep muzzle flash unparented only if it needs world-space simulation.
- Use small tracer lifetime values: `0.04-0.1`.
- Keep decals simple and limit lifetime to avoid scene clutter.
- Do not render weapon meshes on the world camera.
- Keep weapon colliders disabled or on a layer ignored by gameplay raycasts.
