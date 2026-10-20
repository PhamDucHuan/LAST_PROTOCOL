# Kết quả kiểm tra bố cục Campus II revision4

Ngày 16/10/2026, Unity6000.5.4f1, Blender5.2.2 LTS, Protocol14. Tất cả Unity batch và player fixture chạy từ `Builds/ValidationProject`, sau đó tích hợp các asset có hash đúng với bản đã kiểm tra. Không thao tác scene trong Editor của người dùng.

| Kiểm tra thực hiện | Kết quả | Bằng chứng |
|---|---|---|
| Blender dựng/xuất | PASS, 263 renderer, 258.948 triangle, 277 collision, 40 landmark | `Builds/CanThoMap/20261016-SchematicLayout/Candidate02/BuildStats.json`, `Build03.log` |
| Compile / EditMode | **110/110 PASS**, không skip/fail | `Builds/CanThoMap/20261016-SchematicLayout/EditMode01.xml` và `.log` |
| Bố cục tham chiếu | Các cụm có quan hệ vị trí đúng, đủ các landmark bổ sung, ID duy nhất, thứ tự cổng C/B/A | `CanThoSchematicTests` trong XML trên |
| Lối đi / enemy NavMesh | Tất cả entrance/checkpoint reachable; đường xuyên nhà bị chặn; capsule đi trọn tuyến; gate mở, rào chặn | Map/Fence/Route/Schematic/Mission tests |
| Camera sát vật cản | PASS ở Nhà điều hành, Học liệu và rào; có obstacle, kéo camera vào, không overlap sphere0.2 | EditMode và graphical runtime |
| Ảnh Unity Direct3D12 | PASS, lưu 9 góc gồm plan từ trên xuống; process exit0 | `Builds/CanThoMap/20261016-SchematicLayout/UnityCaptureDX12/` |
| Windows Development build | PASS, không có compile error liên quan thay đổi | `WindowsBuild.log`, output `Builds/ValidationProject/Builds/CanThoSchematicPlayer/` |
| Graphical runtime | **70 kiểm tra PASS**, không failure | `RuntimeEvidence/GraphicalPreview/Checks.json` và `Player.log` |
| Host + 3 Client localhost | **6/6 Summary PASS**, gồm 4 người chơi, từ chối người thứ5 và phòng đã Start | `RuntimeEvidence/MissionSmoke/Summary.json`, JSON/log từng process |
| Nhiệm vụ Recon | Cả đội/partial/downed/hold ngắt, tiến độ chung, hoàn thành, wipe/restart; Client không ghi state | Mission smoke và graphical runtime |
| Bảo toàn và tích hợp | GUID giữ nguyên; source nhận đúng file đã test; các scene khác, Player/Room/Robot, MissionDefinition, package/settings không đổi | `PromotedAssets.json`, `FinalVerification.json`, backup |
| Relay nhiều máy / FPS thực tế | **CHƯA KIỂM THỬ lại trên revision4** | Không dùng localhost hoặc ảnh render thay bằng chứng Internet/performance |

Các đường dẫn ngắn như `RuntimeEvidence/...`, `WindowsBuild.log`, `PromotedAssets.json` ở dưới `Builds/CanThoMap/20261016-SchematicLayout/`. File `FinalVerification.json` ghi đối chiếu cuối sau tích hợp và xác minh package được thực hiện riêng bởi công cụ đóng gói.

## Phạm vi thực sự đã kiểm tra

EditMode gồm cả hồi quy các hệ thống hiện có. Bài kiểm tra mới xác nhận vị trí tương đối theo sơ đồ, hình rào chéo và đường tới lối vào; không khẳng định đo đạc 1:1 hay ngoại thất chính xác từng cửa sổ của trường thật.

Graphical preview đi qua menu/chọn campus/lobby/Ready/Deploy/return, kiểm tra chế độ nhẹ/FPS sampler, NavMeshAgent kích thước enemy đi 7 chặng, 3 camera probe, HUD nhiệm vụ và reset. Fixture này dùng solo opt-in cho kiểm tra, không đổi điều kiện campaign/co-op mặc định 2–4 người.

Smoke mạng chạy 1 Host + 3 Client bằng direct localhost, không gọi tài khoản Unity Cloud hoặc tự cấu hình dịch vụ. Cả bốn quan sát scene được chọn, sở hữu Player, movement/rotation/sprint/jump và state nhiệm vụ. Người thứ5 bị RoomFull; tham gia sau Deploy bị RoomClosed; Client nhận Host disconnect. Không gọi đây là test Relay trên bốn máy.

Lượt chụp Editor Direct3D11 ban đầu đã ghi ảnh nhưng process thoát lỗi `-1073741819`, nên **không tính PASS**. Lượt Direct3D12 sau đó ghi đủ ảnh và exit0. Tool chụp giờ cho chọn API và mặc định D3D12 trên máy này. Windows graphical player vẫn chạy fixture D3D11 và đạt 70 kiểm tra; lỗi Editor capture không được suy ra là mọi player đều lỗi.

## Tích hợp và giới hạn

Bản cũ được giữ ở `Builds/CanThoBackups/20261016-SchematicLayout`. Việc tích hợp chỉ thay model/layout, collider/markers trong scene campus, NavMesh, material mapping và menu version; giữ GUID. Protocol tăng14 vì bố cục/collider đã đổi và không nên ghép client map cũ vào cùng trận.

Không bổ sung hệ thống chip/class/quest mới, enemy toàn campus, nội thất, NPC, giao thông hoặc save campaign. Asset budget đạt không phải số đo FPS. F8/F9 vẫn có để đo trên từng máy; cần chạy cùng build mới qua Relay để đánh giá frame time/RTT và camera tại các góc người chơi tự chọn.

Hướng dẫn và bố cục: [CAN_THO_SCHEMATIC_LAYOUT.md](CAN_THO_SCHEMATIC_LAYOUT.md).
