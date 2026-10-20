# Kiểm tra khu phố campus — revision6 / Protocol16

Ngày 16/10/2026. Candidate cuối: `Builds/CanThoMap/20261016-Neighborhood/Candidate05`. Unity 6000.5.4f1, Blender 5.2.2 LTS. Kiểm tra trong bản riêng `Builds/ValidationProject`, sau đó chép đúng asset được kiểm tra về project chính bằng hash/GUID. Không điều khiển scene hoặc đóng Editor của người dùng.

| Hạng mục đã chạy | Kết quả | Bằng chứng dưới thư mục task |
|---|---|---|
| Blender export cuối | PASS, 39.010 mesh nguồn, 334 renderer gộp, 506.766 triangle, 807 collision box, 40 campus landmark | `Candidate05/BuildStats.json`, `Blender05.log` |
| Bảo toàn campus | PASS, 27.403 mesh cũ khớp, giữ nguyên đường/plot/hàng rào/cổng/landmark/route/collider cũ ngoài biên cần dời | `PreservationFinal.json` |
| Vật liệu nguồn | PASS, đủ21 PNG512×512 đóng gói và đường dẫn tương đối hợp lệ | `PreservationFinal.json` |
| Mái nhà mới | PASS, 84 nửa mái dốc từ nóc xuống mái hiên theo đúng quy ước tọa độ | `PreservationFinal.json` |
| Unity compile/EditMode | **117/117 PASS**, không fail/skip; process exit0 | `EditModeFinal.xml`, `EditModeFinal.log`, `EditModeFinal-ExitCode.txt` |
| Neighborhood NavMesh | PASS, 29 điểm ở ba hẻm/đường ngang/khu ăn uống đều nối vào campus; 167 nội thất kín không walkable | `CanThoNeighborhoodTests` trong XML |
| Collision | PASS, collider khớp167 căn nhà, có ground, biên ngoài không cắt vùng mới; capsule đi tuyến vòng | `CanThoNeighborhoodTests` trong XML |
| Hồi quy campus | PASS, spawn, đường Recon, cổng/hàng rào, camera sát tường, URP vật liệu và cài lại không phá dữ liệu | Các test Map/Fence/Route/Schematic/Architecture/Mission |
| Windows build | PASS, process exit0 | `WindowsBuild.log`, `WindowsBuild-ExitCode.txt` |
| Host +3 Client localhost | **6/6 Summary PASS**, gồm4 process người chơi và2 process kiểm tra RoomFull/RoomClosed | `RuntimeEvidence/MissionSmoke/Summary.json` và log/JSON từng process |
| Graphical Windows runtime | **80/80 PASS**, không failure, process exit0 | `RuntimeEvidence/GraphicalPreview/Checks.json`, `Player.log`, `GraphicalPreview.log` |
| Tích hợp | PASS,224 file asset/meta khớp hash; giữGUID và không có file được bảo vệ thay đổi | `PromotedAssets.json`, `FinalVerification.json` |
| Relay Internet 3/4 máy, RTT/loss và FPS trên máy mục tiêu | **CHƯA KIỂM THỬ cho bản map này** | Người dùng cần chạy cùng gói16 trên các máy |

80 kiểm tra graphical gồm luồng menu/chọn màn/Ready/Deploy/return, công cụ lightweight và FPS sampler, agent kích thước enemy đi7 chặng trong campus và8 chặng ngoài campus,4 camera probe, nhiệm vụ/HUD/wipe/reset. Tốc độ agent cao chỉ để chạy fixture nhanh; không đổi tốc độ enemy gameplay. Smoke dùng direct localhost, không dùng tài khoản Unity Cloud và không được coi là kiểm tra Relay Internet.

## Kiểm tra hình ảnh và lỗi công cụ

- Đã xem ảnh URP của bản cuối: tổng thể khu phố, nhà trọ, hẻm và khu ăn uống; kiểm tra cửa sổ phụ, mái và vị trí mặt tiền. 15 PNG nằm ở `UnityCaptureFinal`.
- Lượt capture candidate03 đạt process exit0. Lượt capture cuối ghi đủ15 ảnh rồi process Editor thoát `-1073741819` sau thông báo quit; log ở `UnityCaptureFinal/Unity.log`. **Process capture cuối là FAIL**, ảnh chỉ dùng để kiểm tra thị giác. Chưa xác định nguyên nhân native shutdown; không coi lỗi tiện ích này là PASS.
- Lượt Apply đầu cũng lưu scene xong rồi thoát native lỗi. Lượt Refresh sau đạt exit0; EditMode cuối, Windows build và Windows graphical đều đạt exit0.
- Candidate đầu có một điểm vào khu ăn uống nằm trong căn nhà cạnh đó. Test phát hiện, đã dời cả quầy/bàn/paving sang phía tây và chạy lại toàn bộ kiểm tra.
- Đã bổ sung mặt hông/mặt sau và sửa chiều dốc mái trước khi kiểm tra/build cuối. Không dùng ảnh/bản build trung gian để đóng gói.

## Phạm vi kết luận

Đã kiểm tra collider, NavMesh, đường vòng, camera và co-op hiện có. Chưa có đợt enemy mới ngoài trường, nội thất, NPC/xe giao thông, LOD hoặc streaming. Số renderer/triangle còn dưới budget500/600k nhưng không chứng minh FPS ổn định trên mọi máy. Mô hình phố lấy cảm hứng ảnh thực tế, bố trí hẻm/nhà do game thiết kế; không phải tái dựng khảo sát1:1.

Gói16 có manifest SHA256, được đọc lại toàn bộ entry sau khi nén bằng `Tools/Package-WindowsPlayer.ps1`. Kết quả đóng gói cuối ghi trong `FinalVerification.json`.
