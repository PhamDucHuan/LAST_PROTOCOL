# Hàng rào khuôn viên Cần Thơ

Ngày 2026-10-15; Unity 6000.5.4f1, Blender 5.2.2 LTS. Art revision 2, campusFenceRevision 1, protocol 10.

## Phạm vi thay đổi

Thêm bốn đoạn hàng rào mặt trước, nối vào trụ cổng A/B/C và ba đoạn tây/bắc/đông trên tuyến tường mép bản đồ. Các khoảng mở giữa trụ cổng rộng 16 m; hàng rào không cắt đường 3/2 phía ngoài. Giữ các công trình, biển trường, nhà bảo vệ, điểm spawn và khu thử combat.

Hàng rào có bệ bê tông, thanh kim loại, trụ và nắp ceramic, dải cyan. Dùng năm vật liệu CT2 đã có; mesh được gộp theo collection/vật liệu. Bảy collider hộp liên tục cao 3.54 m ngăn đi xuyên khe rào. Collider giản lược cũng chặn đạn qua các panel; chưa có cửa chuyển động hoặc phá hủy rào.

Toàn map sau cập nhật: **11.286 object nguồn, 218 renderer, 260.064 triangle, 233 collider hộp**. Đây là số liệu asset, chưa phải benchmark hiệu năng.

## Kiểm tra đã thực hiện

- Blender xuất source, FBX và JSON: PASS. Script từ chối nguồn đã có rào hoặc output tồn tại để tránh tạo trùng/ghi đè.
- Unity nhập model, remap vật liệu URP, cập nhật collider và bake NavMesh trên bản sao validation: PASS.
- EditMode toàn bộ project: **85/85 PASS**, Unity exit 0. Có ba test mới: raycast phủ bảy đoạn rào; khoảng mở cổng/đường ngoài; đường NavMesh đi trong đúng từng cổng và không cắt panel rào.
- Windows Development build: PASS, log `Build Finished, Result: Success`, Unity exit 0. Build ở `Builds/ValidationProject/Builds/CanThoPlayer/`.
- Smoke trên build mới: **6/6 mục PASS** (Host, ba Client, người thứ năm, phòng đã đóng). Xác nhận cùng tải CanThoCampus, Host chọn màn/Ready reset, movement/rotation/sprint/jump, HP/down/reset và combat regression trên bốn Player; Client nhận Host ngắt kết nối. Đây là localhost, không phải bốn máy qua Relay. Robot được cô lập trong combat smoke để không làm nhiễu kiểm tra.
- Hình ảnh: đã xem ảnh Unity sát hàng rào mặt trước, cổng B và toàn cảnh. Sáu góc chụp được lưu trong thư mục bằng chứng.
- GUID model/scene/NavMesh/menu được giữ. Hash MainMenu, hai sân test cũ, SampleScene, NetworkPlayer và OperationsMenu cũ giữ nguyên so với backup trước bổ sung rào.

Một test ban đầu dùng NavMesh.Raycast thẳng tại đúng x=0 báo cản; kiểm tra CalculatePath sau đó xác nhận đường hoàn chỉnh qua cổng B, hai đầu và các góc đều trong khoảng mở. Test cuối kiểm tra đường đi thực tế và giới hạn vị trí góc, không bắt buộc kết quả raycast thẳng phải trống. Không thay dữ liệu NavMesh để né kết quả test này.

Lượt chụp đầu hết giới hạn 120 giây trong lúc Unity biên dịch lại assembly. Giới hạn được tăng lên năm phút; lượt kế tiếp xuất ảnh đầy đủ và exit 0.

## Bằng chứng và khôi phục

- Nguồn đang dùng: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend` và sidecar cạnh nó.
- Output Blender trước tích hợp: `Builds/CanThoFence/20261015-Fence-v1/`.
- Backup: `Builds/CanThoBackups/20261015-201417-BeforeFence/`; có hash trước/sau tích hợp.
- `Builds/ValidationResults/EditMode.xml`, `Tests.log`: lượt 85/85.
- `Builds/ValidationResults/FenceGatePaths.xml`, `FenceGatePaths.log`: ba test riêng cho rào và đường qua cổng.
- `Builds/ValidationResults/CanThoMapPreview/20261015-203423/`: ảnh Unity cuối và log.
- `Builds/ValidationResults/CanThoBuild.log`, `LevelSmoke2/Summary.json`: build cuối và smoke mới.
- `Builds/ValidationResults/RegressionRuns/20261015-CanTho-Fence-final/`: bản sao bằng chứng cuối, báo cáo và hash asset.

Batch Unity chỉ chạy tại `Builds/ValidationProject`, không chuyển scene hiện tại trong Editor của người dùng. Dừng Play Mode rồi vào lại CAN THO / CAMPUS II từ MainMenu để nạp map mới. Khi kiểm thử nhiều máy, gửi cả thư mục build protocol 10; các build cũ sẽ bị từ chối vì collider môi trường khác nhau.

**CHƯA KIỂM THỬ:** bốn máy thật qua Internet/Relay, chơi tay toàn tuyến hàng rào, mọi góc camera sát rào, FPS/GPU trên máy mục tiêu. Các báo cáo art trước hàng rào được giữ làm lịch sử, không coi kết quả cũ là lượt test mới.
