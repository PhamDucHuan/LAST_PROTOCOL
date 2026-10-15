# Basic Combat — kết quả kiểm chứng

Ngày: 2026-10-15. Unity 6000.5.4f1, NGO 2.13.3, protocol 4.

## Kết quả thực chạy

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Combat setup/migration | PASS sau khi xử lý scene untitled của batch; không error CS/warning CS | CombatSetup.log |
| EditMode regression + Combat | PASS 40/40, failed=0, skipped=0, Unity exit=0 | EditMode.xml / Tests.log |
| Aim finite/origin gần Player | PASS unit | CombatTests |
| Cooldown, spam, replay, unsigned sequence wrap | PASS unit | CombatTests |
| Cover/range/trigger và bỏ qua riêng shooter | PASS unit | CombatTests |
| Config và setup chạy lại không đổi scene/prefab/list | PASS unit | CombatTests |
| Health/controller/connection regression | PASS trong suite 40 tests | Các test hiện có |
| Windows Development Combat build | PASS, Unity exit=0, không error CS/warning CS | CombatBuild.log |
| Bốn Owner bắn và gây damage | PASS trên cả bốn tiến trình, shootingPlayers=4/damagingPlayers=4 | CombatSmoke/Summary.json |
| Spawn bia, HP giảm/gục/tự reset | PASS, đúng bốn bia trên mọi tiến trình | maximumTargets=4, targetDamage/Down/ResetObserved=true |
| Bắn khi gục / aim NaN, zero, origin xa | PASS: không có phát bắn được chấp nhận trong các bước này | downedFireRejected / invalidAimRejected |
| Năm request cùng frame | PASS: một phát hợp lệ mỗi Owner, theo cooldown Host | shotSpamLimited=true |
| Friendly fire OFF | PASS: Player chặn đạn, HP Player không giảm | friendlyFireBlocked=true |
| Health/controller regression | PASS trên bốn tiến trình | HP/down/reset/movement/sprint/jump/ownership/camera |
| Room Full / Room Closed / Host disconnect | PASS | fifth/closed JSON; ba Client sawHostDisconnect=true |

Lượt CombatSmoke đầu quan sát đầy đủ combat trên bốn tiến trình, nhưng summary cuối đánh fail ba Client do harness đặt lại shootingPlayers/damagingPlayers về 0 ở frame despawn khi Host thoát. Đã sửa harness giữ số lượng lớn nhất đã quan sát, rebuild và chạy lại. Lượt cuối kết thúc **exit=0**, cả sáu mục host/client1/client2/client3/fifth/closed đều Passed=true; chỉ dùng lượt này để kết luận PASS. Lượt cũ được giữ trong History.

Toàn bộ source C# trong project khớp bản sao đã build và chạy smoke (0 file khác biệt). Đã xác nhận runtime/editor/NGO IL post-processing và Windows build không có error CS/warning CS. Đây là kiểm thử loopback bằng RPC input tự động, không phải xác nhận chuột/UI trực tiếp hay Internet.

## Tích hợp và bảo toàn dữ liệu

Backup Player prefab và MvpNetworkPrefabs.asset trước khi tích hợp: `Builds/CombatBackups/20261015-095320-703-BeforeIntegration/`. GUID các asset có sẵn được giữ nguyên. Tạo TrainingTarget.prefab và ShotTracer.mat mới; bổ sung Weapon/HUD và entry target, không xóa component hoặc entry cũ.

Không sửa Scene, ProjectSettings, Packages hoặc Unity Cloud. Combat arena spawn trong Gameplay Scene từ Host sau network scene load; không sửa scene hiện tại của người dùng. Setup scratch scene additive được đóng và khôi phục active scene; với editor tương tác, scene chưa có tên phải được lưu thủ công, tool không tự lưu hoặc thay thế nó.

## Chưa kiểm thử

- Bắn bằng chuột thật, tâm ngắm/tracer/HIT/label bằng mắt, mouse capture/Esc/Alt+Tab và nút UI: **CHƯA KIỂM THỬ** trực tiếp.
- Combat trên nhiều PC qua Relay/Internet, packet loss/jitter/high latency: **CHƯA KIỂM THỬ** trong module này.
- Friendly fire **ON** trên Player: **CHƯA KIỂM THỬ** tích hợp; cấu hình mặc định và smoke dùng OFF.
- Release build: **CHƯA BUILD RELEASE**. Development harness có điều kiện biên dịch như trước.
- Chưa có lag compensation, model/animation/audio súng, ammo/reload, Enemy AI, combat prediction, inventory hoặc revive gameplay.

## File tạo/chỉnh sửa

- Tạo scripts: Gameplay/Combat/CombatRules.cs, NetworkHitscanWeapon.cs, TrainingTarget.cs; UI/Multiplayer/WeaponHUD.cs; Editor/CombatSetupTool.cs; Tests/EditMode/CombatTests.cs và .meta.
- Tạo assets: Prefabs/Networking/TrainingTarget.prefab, Materials/ShotTracer.mat và .meta.
- Chỉnh sửa: MultiplayerConfig.cs + MultiplayerConfig.asset (protocol 4/thông số combat), NetworkPlayer.prefab, MvpNetworkPrefabs.asset, MultiplayerSession.cs (spawn arena), MultiplayerRoomUI.cs (hướng dẫn LMB), MultiplayerSetupTool.cs (menu build), MultiplayerSmokeHarness.cs, Tools/Run-MultiplayerValidation.ps1, Tools/Run-MultiplayerSmoke.ps1.
- Tài liệu: BASIC_COMBAT_SYSTEM.md, báo cáo này, MULTIPLAYER_ARCHITECTURE.md và MULTIPLAYER_TEST_CASES.md.

Hướng dẫn build/chạy/test: BASIC_COMBAT_SYSTEM.md. Binary mới nhất: `Builds/ValidationProject/Builds/CombatPlayer/LAST_PROTOCOL.exe`; không trộn với HealthPlayer protocol 3 hoặc ThirdPersonPlayer protocol 2.
