# Encounter bảo vệ uplink

**Hiện hành Protocol26:** [checkpoint và tutorial chip](CHECKPOINT_CHIP.md) bổ sung drill trước máy phát, chỉ carrier channel uplink; retry giữ máy phát và dữ liệu, bắt đầu lại trận bảo vệ trong Attempt mới. Các kết quả và reset toàn lượt dưới đây là lịch sử Protocol25.

Protocol25, 21/10/2026. Bổ sung một đợt ba Patrol Unit vào phân đoạn `CTU_01_RESTORE_UPLINK`, sau khi khởi động máy phát. Đây là encounter campus của prototype nhiệm vụ đầu, chưa phải MEKONG GUARDIAN, nhiệm vụ cứu Free Settlement hoặc tutorial chuyển chip.

## Cách chơi

Chọn CAN THO / CAMPUS II từ MainMenu, đội 2–4 người dùng cùng Protocol25. Tập hợp, thu ba bản ghi và giữ E 4 giây tại máy phát như trước. Khi máy phát hoàn tất, ba robot xuất hiện tại sân trước Học liệu. HUD hiển thị số robot còn sống.

Một người giữ E 5 giây tại uplink, đồng đội chặn hoặc hạ robot để bảo vệ người tương tác. Người đang giữ E không thể bắn. Nhận sát thương, thả E, di chuyển, gục, mất LOS/khoảng cách/focus hoặc mất heartbeat đều hủy channel theo cơ chế chung. Có thể dọn địch trước rồi thực hiện uplink.

Hủy và giữ E lại không tạo thêm đợt địch, không hồi sinh robot đã bị hạ, không khôi phục HP của robot còn sống. Khi toàn bộ robot bị hạ, HUD hiện Area clear; vẫn phải giữ E để hoàn tất liên lạc. Khi liên lạc được khôi phục, robot còn sống và xác robot được dọn. Wipe làm nhiệm vụ Failed và dọn encounter; Host restart đặt đợt địch về Dormant. Lượt mới chỉ xuất hiện địch sau khi bật lại máy phát.

## Dữ liệu và authority

`UplinkEncounter` là NetworkBehaviour trên Room. Chỉ Host kích hoạt theo bit máy phát đã hoàn tất và objective uplink đang khả dụng. Ledger gồm stable wave ID `CTU01_UPLINK_DEFENCE`, Attempt, Activations, Spawned, Alive, Defeated và phase. Activations được khóa trước khi spawn, tối đa một lần mỗi Attempt; thao tác E không có quyền đặt lại ledger.

Phase: Dormant → Active → Cleared nếu hạ hết địch; Secured khi mission hoàn tất; Failed khi mission thất bại. Round restart tạo Attempt mới với số đếm bằng 0. Cấu hình hiện có ba robot, validator giới hạn 1–4, không có spawner vô hạn hay tự tăng địch theo số lần tương tác.

`UplinkPatrol.prefab` riêng, đăng ký trong `MvpNetworkPrefabs.asset`, tái sử dụng AI/mesh chiến đấu hiện tại. Robot nhiệm vụ không tự respawn và dùng chung một NavMeshDataInstance do encounter sở hữu. Robot thử nghiệm vẫn giữ hành vi cũ trong sân tập và Recon; nó không xuất hiện đầu map khi chơi nhiệm vụ uplink. Không thêm NetworkManager, PlayerInput hay AudioListener.

| Spawn | X | Z |
|---|---:|---:|
| Patrol 1 | 50 | −46 |
| Patrol 2 | 58 | −46 |
| Patrol 3 | 66 | −46 |

Vị trí nằm trên NavMesh hiện có, đường tới người vận hành tại X61 Z−44 đã được kiểm tra. Không sửa scene campus, NavMesh, nhà, cây, đường hoặc nguồn Blender. Prefab địch mẫu không bị ghi đè. Nếu cấu hình/spawn NavMesh không hợp lệ, Host dọn encounter, báo Failed và có thể restart lượt.

## Kiểm tra và bằng chứng

Kết quả lưu tại `Builds/Encounters/20261021-UplinkDefense`. Bộ `Run-UplinkDefenseSmoke.ps1` dùng owner RPC giữ E và bắn thật; placement, damage gây wipe và pause AI ở các bước cô lập là fixture. Bước tấn công cho robot chạy AI thật; bước chống hồi sinh cho robot chết chạy qua thời gian respawn của bản demo. So sánh ID của cùng đợt, snapshot hoàn tất và reset giữa các participant.

Kiểm tra localhost không thay thế nghiệm thu Relay trên nhiều máy thật. Carrier/chip, Trace, checkpoint lưu xuyên phiên, encounter cứu dân và Director/wave framework tổng quát vẫn chưa triển khai.

Kết quả Protocol25 đã đạt:

- `EditMode.xml`: 145/145, gồm kiểm tra spawn/collider/NavMesh, một wave mỗi Attempt, prefab mission không respawn, đăng ký mạng và cài lại không đổi asset.
- `NetworkSmoke2-Verified` và `NetworkSmoke4`: mỗi đội qua 22 tình huống, cùng ID wave và snapshot hoàn tất/reset. Robot chạy AI thật đánh ngắt channel; Client bắn hạ robot trong khi người vận hành giữ E; người đang giữ E bị từ chối bắn. Chờ qua thời gian respawn demo vẫn không hồi sinh robot nhiệm vụ. Wipe/restart và kích hoạt lượt mới không vượt cap ba robot.
- `GraphicalPreview/Checks.json`: 107 kiểm tra, gồm prompt/progress, hủy E không thêm wave, HUD số địch, dọn địch còn sống khi uplink hoàn tất và cleanup UI khi rời phiên. Ảnh `26-uplink-defence-scene.png` ghi nhận ba robot tại sân Học liệu.
- `MissionRegression`: 21 tình huống authority/idempotence của chuỗi bốn bước vẫn qua trên Host–Client.
- `EnemyRegression`: bốn participant qua AI patrol/chase/melee/retarget/death/respawn, movement/health/combat; từ chối người thứ năm và join sau khi đã bắt đầu. Respawn của robot demo vẫn hoạt động.
- `PreservationValidation.json` và `Idempotence.json`: scene, NavMesh, art, Player và robot demo nguyên byte; Room chỉ thêm một component, prefab list chỉ thêm một entry, menu chỉ đổi nhãn Protocol25. Cài lại không tạo thay đổi.

Lần `NetworkSmoke2` đầu dừng ở assertion reset cuối do fixture để người chơi đứng sẵn trong vòng tập hợp, khiến bước đó tự hoàn tất sau hai giây. Đã đặt fixture reset ngoài vòng và chạy lại toàn bộ trong `NetworkSmoke2-Verified`; không sửa logic gameplay để vượt kiểm tra.
