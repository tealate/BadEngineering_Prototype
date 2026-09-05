# BadEngineering Prototype Guide

The generated `Assets/Scenes/PrototypeTest.unity` scene is the executable specification sandbox.

## Controls

- `WASD`: move on foot; accelerate/steer while in the Driver seat
- `Space`: jump on foot
- Mouse: look
- `1`, `2`, `3`: select an owned weapon (including one attached to a vehicle)
- Left/right mouse: primary/secondary weapon input
- `E`: pick up a dropped weapon, attach the selected weapon at the aimed vehicle position,
  recover your attached weapon by aiming at it, enter a seat, or leave the current seat
- `Q`: drop the selected weapon from either Player or Vehicle host

## Intended test loop

1. Fire the Starter Gun and observe Player recoil/loss of control/recovery.
2. Pick up Heavy Cannon or Kick Gun with `E`.
3. Aim at different points on the vehicle chassis and press `E` to attach.
4. Keep that owned weapon selected and fire it; recoil is applied to the vehicle at its attach point.
5. Attach weapons asymmetrically and observe total mass and center-of-mass changes.
6. Enter Driver Seat, verify weapon input is disabled, and drive with `WASD`.
7. Enter Crew Seat, verify owned attached weapons remain selectable and fireable.
8. Shoot the approaching PvE targets.

Rebuild the scene after changing the builder with **Bad Engineering > Build Prototype Test Scene**.

## Architecture snapshot

- `Weapon.Owner` is the owning `PlayerWeaponSlots`; it does not change when attached to a vehicle.
- `Weapon.Host` is an `IWeaponHost` implemented by the `WeaponHost` component on Player/Vehicle.
- `Weapon.State` is `Held`, `Attached`, or `Dropped`.
- Recoil always targets the current Host body with `AddForceAtPosition`.
- Vehicle host mass and center of mass are recalculated from attached weapon mass/positions.
- `VehicleStationUser` distinguishes Driver and Crew. Driver routes movement to the vehicle and cannot use weapons; Crew retains normal weapon input.
- `PlayerPhysicsController` owns grounding and Normal/Uncontrolled/Recovering transitions;
  the first-person controller only moves when `CanMove` is true.

Multiplayer authority is intentionally not implemented yet: Netcode for GameObjects is not currently a project dependency. The Owner/Host split and input-routing boundaries are designed so authoritative commands can be added without replacing the local gameplay model.
