# Kết quả hoàn thiện tuyến campus — 2026-10-16

Art revision 3, campusFenceRevision 1, protocol 11. Source: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`. Unity 6000.5.4f1; Windows Development build dùng URP 17.5.0.

## Kết quả cuối

| Kiểm tra | Kết quả |
| --- | --- |
| Đối chiếu | Đã xem Google Maps lớp vệ tinh tại campus ở mức 100 m / 50 m và ảnh nhà điều hành, Học liệu, cổng; nguồn được ghi trong REFERENCES.md |
| Blender | PASS; source/FBX/layout mới hợp lệ; 12.926 object nguồn, 236 renderer, 317.384 triangle, 292 collider |
| C# / Windows build | PASS; Unity exit 0, `Build Finished, Result: Success` và Windows development build PASS |
| EditMode | **88/88 PASS** trên bản candidate đã sửa; gồm ba test mới cho tuyến, capsule và camera, cùng các test hồi quy cũ |
| Capsule Player | PASS; CharacterController đọc cùng height/radius/center/stepOffset với prefab, đi hết các waypoint và còn nền đỡ |
| NavMesh | PASS; đường giữa các chặng hoàn chỉnh, giới hạn độ vòng tránh; cổng và landmark cũ vẫn tới được, nhà/rào/obstacle được loại khỏi vùng đi |
| Camera pipeline | PASS tại tường nhà điều hành, mặt cong Học liệu và hàng rào: phát hiện vật cản, thu camera và không overlap môi trường với sphere radius 0.2 m |
| Runtime graphical preview | **46/46 PASS**, không failure; Agent kích thước Enemy chạy thật qua bảy chặng, ba camera probe, menu/chọn màn/Ready/chuyển scene/quay lại menu |
| Multiplayer localhost | **6/6 mục Summary PASS**: Host, ba Client, người thứ năm bị Room Full, phòng đã Start bị Room Closed |
| Gameplay regression | Bốn Player cùng vào CanThoCampus; di chuyển/xoay/sprint/jump, HP/down/reset, combat/target/reset/cadence/friendly fire và Host ngắt kết nối được quan sát trong smoke |
| Hình ảnh | Đã xem cổng B, toàn cảnh, sân nhà điều hành, Học liệu; ba ảnh camera runtime không có cảnh camera nằm trong khối tường. Có bộ tám ảnh Unity và ba ảnh camera runtime |
| Bảo toàn dữ liệu | Giữ GUID map/scene/NavMesh/menu; sáu asset bảo vệ giữ hash: MainMenu, MultiplayerTest, ContainmentYard, SampleScene, NetworkPlayer và OperationsMenu cũ |

Tất cả Editor batch chạy ở `Builds/ValidationProject`. Không điều khiển/chuyển scene hiện tại trong Editor của người dùng. Chỉ tích hợp asset môi trường đã qua kiểm tra sau khi đối chiếu hash với backup.

## Những lỗi được phát hiện và xử lý

- Candidate đầu có lối đi sát cột portico; capsule thực tế bị cản. Đã đổi đường qua sân trước và vòng ngoài cột canopy Học liệu, không bỏ qua kiểm tra collision.
- Test hàng rào cũ đặt điểm bên trong đúng vị trí bãi xe vừa thêm. Đã dời điểm đo ra phần trống trên cùng đoạn rào; vẫn kiểm tra đủ bốn đoạn mặt trước và raycast phủ bảy collider rào.
- Một cây được dời ra khỏi tuyến/điểm đo. Test camera đạt ở các candidate; dữ liệu NavMesh/collider cuối được bake lại.
- Công cụ cập nhật marker ban đầu gặp các tên landmark trùng nhau. Đã giới hạn việc dời marker vào đúng nhà điều hành, giữ các marker còn lại.
- Nguồn Blender cũ đã bỏ vật liệu GrassLight không dùng; script tái tạo nó theo palette trước khi dựng cảnh quan mới. Không tải/cài thêm package hoặc model.

Các lượt candidate chưa đạt được giữ trong log/history; không dùng chúng làm kết quả PASS. Các thay đổi cuối sau lượt EditMode đạt chỉ bổ sung kiểm tra đầu vào rỗng cho công cụ/harness và chỉnh vị trí camera chụp; scene/mesh/NavMesh đã kiểm tra giữ hash khi tích hợp. Build và runtime preview sau đó được chạy trên mã cuối.

## Bằng chứng

- `Builds/ValidationResults/CampusRouteFinal.xml` và `.log`: 88/88.
- `Builds/ValidationResults/CanThoBuild.log`: build cuối.
- `Builds/ValidationResults/CanThoMenuPreview/Checks.json`: 46/46; ảnh Campus-Camera-Wall-0..2 và menu/gameplay.
- `Builds/ValidationResults/LevelSmoke2/Summary.json`: 6/6; JSON/log Host và Client.
- `Builds/ValidationResults/CanThoMapPreview/20261016-090845/`: tám góc cuối. Bộ trước ở `20261016-090122` có góc Học liệu rộng hơn để đối chiếu.
- `Builds/ValidationResults/RegressionRuns/20261016-CanTho-Route-final/`: snapshot bằng chứng, báo cáo và ArtifactHashes.json.
- `Builds/CanThoRoute/20261016-Route-v3/`: nguồn candidate cuối và BuildStats.json.
- `Builds/CanThoBackups/20261016-BeforeCampusRoute/`: source/asset trước thay đổi, BeforeHashes.json và IntegratedHashes.json.

## Phạm vi chưa kiểm thử

**Relay trên nhiều máy thật: CHƯA KIỂM THỬ.** Môi trường hiện tại chỉ cho chạy trên máy cục bộ; không có phiên truy cập các máy Client khác. Localhost không phải bằng chứng đã thử Internet. Hướng dẫn phối hợp nhiều máy ở `CAN_THO_PLAYABLE_ROUTE.md`; mọi máy cần cả thư mục build protocol 11.

Chưa kiểm thử mọi góc yaw/pitch hoặc camera sát mọi vật thể, benchmark FPS/GPU trên máy mục tiêu, Enemy AI giao chiến khắp campus và hồi sinh trên mọi góc địa hình. Runtime route probe dùng Agent theo thông số kích thước Enemy, tăng tốc để kiểm tra đường đi; không thay AI gốc thành tuần tra toàn tuyến. Robot bị cô lập trong combat smoke để kiểm tra HP/damage ổn định. Không đánh dấu các trường hợp chưa chạy là PASS.

Độ giống thực tế được đánh giá qua ảnh/bố cục nhìn thấy; chưa có đo đạc thực địa, photogrammetry hoặc kiểm chứng từng kích thước. Model vẫn là bản dựng game stylized có tỷ lệ nén; các khu ngoài tuyến có chi tiết thấp hơn.
