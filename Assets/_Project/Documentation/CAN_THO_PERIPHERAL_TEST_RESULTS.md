# Kiểm tra ngoại vi theo Google Maps — revision7 / Protocol17

> **Bản hiện tại:** [ngoại vi và bờ hồ revision8 / Protocol18](CAN_THO_STREETSCAPE.md). Thêm nhà trong hẻm, cây, ghế và đèn; giữ mạng đường/campus revision7. Số liệu bên dưới thuộc lịch sử.

Hoàn thiện17/10/2026, nguồn tham chiếu đã xem16/10. Candidate được tích hợp: `Builds/CanThoMap/20261016-PeripheralLayout/Candidate02`. Thực hiện trong `Builds/ValidationProject`; không chuyển scene hoặc đóng Editor của người dùng.

| Hạng mục đã chạy | Kết quả | Bằng chứng trong thư mục task |
|---|---|---|
| Blender export | PASS:39.635 mesh nguồn,360 renderer gộp,514.781 triangle,895 collision box,40 campus landmark | `Candidate02/BuildStats.json`, `Blender02.log` |
| Bảo toàn campus | PASS:27.403 mesh cũ khớp, giữ nguyên roads/plots/fence/gates/landmark/route/collider trong campus | `PreservationFinal.json` |
| Lô nhà | PASS:167 nhà được đặt lại, không chồng nhau, mặt nước, campus hoặc hành lang đường | `PreservationFinal.json` |
| Texture và nước | PASS:21 PNG đóng gói, đường dẫn tương đối; mặt nước hướng lên | `PreservationFinal.json` |
| Bake scene/NavMesh | PASS, process exit0 | `Refresh.log`, `Refresh-ExitCode.txt` |
| Unity EditMode | **120/120 PASS**, không fail/skip, process exit0 | `EditModeFinal.xml`, `EditModeFinal.log`, `EditModeFinal-ExitCode.txt` |
| Sơ đồ đường | PASS: sáu tuyến, các nút nối3/2–Búng Xáng/Trần Văn Hoài/Mạc Thiên Tích, các nhánh tới Mậu Thân và vòng bắc | `CanThoPeripheralTests` trong XML |
| Nước/shoreline | PASS: nước không walkable, đường ven hồ có NavMesh, có rail collision khớp biên | `CanThoPeripheralTests` trong XML |
| Collider/capsule | PASS:167 nhà có collider đúng vị trí/size/yaw, ground liên tục; capsule đi vòng ngoại vi rồi về campus | `CanThoNeighborhoodTests` trong XML |
| Hồi quy chức năng | PASS: spawn, cổng/rào, tuyến Recon, vật liệu URP, camera campus và cài đặt lại an toàn | Các test Map/Fence/Route/Schematic/Architecture/Mission |
| Windows build | PASS, process exit0 | `WindowsBuild.log`, `WindowsBuild-ExitCode.txt` |
| Co-op localhost | **6/6 Summary PASS**: Host+3Client, người thứ5 bị từ chối, phòng đã đóng từ chối người vào thêm | `RuntimeEvidence/MissionSmoke/Summary.json`, JSON/log từng process |
| Graphical runtime | **95/95 PASS**, không failure, process exit0 | `RuntimeEvidence/GraphicalPreview/Checks.json`, `Player.log` |
| Unity render capture | PASS, hai lượt đều exit0; lượt cuối17 PNG, đã xem plan/overview/waterfront/street views | `UnityCaptureFinal/`, `CaptureFinal02.log` |
| Tích hợp | PASS,224 asset/meta đúng hash, giữGUID của scene/model/nav/menu và các file được bảo vệ | `PromotedAssets.json`, `FinalVerification.json` |
| Relay Internet và FPS máy mục tiêu | **CHƯA KIỂM THỬ cho bản17** | Cần cùng gói17 trên ba/bốn máy của nhóm |

95 kiểm tra graphical bao gồm menu/chọn màn/Ready/Deploy/return, lightweight/FPS sampler, agent kích thước enemy đi7 chặng campus và23 chặng ngoại vi,4 camera probe, nhiệm vụ/HUD/hoàn thành/wipe/reset. Agent chạy nhanh trong fixture để rút ngắn thời gian; không đổi tốc độ enemy gameplay. Smoke là direct localhost, không dùng tài khoản Unity Cloud và không coi là Relay Internet.

## Xử lý trong lượt triển khai

- Vật liệu nước có trong palette cũ nhưng đã bị Blender loại khỏi file vì chưa dùng. Đã tái tạo từ đúng thông số palette, không tải vật liệu mới; kiểm tra mặt nước hướng lên trước khi xuất.
- Bài kiểm tra bảo toàn Blender bị gián đoạn ở lượt trước. Đã chạy lại đầy đủ và lấy `PreservationFinal.json` có tất cả kiểm tra đạt; không dùng log chưa hoàn tất làm bằng chứngPASS.
- Ảnh aerial đầu bị sương mù che xa. Chỉ tắt fog cho camera kiểm tra có tênOverview, không đổi fog gameplay; build và capture cuối đều biên dịch/chạy thành công sau chỉnh sửa này. EditMode kiểm tra geometry/navigation đã đạt trước thay đổi camera chụp này; không sửa dữ liệu map sau các test đó.
- Không tái hiện lỗi native shutdown của tiện ích capture ở các lượt hiện tại. Các log lỗi capture của bản16 thuộc lịch sử, không được gộp vào kết quả bản17.

## Giới hạn

Mạng đường là bản dựng tay từ quan hệ vị trí quan sát trên Google Maps, sau đó nén để phù hợp campus sơ đồ hiện có. Từng khoảng cách/thửa nhà/cao độ và bờ nước chưa được đo GPS; hướng Nguyễn Văn Cừ là tuyến dẫn hướng giản lược. Các mảng giữa những tuyến phố vẫn để đơn giản, chưa dựng đầy đủ mật độ dân cư thực tế.

Chưa có bơi/lặn, nội thất, NPC/xe AI, streaming/LOD mới hay enemy wave ngoài trường. 360 renderer/514.781 triangle còn trong budget500/600k, không chứng minh FPS ổn định trên mọi máy. Công cụ FPS sampler trong test chỉ được xác nhận hoạt động; chưa benchmark GPU/CPU/RTT/loss trên máy mục tiêu.

Gói17 có manifest/hash và được đọc lại toàn bộ entry sau nén bằng `Tools/Package-WindowsPlayer.ps1`. Kết quả đóng gói cuối lưu trong `FinalVerification.json`.
