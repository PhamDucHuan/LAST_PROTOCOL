# Enemy đầu tiên — LAST_PROTOCOL

Unity 6000.5.4f1 / NGO 2.13.3, protocol **6**. Một robot cận chiến trong MultiplayerTest, do Host quản lý. Mesh tạm tạo bằng các khối Unity; chưa có Animator/model/âm thanh chuyên dụng, Boss, wave hoặc phần thưởng.

## Chơi thử

1. Dừng Play và đóng bản Client cũ; chờ Unity compile. Dùng Editor với code mới làm Host, tạo phòng mới.
2. Client dùng `Builds/ValidationProject/Builds/RevivePlayer/LAST_PROTOCOL.exe`, cùng PC thêm `--lp-auth-profile client2`. Join/Ready/Start như trước. Không dùng bản protocol 4/5 cũ cùng bản hồi sinh protocol 6.
3. Robot màu đỏ ở phía sau hàng bia, gần `(0, 0, 15)`. Tiến lại gần hoặc bắn vào thân robot để thử aggro. Giữ chuột trái để bắn bằng súng hiện có.
4. Robot tuần tra, phát hiện người còn sống, đuổi theo và chuyển vàng khi chuẩn bị đánh. Rời khỏi tầm đánh trong thời gian chuẩn bị để né. Host kiểm tra lại range/LOS/trạng thái sống đúng lúc áp dụng damage.
5. HP robot mặc định 100; mỗi phát súng hợp lệ gây 25 damage. HP và trạng thái hiển thị trên đầu, mọi máy nhận cùng snapshot. Robot gục thì ngừng AI, bỏ collider, đổi màu/xoay visual; sau 8 giây trở lại điểm xuất phát với đầy HP để thử tiếp.
6. Robot đánh 10 damage/đòn; Player gục không còn được chọn làm mục tiêu và không thể bắn. Đồng đội còn sống có thể tiếp tục hạ robot. Đồng đội giữ E 3 giây trong 2,5 m và có LOS để cứu với 40% HP; tránh robot đánh trong lúc cứu. Cả đội gục thì Host dùng RESTART TEST (HOST). Xem DOWNED_REVIVE_SYSTEM.md.

Friendly fire mặc định OFF chỉ áp dụng giữa Player; súng vẫn gây damage lên enemy. Bia tập bắn cũ được giữ lại. Không có auto-reset HP của Player khi robot respawn.

## Authority và module

| Script | Vai trò |
|---|---|
| `RobotEnemy` | NetworkBehaviour trên Host: NavMeshAgent, chọn mục tiêu, AI state machine, melee windup/cooldown, HP/death/respawn, đăng ký/gỡ NavMeshData |
| `EnemyState` | Snapshot atomic gồm HealthState, Mode, TargetClientId, counter đòn đánh thành công; server-write/everyone-read |
| `EnemyRules` | Các kiểm tra range, dữ liệu vị trí, điều kiện melee thuần để kiểm thử |
| `RobotEnemyView` | Visual robot, thanh HP, text trạng thái, màu báo windup/hit/dead, billboard cục bộ theo camera |
| `EnemySetupTool` | Bake dữ liệu sân mẫu, tạo robot prefab, thêm tham chiếu trên Player weapon và đăng ký network prefab; backup/idempotence |
| `EnemyTests` | Guard trước spawn, range/LOS/alive, config, đường NavMesh/vật cản, installer không đổi scene/asset đã đúng |
| `EnemySmokeScenario` | Kịch bản Development opt-in kiểm tra AI và Client bắn robot; không chạy trong phiên thường |

`NetworkHitscanWeapon` dùng muzzle ray đã được Host xác nhận để gọi `RobotEnemy.TryApplyDamageServer(damage, attackerHealth)`. Client không gửi target ID, HP hay lượng damage. Robot bị bắn có thể aggro người bắn hợp lệ trong giới hạn lose/leash và có LOS. Friendly fire không chặn sát thương lên robot.

## AI state machine

- **Patrol**: qua lại hai điểm cách home +/-3 m theo trục X, tốc độ cấu hình.
- **Chase**: Host giữ một mục tiêu sống hợp lệ; cập nhật destination theo NavMesh. Re-evaluate mỗi 0.2 giây, không RPC AI mỗi frame.
- **Windup**: dừng di chuyển, quay về mục tiêu, báo vàng trong 0.45 giây. Giữ chính mục tiêu của đòn đang chuẩn bị; không chuyển sang đánh người khác giữa đòn.
- **Recover**: sau khi kiểm tra hit, nghỉ 1 giây. Đòn trượt cũng tiêu thụ lượt/cooldown.
- **Returning**: bỏ mục tiêu gục/disconnect/ra khỏi leash hoặc mất dấu quá 2 giây; đi về khu vực home, tiếp tục tuần tra. Có thể chọn người sống khác trong phạm vi.
- **Dead**: HP=0, dừng agent/collider/attack; respawn đầy HP tại home sau 8 giây chỉ nhằm hỗ trợ sân test.

