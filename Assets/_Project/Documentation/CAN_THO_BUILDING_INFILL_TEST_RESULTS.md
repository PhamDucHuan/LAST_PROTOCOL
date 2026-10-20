# Kiểm tra nhà bổ sung trên đất cũ — revision9 / Protocol19

Hoàn thiện ngày 20/10/2026. Ứng viên: `Builds/CanThoMap/20261020-BuildingInfill/Candidate01`. Các lượt Unity chạy trên bản sao `Builds/ValidationProject`; bộ asset đã kiểm tra được tích hợp vào project chính, giữ GUID model/scene/NavMesh/menu.

## Kết quả thực tế

| Hạng mục | Kết quả | Bằng chứng trong thư mục task |
|---|---|---|
| Blender/FBX | PASS, process exit 0; 50.940 mesh nguồn, 450 renderer, 598.989 tam giác, 2.345 collision box | `Blender01.log`, `Candidate01/BuildStats.json` |
| Nhà bổ sung | PASS: thêm 1.100, tổng 1.351 nhà; 129 nhà 1 tầng, 459 nhà 2 tầng, 442 nhà 3 tầng, 70 nhà 4 tầng | `Candidate01/CanThoCampus.layout.json`, `InfillPlan.json` |
| Phạm vi đất | PASS: giữ nguyên nền 940 × 840 m và world bounds, không thêm đường hoặc hẻm | `PreservationFinal.json`, `LayoutValidation.json` |
| Bảo toàn | PASS: 49.503 mesh cũ giữ nguyên geometry/UV/material slot/transform; collider cũ, 251 nhà cũ, đường, mặt nước và route không đổi | `PreservationFinal.json` |
| Ô chức năng campus | PASS: nhà mới không chồng lên 13 ô sân/khu chức năng trong schematicPlots | `LayoutValidation.json` |
| Lô nhà | PASS: nằm trong nền cũ, tách các lô cũ/mới, chừa đường chính/hẻm/tuyến chơi, ngoài campus và vùng nước có khoảng lùi | `PreservationFinal.json` |
| Texture | PASS: 23 PNG portable/packed, gồm 21 ảnh cũ và 2 atlas 2048 mới; shader URP/Lit, normal map linear, UV có dữ liệu | `PreservationFinal.json`, `CanThoBuildingInfillTests` |
| Import/collider/NavMesh | PASS, process exit 0 | `Refresh.log`, `Refresh-ExitCode.txt` |
| Unity EditMode | **126/126 PASS**, không fail/skip, process exit 0 | `EditModeFinal.xml`, `EditModeFinal.log` |
| Đường đi mới | PASS: 69 điểm giữa các dãy nhà có đường NavMesh hoàn chỉnh từ campus; tâm 1.100 nhà không walkable | `AddedCourtyardsConnectToCampusAndInteriorsStayBlocked` trong XML |
| Collider/nền | PASS: mỗi nhà mới có collider riêng đúng kích thước/góc; bốn góc nằm trên nền cũ; kiểm tra nhà cũ tiếp tục đạt | `AddedBuildingsStayOnOriginalGroundAndHaveDistinctColliders` và các test Neighborhood |
| Hồi quy | PASS: các tuyến capsule/hẻm cũ, cổng/rào/spawn, camera, landmark, nước và installer idempotent | Các test Route/Neighborhood/Streetscape/Fence/Architecture |
| Windows build | PASS, process exit 0 | `WindowsBuild.log`, `WindowsBuild-ExitCode.txt` |
| Co-op localhost | **6/6 tiến trình PASS**: Host+3Client; người thứ5 bị từ chối; phòng đã bắt đầu từ chối người mới | `RuntimeEvidence/MissionSmoke/Summary.json` |
| Phiên/nhiệm vụ | PASS: di chuyển/xoay/chạy/nhảy, chọn màn/Ready, Recon chung, completed/wipe/restart và Host disconnect | JSON/log Host và ba Client trong MissionSmoke |
| Runtime đồ họa | **95/95 PASS**, complete=true, không failure, process exit 0 | `RuntimeEvidence/GraphicalPreview/Checks.json`, `Player.log` |
| Ảnh Unity | Đủ 21 PNG; đã xem tổng thể, sơ đồ và cụm nhà/mặt tiền. **Tiện ích Editor capture bị lỗi khi thoát** sau khi lưu ảnh | `UnityCaptureFinal/`, `CaptureFinal.log`, `Capture-D3D12.log` |
| Tích hợp | PASS: 230 asset/meta đúng hash, 70 file C# khớp bản validation; 8 file được bảo vệ giữ nguyên | `PromotedAssets.json`, `FinalVerification.json` |
| Nguồn chỉnh sửa | PASS: blend/layout khớp ứng viên; hai atlas mới được chép vào Textures của nguồn Blender và đối chiếu hash | `FinalVerification.json`, `ArtSource/Blender/CanThoCampus` |
| Đóng gói | PASS: 376 file game, manifest/hướng dẫn; đọc lại toàn bộ entry ZIP và so sánh hash/size | `Package.log`, `FinalVerification.json` |
| Relay/FPS máy nhóm | **CHƯA KIỂM THỬ cho Protocol19** | Cần chạy cùng gói mới trên các máy thật |

