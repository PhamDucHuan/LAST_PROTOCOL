# Can Tho — 15–20 minute rescue operation (Protocol 28)

The default Can Tho mission is now `CTU_01_CAMPUS_RESCUE`, a 12-stage campus rescue route. This extends the existing fictional opening segment on the current map. It does not claim to implement the entire MEKONG chapter, its boss or the final Core choice.

| Stages | Gameplay | Active operation time |
| --- | --- | --- |
| 1–4 | Gate B rally, three records, chip relay tutorial, generator and first uplink encounter | Existing short interactions and combat |
| 5 | Protect the library transfer | 3:00 |
| 6–7 | Reconnect two grid switches, stabilize power near ICT | Switches: 4 s each; stabilization: 3:00 |
| 8–9 | Secure B1/RLC emergency logs, split the squad between two signal sites | Logs: 4 s each; signal sync: 3:30 |
| 10–11 | Reconnect the west/ATL radio route and defend the rescue broadcast | Relays: 4 s each; broadcast: 4:00 |
| 12 | Whole squad regroups alive at extraction, chip secure | 0:10 |

Four operations require **810 seconds / 13:30** of actual occupied work. The rest comes from travel across the existing campus, the opening tutorial, terminal interactions and combat. Target is approximately 15–20 minutes on a successful run, rather than a forced wall-clock gate. Skilled sprinting/parallel routes may shorten travel; wipes and recovery can extend a run beyond 20 minutes. Human difficulty and completion-time playtesting are still needed.

## Operating rules

Hold-zone work needs at least one living player in its ring and the unique chip carried by a living player. Players can move and shoot normally; no long E hold is required. Leaving pauses accrued work. Signal sync needs different living players in two non-overlapping zones; a single avatar cannot fill both. The HUD shows two world markers, progress, active mission time and checkpoints. Extraction requires every connected player alive and the chip secure.

Initial E interactions retain the shared dispatcher, cancellation and owner RPC checks. Six added terminals have server-owned replicated identities so every Client sees the same device IDs and completion ledger. Each station counts once, including after retry.

## Combat pacing

The original generator encounter stays one wave per attempt. After the local uplink, ChapterOperation manages stage-local encounters. It warns for three seconds before the first group and checks for reinforcement approximately every 18 seconds. It never exceeds three living robots. Each stage has a finite group quota; cancelling E or receiving a progress tick does not reset that quota. Terminal legs have one patrol group; timed operations have repeated pressure. Role combinations rotate among the existing six profiles and inherit the active Director response.

Spawns must be on the existing NavMesh, have a complete path to the objective, stay at least nine metres from living players, and avoid overlapping robots. Stage completion cleans up its own group. A wipe or Trace failure restores confirmed station bits, the last confirmed carrier and the current stage; unfinished timer work restarts from that checkpoint. Confirmed timed stages are not replayed.

## Preserved content

No land, buildings, streets or baked navigation are expanded or regenerated. The six new terminals are spawned from a registered prefab only for this mission. The short `RestoreUplink` definition remains for explicit Development regression flags. Training/Containment scenes remain drills. Both Host and Clients need Protocol 28.

Runtime: `ChapterPlan`, `ChapterOperation`, extended `NetworkMissionState`, `MissionHUD`. Data: `CampusRescue.asset`; Room references that definition and the terminal prefab. Install only in the validation copy using `ChapterSetupTool`, then promote verified assets.

## Validation

`ChapterTests` checks the full chain, time budget, occupancy, anti-fast-forward cap, station uniqueness, valid checkpoint masks, navigation and installer idempotence. `--lp-chapter-test` is an explicit Development localhost fixture: it skips the unchanged introductory quest setup and accelerates operation ticks 60× to test the extended chain, real owner E RPCs, two-site gating and retry. It pauses enemies for deterministic mission checks; AI combat is validated separately. It is not evidence of a human 15–20 minute playthrough. Production uses unscaled Host time with no client API to advance progress.

Evidence and reports: `Builds/Pacing/20261022-Map1`.
