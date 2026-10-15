# Campus Cần Thơ phiên bản chi tiết và tương lai

Art revision 2 giữ bố cục campus và hai sân test cũ, đồng thời dựng lại bốn điểm mốc chính theo ảnh tham khảo. Sau đó bổ sung hạ tầng hư cấu của AI MEKONG để hợp với bối cảnh LAST_PROTOCOL. Đây vẫn là môi trường stylized với tỷ lệ giản lược cho gameplay.

## Điểm thay đổi

| Khu vực | Nội dung mới |
| --- | --- |
| ATL | Lam ngang dày, vách giữa có motif perforation, hệ lam đứng, dải kính bên hông, mái/canopy và khung thiết bị |
| Nhà điều hành | Khung đứng và dải tầng nổi, mặt bên có kính/lam, khung mở trên mái, canopy phát sáng |
| Trung tâm Học liệu | Mặt đứng cong chia panel, dải kính, lam bên, khối biển đứng và điểm nhấn MEKONG |
| Hội trường Rùa | Khối hội trường dài, mặt tiền tam giác với hệ kính đứng, mái lớn có sườn, canopy vào sảnh, clerestory bên hông; thay mái tròn của prototype |
| Ba cổng | Biển trường Việt/Anh trên khối trắng, ốp ceramic/kim loại, cổng quét và camera tĩnh, khe sáng cyan |
| Các khối còn lại | Khung kỹ thuật, hộp thiết bị mái, bảng service và đường conduit |
| Đường và cảnh quan | Biển dẫn đường, đèn dẫn tuyến, hai bến xe thông minh, một xe điện tĩnh, bồn cây, hố ga và tán cây nhiều cụm/nhánh |
| MEKONG | Tháp dữ liệu có cụm làm mát và anten, console và dải sáng; chỉ là môi trường tĩnh, chưa có gameplay hack hoặc AI điều khiển xe |
| Unity | Vật liệu native URP mới, bloom nhẹ, ACES, SMAA, ánh sáng/sky/fog riêng scene, collider và NavMesh được cập nhật |

Các ảnh tham khảo và nguồn được ghi ở `ArtSource/References/CanThoCampus/REFERENCES.md`. Ảnh được xem để đối chiếu kiến trúc, không làm texture trong game. Màu, thiết bị và hệ thống tương lai là phần sáng tạo của game.

## Asset và công cụ