95 kiểm tra runtime bao gồm menu/chọn màn/Ready/Deploy/return, bộ lấy mẫu FPS, camera, agent trên tuyến campus và23 chặng ngoại vi, nhiệm vụ và HUD. Các khoảng giữa nhà mới được kiểm tra bằng NavMesh/collider; chưa có lượt bốn người chiến đấu trong từng ô phố mới. Smoke này không bật riêng fixture combat/enemy/revive, nên các trường mặc định false của chúng không phải kết quả kiểm thử lại combat.

## Các vấn đề và cách xử lý trong lượt này

- Lượt EditMode đầu đạt 125/126: một test cũ chỉ cho phép tên material CT2/3/4/5 nên chưa nhận CT9. Đã thêm họ CT9 vào danh sách, giữ điều kiện shader/texture/collider; chạy lại đạt 126/126. Bằng chứng đầu lưu ở `EditMode-Initial.xml/log`.
- Tôi chạy smoke và graphical preview cùng lúc ở lần đầu, khiến preview không bind được cổng 7777 và fixture báo lỗi tiếp theo. Đã lưu log `Graphical-PortCollision-*`, cho smoke kết thúc rồi chạy riêng graphical preview; lượt cuối đạt 95/95, exit 0. Không sửa gameplay để xử lý lỗi sắp lịch kiểm thử này.
- Editor capture ở cả D3D12 và D3D11 đều đã lưu đủ 21 ảnh rồi thoát với mã **-1073741819 (0xC0000005)** sau thông báo shutdown. Chưa xác định nguyên nhân native. Đây là lỗi tiện ích chụp Editor; không coi nó là exit 0 hoặc lỗi đã sửa. Import, test, build, smoke và Windows graphical player cuối đều kết thúc thành công. Trạng thái tổng hợp là `REQUIRED_CHECKS_PASS_WITH_CAPTURE_UTILITY_FAILURE`.

## Gói chơi

`Builds/Distributions/LAST_PROTOCOL-Campus-Protocol19-BuildingInfill-20261020.zip`

- Dung lượng:102.959.541 byte.
- SHA256:`6FC7CF2C65DC0C0F77858C14BE3BCD9C57F1CAA9A45D4D17AACF51EB1B8535C5`.
- Host/Client dùng cùng Protocol19, giải nén đầy đủ và tạo Join Code mới.

Nhà được dựng theo quan sát quan hệ/mật độ trên Google Maps, với tỷ lệ gameplay và mặt tiền thủ tục; chưa phải thửa nhà đo đạc 1:1, chưa có nội thất. Budget tam giác/renderer không chứng minh FPS ổn định trên máy thật.
