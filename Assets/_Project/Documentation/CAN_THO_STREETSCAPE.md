# Ngoại vi và bờ hồ — revision8 / Protocol18

Đợt hoàn thiện ngày 17/10/2026 bổ sung mật độ dân cư và tiện ích ngoài campus. Nguồn editable: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`; layout tương ứng nằm cùng thư mục. Không cần cài package Unity hoặc tải model bên thứ ba.

## Nội dung

- Thêm 84 nhà, tổng 251 nhà ngoại vi. Nhà mới có mặt tiền, cửa cuốn, cửa sổ trước/sau/bên hông, ban công, mái, bồn nước và pin mặt trời; không có nội thất.
- Bảy hẻm dân cư rộng 4 m nối với mạng đường chính. Đây là hẻm và thửa nhà mô phỏng cho gameplay, không phải địa chỉ hay kích thước đã khảo sát ngoài thực địa.
- Thêm 185 cây bóng mát dùng texture lá alpha sẵn có; thân có collider, tán không cản người chơi.
- Bờ hồ có thêm 12 ghế nghỉ và viền đá; thêm 69 đèn năng lượng mặt trời dọc phố và quanh hồ. Đèn là hình học tĩnh có chi tiết phát sáng, không thêm hàng chục realtime Light.
- Rãnh thoát nước phẳng, tấm pin và đèn trạng thái tạo điểm nhấn tương lai, giữ tỷ lệ nhà phố Việt Nam.

Giữ nguyên 167 nhà cũ, toàn bộ mesh/collider cũ, 40 landmark campus, cổng/rào, nhiệm vụ CTU_00_RECON, sáu tuyến đường tham chiếu, mặt nước và tuyến kiểm thử ngoại vi 23 chặng. Không đổi scene MainMenu, MultiplayerTest, ContainmentYard hoặc package/ProjectSettings.

## Dựng lại và tích hợp

`Tools/Blender/PlanCampusStreetscape.py` lập phương án từ layout revision7, dùng khoảng lùi và kiểm tra footprint để tránh campus, nước, đường chính và các lô có sẵn. `CompleteCampusStreetscape.py` mở nguồn revision7 rồi ghi thư mục ứng viên mới, không sửa nguồn đầu vào. Không chạy lại trên nguồn revision8; lấy revision7 trong backup nếu muốn tái tạo.

`ValidateStreetscapePreservation.py` đối chiếu mesh/transform, toàn bộ collider cũ,167 nhà cũ và dữ liệu đường/campus; kiểm tra footprint nhà, hành lang hẻm và budget 500 renderer/600.000 triangle. Bản xuất có 430 renderer,581.597 triangle và 1.245 collision box. Collection64/65/66 chia nhóm theo vùng và vật liệu để giữ số renderer hữu hạn.

`SealPeripheralBends.py` đọc ứng viên đầu và xuất ứng viên thứ hai, bổ sung 58 mặt vá bo tròn tại các khúc cua. Mục đích là che kín các khe tam giác giữa hai đoạn mặt đường/vỉa hè ghép, không thay đổi collider hay đường tâm tham chiếu.

`CanThoStreetscapeTests.cs` kiểm tra NavMesh của từng hẻm, capsule đi cả hai chiều và collider của cây/ghế/đèn. Các bài kiểm tra campus, camera, enemy navigation và multiplayer hiện có tiếp tục được chạy. Bằng chứng và kết quả thực tế: `CAN_THO_STREETSCAPE_TEST_RESULTS.md`.

Trong Unity mở scene CanThoCampus sau khi import hoàn tất, hoặc chọn màn campus từ menu. Nếu đang mở scene campus từ trước lúc cập nhật, thoát Play và mở lại scene để nạp nội dung mới. Chỉ dùng `LastProtocol/Maps/Build Windows Campus Streetscape` khi muốn tự build lại; không cần chạy công cụ dựng lại scene đã tích hợp.

## Chạy nhóm

Dùng cùng gói **Protocol18** trên tất cả máy. Host tạo phòng mới, gửi Join Code mới cho Client; mọi người Ready rồi Host Deploy. Bản17 trở về trước bị từ chối do khác protocol. Gói ZIP chứa toàn bộ thư mục game, hướng dẫn và manifestSHA256; phải giải nén đầy đủ trước khi mở EXE.

Kiểm tra thêm các hẻm mới ở rìa Nguyễn Văn Cừ, phía ngoài Búng Xáng, Trần Văn Hoài và Mậu Thân; đi dọc bờ hồ rồi về cổngB. Luân phiên Host trên các máy, quan sát FPS/RTT/loss và thử mất kết nối. Kết quả localhost không thay thế kiểm thử RelayInternet.

Mạng đường nền được dựng tay theo quan hệ vị trí đã quan sát trên GoogleMaps ở revision7, nén vào sơ đồ campus. Xem `ArtSource/References/CanThoCampus/REFERENCES_PERIPHERY.md` và `Design/Maps/CanThoCampusII/PERIPHERAL_PLAN.json`. Không phải bản sao1:1, chưa có cao độ đo đạc, giao thông động, cửa hàng có thật hay nội thất. Thêm cây có thể tăng overdraw; FPS trên các máy mục tiêu cần đo với gói mới.
