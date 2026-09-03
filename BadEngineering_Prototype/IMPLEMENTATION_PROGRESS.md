# Specification implementation progress

This file is the compact resume point for `BadEngineering_SpecificationDiagram.md` work.

## Source of truth

- `AGENTS.md`
- `BadEngineering_SpecificationDiagram.md`
- Branch: `codex/specification-implementation`

## Current plan

1. Weapon owner/host/state model, slots, pickup/drop/attach and recoil.
2. Rigidbody vehicle, driver/crew stations and player restrictions.
3. Prototype scene builder containing an end-to-end test space.
4. Compile/static validation, fixes, documentation and commits.

## Guardrails

- Specification wins over existing prototype behavior.
- Keep the prototype simple and Rigidbody-driven.
- Do not touch `Assets/_Recovery` unless explicitly required.
- Do not push commits.

## Completed

- Repository and specification inspected.
- Work branch created and Git workflow verified.
- Added `IWeaponHost`/`WeaponHost` shared host abstraction.
- Reworked Weapon around independent Owner, Host and Held/Attached/Dropped state.
- Added owned-slot pickup/drop/select behavior and host-aware recoil.
- Added arbitrary hit-position vehicle attachment surface.
- Added Rigidbody vehicle physics and Driver/Crew station state.
- Unity 6000.3.23f1 compiled `Assembly-CSharp.dll` successfully at 01:49.
- Added attached weapon mass/center-of-mass contribution to vehicle hosts.
- Added attached weapon recovery, collision-triggered Player knockdown and state HUD.
- Added projectile damage, health and simple Rigidbody PvE targets.
- Expanded the scene builder and regenerated `PrototypeTest.unity` successfully.
- Added `PROTOTYPE_GUIDE.md` with controls, test loop and architecture snapshot.
- Fixed camera hierarchy so pitch affects `HeadPivot`, not the Player Rigidbody.
- Added scene-structure validation and automatic Build Settings registration.
- Added command-line Play Mode smoke test covering initialization, attach/recover,
  host recoil projectile spawn, vehicle mass, Driver enter/exit and Player knockdown.
- First Play Mode smoke run passed; it exposed kinematic velocity warnings, now fixed.

## Next

- Re-run warning-free scene build and Play Mode smoke test.
- Commit the physics/PvE/scene validation increment.
- Review specification coverage and implement remaining high-value gaps.
