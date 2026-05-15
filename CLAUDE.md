# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6000.2.5f1 VR multiplayer zombie shooter. Key stack:
- **VR**: XR Interaction Toolkit 3.2.2, Oculus/OpenXR, XR Hands
- **Multiplayer**: Photon Fusion 2.0.12
- **Navigation**: Unity AI Navigation 2.0.11 (NavMesh)
- **Input**: Unity Input System 1.14.2
- **Rendering**: URP 17.2.0

## Unity Development Commands

This project is developed inside the **Unity Editor** (no CLI build pipeline). Code changes take effect on next Play or recompile. There are no test runner or lint commands configured.

- Open project: Launch Unity Hub, select this folder
- Build: `File > Build Settings > Build` in the Unity Editor
- Test scene: `Assets/Scenes/TestScene.unity`

## Architecture

### System Overview

```
BasicSpawner (network init)
    └─> spawns NetworkRunner + PlayerPrefab

PlayerController (VR movement)
    ├─> reads XR thumbstick / Gamepad / Keyboard
    └─> moves CharacterController relative to XR Camera orientation

RifleController / PistolController (weapons)
    ├─> attached to XR Grab Interactables
    ├─> fire via XRBaseInteractable.activated events
    └─> raycast hit detection (damage application not yet wired)

EnemyPatrol (state machine: Patrol → Follow → Attack)
    ├─> NavMeshAgent for pathfinding
    ├─> 3-tier vision: OverlapSphereNonAlloc → dot product cone → raycast LOS
    └─> calls EnemyAttack.Attack() when in range

EnemyAttack (melee)
    └─> triggers animator + placeholder for player damage
```

### Script Responsibilities

**`Assets/Scripts/BasicSpawner.cs`** — Photon Fusion entry point. Creates `NetworkRunner`, joins/creates session `"L4DVR_Room_01"`, spawns player prefabs. Implements `INetworkRunnerCallbacks`; most callbacks are stubs.

**`Assets/Scripts/PlayerController.cs`** — VR locomotion. Input priority: XR thumbstick > Gamepad left stick > WASD keyboard. Movement is relative to the XR Camera's forward direction. Uses `CharacterController`.

**`Assets/Scripts/RifleController.cs`** — Semi-auto rifle. Fire rate 0.3 s. Plays audio + particles, haptic feedback, raycast hit detection. Damage not applied yet.

**`Assets/Scripts/PistolController.cs`** — Pistol with ammo management. Magazine 10, reserve 40, fire rate 1.2 s. Has critical hit system (10% chance, 1.5× multiplier). `Reload()` method exists but is **not yet called** — reload input binding is missing.

**`Assets/Scripts/Enemies/EnemyPatrol.cs`** — 326-line FSM. States: `Patrolling`, `Following`, `Attacking`. Detection range 10 m, cone angle 90°, close-proximity follow range 3 m. Stuck detection recovers pathfinding failures after 5 s. Patrol points use 1.5 m random offsets to prevent enemy clustering.

**`Assets/Scripts/Enemies/EnemyAttack.cs`** — Melee attack, 1.5 s cooldown, 1.2 m range, 10 HP damage. Triggers animator but **damage application is commented out**.

**`Assets/Scripts/GameManager.cs`** — Stub only, logs "Game Started".

**`Assets/AnimateHandOnInput.cs`** — Drives hand animator "Trigger" and "Grip" float parameters from XR controller input.

### Known Integration Gaps

These are the main unconnected pieces as of the last commit:

1. **Weapon → Enemy damage**: Raycasts in both weapon controllers hit enemies but no `TakeDamage()` call exists yet.
2. **Enemy → Player damage**: `EnemyAttack` damage logic is commented out.
3. **Pistol reload input**: `Reload()` method is complete but never invoked; needs secondary button binding.
4. **Multiplayer sync**: `BasicSpawner` callbacks are stubs; `PlayerController` is not yet network-aware.
5. **Wave/game management**: `GameManager` is a placeholder with no game state logic.

### Prefabs & Scene

- Main scene: `Assets/Scenes/TestScene.unity`
- VR Player prefab: `Assets/Prefabs/` (includes IK rig)
- Shoot VFX: `Assets/Prefabs/ShootExplosionVFX.prefab`
- Zombie assets: `Assets/Zombie/`
- Environment: `Assets/Abandoned_Asylum/`
