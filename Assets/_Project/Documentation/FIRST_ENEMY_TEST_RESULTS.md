# Enemy đầu tiên — kết quả kiểm chứng

Ngày: 2026-10-15. Unity 6000.5.4f1, NGO 2.13.3, protocol 5.

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Enemy setup/bake/migration | PASS sau khi sửa API agentTypeID theo Unity hiện có | EnemySetup.log |
| EditMode toàn bộ | **PASS 51/51**, 0 failed/skipped, Unity exit=0 | EditMode.xml / Tests.log |
| Melee chỉ hit khi hai bên sống, có LOS, trong tầm | PASS unit | EnemyTests |
| Spatial/config không hợp lệ, damage trước spawn | PASS unit | EnemyTests |
| NavMesh nối home đến khu Player và đi vòng bia | PASS unit | EnemyTests |
| Setup idempotence không đổi scene/prefab/nav/list đã đúng | PASS unit | EnemyTests |
| Windows Development Enemy build | PASS, exit=0, không error CS/warning CS | EnemyBuild.log |
| AI + damage hai chiều qua 4 tiến trình | **PASS**, suite exit=0; sáu mục host/client1/client2/client3/fifth/closed đều Passed=true | EnemySmoke/Summary.json |
| Spawn/authority | Đúng một robot, agent chỉ chạy Host | maximumEnemies=1 / authorityCorrect=true |
| Patrol/chase/windup/melee | PASS trên cả bốn tiến trình; Player nhận damage thực | patrol/chase/windup/meleeDamage=true |
| Đổi mục tiêu sau khi Player gục | PASS trên cả bốn tiến trình | retarget=true |
| Né ra xa trong windup | PASS: Player thứ hai không mất HP, counter hit không tăng | dodgedAttack=true |
| Mất mục tiêu/quay về | PASS trên cả bốn tiến trình | returning=true |
| Client dùng RPC súng bắn robot | PASS: HP robot giảm, hit feedback tăng so với baseline | clientShotDamagedEnemy / enemyDamaged=true |
| Enemy dead/respawn | PASS: không đi/đánh khi chết, rồi trở lại với HP đầy | enemyDead/Respawned=true; deadMoved/Attacked=false |
| Client gọi API ghi HP robot | PASS: ba Client bị từ chối | clientWriteRejected=true |
| Regression Health/combat/controller/Room Full/Closed/Host disconnect | PASS | Các trường baseline và sáu kết quả tổng hợp |

Source C# trong project khớp bản sao đã build và chạy smoke (0 file khác biệt). Không tìm thấy NullReferenceException/InvalidOperationException/ArgumentException/RpcException hoặc lỗi tạo agent/SetDestination trong log Player được quét. Kết quả trên là loopback với input/RPC tự động, không thay thế QA chuột/UI hay Relay trên nhiều máy.

## Bảo toàn dữ liệu

Tạo RobotEnemy.prefab và EnemyTestNavigation.asset mới; bổ sung enemy reference trên NetworkPlayer.prefab và entry trong MvpNetworkPrefabs.asset. Backup trước tích hợp: `Builds/EnemyBackups/20261015-105116-131-BeforeIntegration/`. Giữ nguyên GUID các asset cũ, không xóa entry/component cũ.

Không sửa Scene, ProjectSettings hoặc Packages; package AI Navigation đã có sẵn. Editor tool bake từ Ground và vị trí bốn bia mẫu; NavMeshData chỉ được đăng ký trên Host khi robot spawn. Source C# bao gồm EnemySmokeScenario Development-only; kịch bản test không chạy trong phiên chơi thường.

## Chưa kiểm thử trực tiếp

- Bắn/di chuyển bằng chuột/phím thật, hiển thị model/HP/màu windup/dead trên màn hình.
- Cảm giác điều khiển, camera/Esc/Alt+Tab khi bị enemy tấn công.
- Relay trên nhiều PC, độ trễ/jitter/packet loss thực tế.
- Client disconnect ngay trong đòn enemy đang windup, cover melee động qua mạng, nhiều enemy/Boss/wave hoặc các map mới.
- Release build; mới build Windows Development. Không có Animator/audio/loot/revive/game over trong module này.

Hướng dẫn chạy mới: FIRST_ENEMY_SYSTEM.md. Dùng EnemyPlayer protocol 5 trên mọi máy, tạo phòng mới; không trộn CombatPlayer protocol 4.

## File tạo/chỉnh sửa

- Tạo: Gameplay/Enemy/EnemyState.cs, RobotEnemy.cs; UI/Multiplayer/RobotEnemyView.cs; Editor/EnemySetupTool.cs; Networking/Session/EnemySmokeScenario.cs; Tests/EditMode/EnemyTests.cs và .meta.
- Tạo asset: Prefabs/Networking/RobotEnemy.prefab, Settings/EnemyTestNavigation.asset và .meta.
- Chỉnh sửa: MultiplayerConfig.cs/asset (thông số AI, protocol 5), NetworkHitscanWeapon.cs (damage enemy/ref), MultiplayerSession.cs (spawn robot), NetworkPlayer.prefab, MvpNetworkPrefabs.asset, MultiplayerSetupTool.cs (build), MultiplayerSmokeHarness.cs, Tools/Run-MultiplayerValidation.ps1, Tools/Run-MultiplayerSmoke.ps1.
- Tài liệu: FIRST_ENEMY_SYSTEM.md, báo cáo này, MULTIPLAYER_ARCHITECTURE.md, MULTIPLAYER_TEST_CASES.md.
