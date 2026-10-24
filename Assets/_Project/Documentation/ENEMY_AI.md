# Enemy AI — Protocol 27

Source: `Design/Story/LAST_PROTOCOL.docx`, section 4.4 and 4.5 (original document SHA256 `5559FACAD4C7B1D13B04BD0170C838611AE792951B32A39CE7A5B73ECC460897`). This implements the ordinary robot AI foundation and connects it to the existing Can Tho uplink encounter.

## Play

Host a Can Tho mission with 2–4 players. Complete the existing rally/data/chip relay tutorial/generator steps. The generator spawns exactly three tactical robots: Frontliner, Suppressor, Hunter. One player can hold E at the uplink while teammates defend. Cancellation never spawns a new wave. On failure, Host retries the confirmed checkpoint with one new wave for that attempt.

## Decisions and fairness

Host owns all targeting, navigation, timers, chip reservations and damage. Clients receive one EnemyState snapshot including role, HP, shield, action target and warning timing. Clients disable NavMeshAgent. Protocol 27 requires matching builds.

EnemyBrain uses bounded profile values, 5 Hz perception/path updates, vision cone, actual physics LOS, and short-lived intensity-scaled sound events. An approved gunshot and a sprint generate sound; a server-only decoy adapter can generate a false sound. Audible locations and nearby squad reports become decaying memory, not live wall tracking. Reports propagate within 18 m of the same encounter group; recipients do not chain rebroadcast. Search returns to home after memory expires. NavMesh paths must be complete; stuck enemies are never teleported by AI.

States cover Idle, Patrol, Suspicious, Investigate, Alert, Chase, Windup/attack, Recover, Reposition, Search, Returning, CallReinforcement, ProtectNode, CaptureChip, Disabled, Hacked, Retreat, Repair and AreaDenial. CallReinforcement currently alerts existing nearby robots; the opening encounter budget does not permit another spawn. All offensive attacks have visible aim/radius warnings. Rifle aim is locked during windup so movement can dodge it; walls block shots. Downed players cannot be targeted.

Six reusable profiles/prefabs:

| Role | Behavior |
| --- | --- |
| Frontliner | Approaches and telegraphs melee attacks |
| Flanker | Approaches via a lateral NavMesh destination |
| Suppressor | Telegraphs a fixed rifle ray; 6 damage by default |
| Support | Repairs living nearby allies by at most 15 HP and grants a non-stacking shield of at most 20; cannot resurrect |
| Controller | Warns before a small area hazard; at most 3 m radius and 4 damage per second |
| Hunter | Prioritizes a **visible** chip carrier; attempts to secure a visible dropped chip |

Low-health robots have a bounded retreat and cooldown. Server DisableServer / HackServer are adapter hooks with bounded durations; hacked robots temporarily attack other robots, not players. These are not unlocked player abilities: the existing Tier-1 chapter does not gain robot hacking.

## Chip contest and Trace

A Hunter reserves the same single chip snapshot that player recovery uses. It must remain alive, close and in LOS for a 3-second capture. Damage, disable, hacking, role changes or death release its reservation or captured chip. Killing/despawning the holder leaves the chip recoverable. A player can recover with the existing E channel after interrupting the Hunter. Captured chips are displayed above the holder; the HUD identifies capture, carrier and Trace.

Prototype balancing: dropped chip Trace rises 2/s, captured chip 8/s, only during the real post-generator encounter. The safe relay tutorial has no Trace timer. Successful player recovery clears Trace. Trace 100 fails the mission and allows checkpoint retry; retry restores the confirmed player carrier, no duplicate records or device credit. Integrity/campaign penalties are not invented in this prototype.

## Adaptive Director / DEADLINE

Telemetry derives from actual approved combat: shots, static shots, actual damage share/range, down/revive, chip transfers and squad spacing. The safe scripted relay down does not count as combat. A whitelist chooses one response after 10 seconds, with a 2-second warning. One response maximum per attempt, at most three living enemies, no HP reset or extra wave on role change. Armor absorbs at most 20%; ReviveHunter prioritizes a living reviver and grants recently revived players 3 seconds of AI protection.

Implemented whitelist: ArmorResponse, FlankAmbush, CyberShield, ReviveHunter, AreaDenial. AntiSniperDrone and ResourcePressure are declared but cannot be selected without their future drone/inventory modules. No fabricated weapon classes, ammo, ability efficiency or hack telemetry. A validated future hacking module can call the explicit clean-hack adapter.

EncounterAlert decays to zero after 8 seconds without pursuit. MissionMeter stays for the mission and retains at least 50% on retry, with a monotonic checkpoint floor so repeated failures cannot halve it repeatedly. Bands are Observing (0–25), Alerted (26–50), Hunting (51–75), Emergency (76–99), Protocol Zero (100). MissionMeter 100 is a reaction tier, not Trace failure. CampaignThreat is separately replicated but remains 0 until campaign/Core decisions and persistence exist. Director logs include tendency, evidence, selected response, attempt and budget.

## Scope and validation

Existing map, art, baked NavMesh, Player prefab, training RobotEnemy and legacy UplinkPatrol remain preserved. New tactical prefabs are additive. Setup must run in the validation copy; promotion checks exact old asset content and GUIDs.

This is a gameplay AI foundation using existing robot visuals. Boss phases, unique drone navigation/animations, complete combat classes, resource pressure, environmental camera/alarm adapters, Tier-2 hack authorization, campaign persistence and difficulty playtesting remain separate work. Localhost multiplayer validation does not establish internet/Relay latency behavior.

Tools: `Run-EnemyAIValidation.ps1`, `Run-EnemyAISmoke.ps1`, `Validate-EnemyAIPreservation.py`; evidence under `Builds/EnemyAI/20261022-EnemyAI`. The opt-in Development AI scenario skips quest setup to reach combat; its owner shots/recovery, production AI, Trace failure and checkpoint retry are real. Existing checkpoint smoke verifies the complete quest path separately.
