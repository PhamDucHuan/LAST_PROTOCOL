# Cần Thơ — tuyến cổng B / nhà điều hành / Học liệu

Ngày đối chiếu: 2026-10-16. Unity 6000.5.4f1, URP 17.5.0, Blender 5.2.2 LTS. Art revision 3; hàng rào revision 1 được giữ; protocol 11.

## Cơ sở dựng hình và độ chính xác

Đã mở [Google Maps tại Trung tâm Học liệu](https://www.google.com/maps/place/Learning+Resource+Center+of+Can+Tho+University/@10.0301788,105.7713541,846m/data=!3m2!1e3!4b1!4m6!3m5!1s0x31a0882326b41949:0xf05727b365e7d457!8m2!3d10.0301788!4d105.7713541!16s%2Fg%2F1tfs8bkb), xem lớp vệ tinh và hai mức tỷ lệ 100 m / 50 m. Đối chiếu thêm [ảnh và giới thiệu chính thức của trường](https://www.ctu.edu.vn/gioithieu/cac-khu.html), ảnh mặt ngoài Học liệu trong trang AUNILO và ảnh cổng đã lưu ở ArtSource/References. Nguồn chi tiết được ghi ở `ArtSource/References/CanThoCampus/REFERENCES.md`.

Tái dựng tập trung vào tương quan các công trình, màu khung xanh xám, khối phụ thấp tầng, sảnh/cột, hệ lam Học liệu, bia đá và cảnh quan có cây che bóng. Không đưa bản đồ, ảnh vệ tinh hoặc ảnh người dùng Google vào texture/model của game. Mesh và vật liệu được dựng riêng.

**Đây là mô hình game có tỷ lệ nén, không phải bản đo đạc 1:1.** Kích thước công trình, vị trí từng cây, bãi xe, đồ sân và một số chi tiết mặt đứng được ước lượng. Ngoại cảnh ngoài tuyến vẫn có độ chi tiết thấp hơn. Chưa có nội thất chơi được. Thiết bị MEKONG, vạch dẫn sáng, trạm điện và cổng quét là phần hư cấu tương lai; không mô tả hiện trạng thật của trường.

## Nội dung mới

- Nhà điều hành: khung xanh xám, cửa sổ chia ô, ban công, khung mở trên mái, hai khối phụ thấp tầng, sảnh có cột, biển tên và bia đá trước sân. Khối chính dịch 4 m sang phía đông trong tọa độ game để giữ khu thử combat.
- Học liệu: mặt đứng cong, vỏ nhôm chia tấm so le, lam đứng/ngang, cửa kính, biển tên, canopy và cột trước sảnh.
- Cổng: nhà bảo vệ có mái trắng phẳng, khung kính/grille; giữ các lối A/B/C, hàng rào và đường 3/2 ngoài khuôn viên.
- Tuyến: sân lát gạch, mạch lát, vỉa hè, cây tán nhiều cụm, ghế, bãi xe có mái, xe máy tĩnh, biển chỉ dẫn và thiết bị MEKONG nhỏ ở rìa sân.

Toàn map: 12.926 object nguồn, 236 renderer sau gộp theo collection/vật liệu, 317.384 triangle, 292 collider hộp. Các cụm lá mới giữ smooth shading qua FBX; các khối kiến trúc giữ mặt phẳng. Số liệu này chưa phải benchmark FPS.

## Tuyến và kiểm tra vật lý

Trục X chạy dọc mặt đường; +Z đi vào campus. Các waypoint được lưu trong sidecar và nhóm `Playable Campus Route` trong scene, có thứ tự:

| Điểm | X | Z | Vai trò |
| --- | ---: | ---: | --- |
| 01 | 0 | -54 | Ngoài cổng B |
| 02 | 0 | -40 | Sân vào, gần bốn spawn |
| 03 | 0 | -24 | Rẽ khỏi trục chính |
| 04 | 12 | -24 | Lối trước sân |
| 05 | 32 | -21 | Trước nhà điều hành |
| 06 | 40 | -22 | Rẽ về Học liệu |
| 07 | 40 | -44 | Tránh trụ canopy Học liệu |
| 08 | 57 | -42 | Trước sảnh Học liệu |

Giữ vị trí bốn spawn và khu mục tiêu bắn hiện có. Capsule test đọc trực tiếp height/radius/center/stepOffset từ NetworkPlayer prefab và dùng CharacterController.Move qua cả tuyến. NavMesh được bake từ collider giản lược, giữ lối cổng, chặn khối nhà và rào.

Camera campus dùng CinemachineThirdPersonFollow, collision layer Default, IgnoreTag Player, radius 0.28 m, xử lý đi vào vật cản ngay và thoát dần; near clip 0.08 m. Test pipeline Cinemachine thật ở ba vị trí: tường nhà điều hành, mặt cong Học liệu và hàng rào. Chưa coi ba vị trí đó là bằng chứng mọi góc xoay/pitch đều đã kiểm thử.

## Công cụ và cách chạy lại

| File | Vai trò |
| --- | --- |
| Tools/Blender/RefineCampusRoute.py | Đọc nguồn revision 2 có rào, dựng revision 3 vào thư mục mới; từ chối output tồn tại hoặc sai nguồn |
| Tools/Blender/FinishCanThoCampus.py | Gộp mesh theo khu/vật liệu, giữ UV và cờ smooth, xuất FBX |
| Scripts/Editor/CanThoDetailSetupTool.cs | Hỗ trợ revision 2/3, sao lưu, thay nhóm môi trường/collider do công cụ sở hữu, bake NavMesh, cấu hình camera và waypoint; Apply bình thường chạy lại không nhân bản |
| Tests/EditMode/CanThoRouteTests.cs | Kiểm tra đường NavMesh, capsule đi hết tuyến và camera sát tường |
| Scripts/UI/Multiplayer/CampusRoutePreview.cs | Kiểm tra NavMeshAgent chạy thật và chụp camera trong Development build khi bật cờ rõ ràng |

Preview có Agent dùng radius/height/type của robot, chạy qua waypoint trong scene. Nó không thay AI gameplay thành tuần tra toàn campus; logic leash/chase/melee của robot cũ được giữ. Harness chỉ được gọi với `--lp-campus-route-preview` kết hợp menu preview, không chạy trong chơi bình thường. Host fixture dùng localhost, không đăng nhập hoặc gọi Relay.

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage CanThoBuild
.\Tools\Run-MenuPreview.ps1 -Player 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\CanThoPlayer\LAST_PROTOCOL.exe' -Level 2 -CampusRoute
.\Tools\Run-MultiplayerSmoke.ps1 -Player 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\CanThoPlayer\LAST_PROTOCOL.exe' -LevelTest -Level 2 -CombatTest
.\Tools\Run-CanThoCapture.ps1
```

Các batch chỉ chạy ở `Builds/ValidationProject`. Nguồn trước nâng cấp ở `Builds/CanThoBackups/20261016-BeforeCampusRoute/`. Các candidate và sidecar không ghi đè nhau, nằm trong `Builds/CanThoRoute/`.

## Kiểm thử nhiều máy qua Relay

1. Sao chép **cả thư mục CanThoPlayer** protocol 11 sang từng máy Windows. Không chỉ gửi EXE.
2. Host chọn CAN THO / CAMPUS II, Create Room và lấy mã mới. Client dùng Join Room với mã đó; xác nhận số người tăng đến 4/4.
3. Mọi người Ready, Host Deploy. Đối chiếu scene/map, bốn spawn và chỉ Owner điều khiển nhân vật của mình.
4. Đi lần lượt theo tuyến. Đối chiếu vị trí Player từ máy khác, thử đi sát rào, trụ và khối nhà; xoay camera khi đứng sát mặt đứng.
5. Thử bắn robot/mục tiêu và hồi sinh đồng đội trong khu thử. Kiểm tra Client rời rồi Host kết thúc phòng. Lưu log Host/Client nếu lỗi.
6. Thử thêm một Client để xác nhận Room Full; thử mạng khác nhau, chẳng hạn một máy dùng hotspot, để có bằng chứng kết nối Internet.

**Chỉ đánh dấu PASS sau khi thực hiện trên các máy thật.** Nhiều tiến trình trên cùng PC không thay thế việc kiểm thử này. Bản protocol 10 trở xuống sẽ bị từ chối nhằm tránh lệch collider/map.