Nguồn đang dùng: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`. Model và sidecar giữ đường dẫn cũ trong `Assets/_Project/Art/Environments/CanThoCampus/` để scene vẫn ở mục **CAN THO / CAMPUS II**. `.meta` được giữ để bảo toàn GUID.

`Tools/Blender/DetailCanThoCampus.py` nâng nguồn revision 1 thành revision 2 trong một thư mục output mới. `RefineCanThoDetail.py` bổ sung mặt bên/góc camera cho candidate đầu tiên; các thay đổi đó cũng đã được đưa vào script dựng detail để tái tạo sau này. Các script giữ đầu vào, không ghi đè trực tiếp file đang đọc.

`CanThoDetailSetupTool` có menu **LastProtocol → Maps → Apply Can Tho Detailed Art Revision**. Lần đầu nó sao lưu scene/NavMesh/menu, thay nhóm model và collider do công cụ sở hữu, giữ các root khác, bake lại NavMesh, thêm profile/sky riêng và đánh dấu `LevelEnvironment.artRevision = 2`. Khi đã ở revision 2, chạy lại chỉ kiểm tra. Công cụ từ chối Play Mode hoặc campus đang mở có thay đổi chưa lưu. Nó không tự cập nhật mọi chỉnh sửa art sau khi revision 2 đã được áp dụng.

Vật liệu mới có tiền tố `CT2_`, còn các vật liệu `CT_` của bản đầu được giữ. Profile ở `Settings/CanThoFuturePostProcessing.asset`; sky ở `Materials/CanThoCampus/CanThoFutureSky.mat`. Chúng chỉ được tham chiếu bởi scene campus, không đổi Graphics/Quality/Render Pipeline của project.

Phản xạ nền dùng cubemap gradient procedural tự tạo ở `Settings/CanThoSkyReflection.cubemap`, giúp kính/kim loại nhận màu trời mà không cần HDRI tải ngoài. Profile có exposure +0.55 EV và bloom nhẹ; directional light dùng soft shadows. Bước bổ sung reflection sao lưu scene/profile trước khi áp dụng lần đầu; các lần Apply tiếp theo giữ cấu hình đã có. Đây là phản xạ môi trường giản lược, không phải phản xạ chính xác từng công trình.

Số liệu trước bổ sung hàng rào: **7.128 object nguồn, 213 renderer sau khi gộp theo khu/vật liệu, 210.168 triangle, 226 collider hộp**. Sau bổ sung hàng rào: **11.286 object nguồn, 218 renderer, 260.064 triangle, 233 collider hộp**. Chưa coi số liệu này là benchmark FPS hoặc draw call.

## Hàng rào khuôn viên

Đã thêm hàng rào ở mặt trước (z = -49.5), nối tới trụ cổng C/B/A và tiếp tục dọc ba cạnh tây/bắc/đông. Bệ bê tông, thanh kim loại đứng, trụ ceramic và dải cyan dùng các vật liệu CT2 có sẵn. Cạnh bên/sau nối với tường chắn mép bản đồ. Ba cổng vẫn có khoảng mở 16 m giữa trụ; đường 3/2 và dãy phố ở ngoài hàng rào.

Mỗi đoạn có một collider liên tục cao 3.54 m, tổng cộng bảy collider mới. Các khe giữa thanh rào là chi tiết hình ảnh; collider giản lược chặn cả nhân vật và đạn qua toàn panel. NavMesh được bake lại. Chưa có cửa đóng/mở hoặc phá hủy hàng rào.

`Tools/Blender/AddCanThoFence.py` đọc nguồn revision 2 chưa có hàng rào, xuất sang thư mục mới và đánh dấu `campusFenceRevision = 1` trong sidecar. Script từ chối nguồn đã có hàng rào hoặc output đã tồn tại, tránh tạo trùng. Nguồn trước thay đổi ở `Builds/CanThoBackups/20261015-201417-BeforeFence/`; không đưa nguồn hiện tại qua script thêm rào lần nữa.

Collider khu Học liệu đã được mở rộng tới phần kính cong nhô ra; NavMesh được bake lại và có raycast test riêng cho lỗi đi xuyên phần mặt đứng này. `CanThoDetailRefresh` là bước batch chủ động thay lại nhóm model/collider do công cụ sở hữu khi sửa sidecar, có backup; Apply thông thường vẫn giữ nguyên revision đã áp dụng.

Khi cần tái tạo từ nguồn revision 1 đã sao lưu, `DetailCanThoCampus.py` nhận thêm `--layout <duong dan sidecar revision 1>`; mặc định tìm sidecar cạnh `.blend` rồi mới tìm trong Assets. Nguồn revision 2 hiện tại không được đưa qua bộ nâng revision 1 lần nữa.

## Chơi và build

MainMenu → PLAY CO-OP → CAN THO / CAMPUS II → Create Room. Host và Client cùng Ready, Host Deploy; các thao tác di chuyển/bắn/hồi sinh giữ như trước.

Build Windows vẫn ở `Builds/ValidationProject/Builds/CanThoPlayer/`. Bản có hàng rào dùng **protocol 10** vì collider môi trường đã thay đổi. Mọi máy phải nhận cả thư mục build mới; các bản protocol cũ sẽ bị từ chối để tránh hai máy nhìn thấy môi trường khác nhau.

Để chạy quy trình kiểm tra trên bản sao:

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage CanThoDetail -CanThoArtDirectory '<thu muc output revision 2>'
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage CanThoBuild
.\Tools\Run-CanThoCapture.ps1
```

Tham số art staging chỉ chép FBX/layout vào bản validation. Tích hợp về project chính là bước riêng sau khi kiểm tra, kèm backup và kiểm tra GUID/hash. Bản trước được lưu ở `Builds/CanThoBackups/20261015-183036-BeforeDetail/`.

## Phạm vi kiểm thử

Kết quả bổ sung hàng rào ở `CAN_THO_FENCE_TEST_RESULTS.md`. `CAN_THO_FUTURE_TEST_RESULTS.md` ghi lượt nâng cấp art trước hàng rào; `CAN_THO_MAP_TEST_RESULTS.md` là lịch sử revision 1. Chưa có nội thất chơi được, xe chạy, traffic/NPC, nhiệm vụ mới hoặc tương tác tháp dữ liệu. Các chi tiết mảnh như lam cửa vẫn dùng collider công trình giản lược. Cần thử tiếp camera sát tường và hiệu năng trên phần cứng mục tiêu.