Melee quyết định trên Host theo khoảng cách 3D từ mắt robot đến điểm thân Player, LOS raycast, mục tiêu còn hoạt động và phase Playing. Bia/tường/Player khác đều có thể chắn LOS. Không đánh xuyên vật cản, không đánh người đã gục, không gây damage tiếp từ windup sau khi robot chết. SuccessfulAttacks tăng chỉ khi PlayerHealth thực sự nhận damage.

NetworkTransform mặc định server authority, có interpolation. NavMeshAgent **chỉ được enable trên Host**; Client nhận transform/snapshot và hiển thị. Không đồng bộ camera. View có thể tiếp tục interpolation ngắn sau khi nhận Dead; Host đã dừng ngay, smoke cho buffer 0.4 giây trước khi đo độ đứng yên trên Client.

## Config mặc định

`Assets/_Project/Settings/MultiplayerConfig.asset`:

| Trường | Giá trị |
|---|---:|
| enemyMaxHealth | 100 |
| enemyMoveSpeed | 3 m/s |
| enemyDetectionRange | 12 m |
| enemyLoseRange | 18 m |
| enemyLeashDistance | 22 m từ home tới Player |
| enemyAttackRange | 1.8 m |
| enemyAttackDamage | 10 |
| enemyAttackWindup | 0.45 s |
| enemyAttackCooldown | 1 s sau windup |
| enemyRespawnSeconds | 8 s |

Các khoảng cách/timer phải hữu hạn và dương; lose >= detection, leash >= lose, attackRange <= detection. Chỉnh trước khi tạo phiên; chưa có UI đồng bộ config asset giữa máy.

## Prefab, NavMesh và Editor

- `Assets/_Project/Prefabs/Networking/RobotEnemy.prefab`: NetworkObject, NetworkTransform, CapsuleCollider, NavMeshAgent disabled, RobotEnemy, RobotEnemyView và visual primitive không có collider riêng. Hitbox là capsule thân.
- `Assets/_Project/Settings/EnemyTestNavigation.asset`: bake riêng từ BoxCollider Ground hiện có và vùng loại trừ tại bốn bia cố định. Dùng agent type sẵn có, radius 0.4/height 2; không sửa NavMeshAreas hoặc ProjectSettings.
- NavMesh chỉ được add vào world trên Host khi robot spawn và remove khi despawn. Demo chỉ spawn một robot; chưa có quản lý NavMesh dùng chung cho wave nhiều robot.
- Dữ liệu bake dành cho sân Ground hiện tại và vị trí bia mẫu. Nếu thay map/obstacle, cần cập nhật nguồn geometry và bake lại; Install giữ asset có sẵn để tránh ghi đè dữ liệu người dùng.
- Menu `LastProtocol > Enemy > Install Robot Enemy Demo`: thêm asset/ref/registration nếu thiếu, không ghi scene. Lưu scene chưa có tên và đóng Prefab Mode trước khi dùng. Backup prefab/list nằm ở `Builds/EnemyBackups/`.
- Menu `LastProtocol > Enemy > Build Windows Enemy Demo`: tạo `Builds/EnemyPlayer/LAST_PROTOCOL.exe` trong project đang build.
- Bản kiểm chứng tự động nằm ở `Builds/ValidationProject/Builds/EnemyPlayer/`. Cài package mới, tài khoản Cloud và thao tác scene thủ công không cần thiết cho sân test này.

## Kiểm thử

Chạy lần lượt:

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage EnemyBuild
.\Tools\Run-MultiplayerSmoke.ps1 -EnemyTest
```

Enemy smoke chạy 1 Host + 3 Client, bao gồm regression movement/Health/combat trước; robot tạm dừng trong phần baseline. Sau đó bật AI, đưa một Client vào vùng phát hiện, đợi nhận melee, làm mục tiêu gục để robot chọn người khác, đưa mục tiêu thứ hai ra xa giữa windup để thử né; chờ robot quay về, dùng **Client thứ ba bắn qua RPC súng thật** để hạ robot, kiểm tra dead đứng yên/không attack và respawn. Các warp/gây gục có chủ đích chỉ trong harness Development.

Không có `--lp-enemy-test` thì kịch bản không chạy. Các suite local cũ có `--lp-smoke` nhưng không có enemy flag bỏ spawn robot để không làm sai lệch Health/cadence regression. Phiên chơi thông thường không có smoke CLI luôn spawn robot và chạy AI bình thường.

Log/JSON: `Builds/ValidationResults/EnemySmoke/`; kết quả thực chạy và giới hạn kiểm chứng: FIRST_ENEMY_TEST_RESULTS.md. Các thao tác chuột/UI/animation hiển thị, cảm giác di chuyển, Relay Internet và các map mới vẫn cần kiểm thử trực tiếp.
