# Specification coverage

Coverage against `BadEngineering_SpecificationDiagram.md` on branch
`codex/specification-implementation`.

## Implemented

- **Player input and movement (sections 3-7, 32):** Rigidbody movement/jump/look,
  independent `PlayerPhysicsController`, Normal/Uncontrolled/Recovering transitions,
  collision/recoil impact entry, grounded/low-speed recovery gate, torque upright recovery.
- **Weapon domain (sections 8-20, 30-31):** explicit Held/Attached/Dropped state,
  independent Owner and `IWeaponHost`, three owned slots, selection of attached weapons,
  pickup/drop/recover, input routed through slots, projectile fire and host-aware recoil.
- **Vehicle and stations (sections 21-29):** Rigidbody vehicle movement, one occupied
  Driver per driver station, arbitrary Crew stations, Driver weapon restriction, Crew
  weapon access, arbitrary chassis hit-position attachment, attached mass and center of
  mass, point-applied recoil, mass-dependent acceleration.
- **World/PvE:** physical projectiles, damageable health and simple Rigidbody enemies.
- **Prototype validation (section 38):** generated scene covers local steps 1-5. A Play
  Mode smoke test also verifies two independent owners attaching weapons to the same
  vehicle body, plus owner/host transitions, seats, mass/COM and Player impact state.
- **Technical baseline (sections 36-37):** Unity 6, URP, Input System, Rigidbody physics,
  simple low-object-count visuals and a scene builder for repeatable test setup.

## Partially implemented

- **Secondary weapon action:** input and extension methods exist; the test projectile
  weapon intentionally has no secondary behavior yet.
- **Vehicle running model:** force/torque prototype driving is present. Wheels are visual;
  suspension and terrain-specific traction are outside the current specification detail.
- **Performance:** implementation keeps allocations/object count modest, but formal CPU,
  GPU, network and memory profiling has not been performed.

## Deferred

- **Network authority and synchronization (sections 33-35 and steps 6-8):** the project
  does not currently include Netcode for GameObjects. Local ownership/host/input boundaries
  and the multi-owner smoke case are in place, but no listen server, RPCs, NetworkObjects,
  physics synchronization, client spawn flow or two-machine test has been added.
- **Formal performance capture (step 9):** requires representative network sessions,
  enemy counts and target hardware before the numbers are meaningful.

## Verification commands

The repository includes editor entry points used from Unity batch mode:

- `BadEngineering.Editor.PrototypeSceneBuilder.BuildScene`
- `BadEngineering.Editor.PrototypePlayModeSmokeTest.RunFromCommandLine`

The latest scene generation and Play Mode smoke runs passed under Unity 6000.3.23f1
without C# warnings or runtime exceptions. A Windows 64-bit Standalone Player build also
completed with `Result: Success`.
