# Campus Recon kết quả kiểm thử

Ngày 16/10/2026. Unity 6000.5.4f1, NGO 2.13.3, Protocol 13. Đã hoàn thành dữ liệu mission, tiến độ server và HUD cho CTU_00_RECON. Chưa triển khai phần lấy data/chip của CTU_01.

| Kiểm tra | Kết quả |
|---|---|
| Editor Setup và Windows Mission build | PASS, exit 0 |
| EditMode | **107/107 PASS**; gồm 16 case mới về mission |
| Host + 3 Client trên localhost | **6/6 participant result PASS**, gồm người thứ năm và người join muộn bị từ chối |
| Host + 1 Client trên localhost | **2/2 PASS luồng nhiệm vụ**, cùng quan sát ba objective, hoàn thành, failure và restart; chạy trước sửa guard harness cuối |
| Host + 2 Client trên localhost | **3/3 PASS luồng nhiệm vụ**, cùng quan sát ba objective, hoàn thành, failure và restart; chạy trước sửa guard harness cuối |
| Một người ngoài vùng / một người gục | Host xác nhận không tiến nhiệm vụ trong các lượt 2/3/4 người |
| Đang giữ vùng rồi một người rời | Host xác nhận thời gian giữ về 0 |
| Quyền ghi Client | Từng Client xác nhận `CanClientWrite == false` với mission NetworkVariable; không có RPC nhảy objective |
| Hoàn tất, team wipe và reset | Client quan sát result; active mission fail khi wipe; hai lần restart đưa mission về đầu, Attempt tăng |
| Đồ họa D3D11 | **70/70 kiểm tra PASS**, gồm menu, camera campus, NavMesh, LIGHT/NORMAL và mission HUD |
| HUD active/failed/completed, không nhân HUD, cleanup khi Leave | PASS; có ảnh active và complete từ Canvas runtime |
| Scene, Player/Enemy prefab, model, NavMesh, Packages và ProjectSettings thuộc danh sách bảo vệ | Hash không đổi so với trước đợt nhiệm vụ |
| CTU_00_RECON trên 2/3/4 máy vật lý qua Relay | **CHƯA KIỂM THỬ** |
| FPS/RTT của Protocol 13 trên máy người dùng | **CHƯA ĐO** |
| Nhặt data, sửa điện, CONTROL CHIP, waves, save campaign | **CHƯA TRIỂN KHAI**, không đánh dấu PASS |

## Cách kiểm thử và giới hạn bằng chứng

Các test mạng dùng nhiều tiến trình exe Development trên một PC, kết nối loopback. Fixture đưa Player tới vùng bằng API teleport trên Host để kiểm tra quy tắc mission và đồng bộ; không thay thế việc đi tuyến bằng bàn phím trên các máy thật. Đường đi/collider/camera được kiểm tra riêng qua suite campus và graphical preview.

Client kiểm tra cấu hình quyền ghi, không thực hiện một cuộc tấn công bằng binary tùy biến. Quy tắc mission chỉ chạy phía server và không tiếp nhận counter/position từ UI. Việc từ chối tương tác từ xa/nhặt data trùng thuộc module tương tác chưa triển khai, chưa được suy ra là PASS từ các bài này.

Graphical preview dùng fixture solo với minimumPlayersToStart=1 trong RAM. Không thay đổi cấu hình mặc định 2–4 người của bản chơi. Ảnh UI render từ bản sao Canvas runtime để tránh swapchain đen của cửa sổ ẩn; chúng kiểm chứng bố cục HUD, không phải benchmark GPU hoặc ảnh chụp Relay.

Lượt smoke bốn người đầu tiên dừng vì fixture gọi ResetRoundServer để hồi riêng một người gục; API từ chối đúng vì chưa team wipe. Đã đổi fixture sang đường phục hồi server phù hợp và chạy lại thành công. Không nới điều kiện Health hoặc mission để vượt bài thử.

Ở lượt hai người, phép đánh giá ban đầu yêu cầu activeScene cuối cùng vẫn là CanThoCampus nên báo sai khi Client đã về MainMenu do Host thoát. Đã sửa runner dùng cờ sticky loadedSelectedScene và kiểm tra lại **cùng JSON/log gốc**: 2/2 đạt. Không chạy lại hoặc sửa runtime cho việc đánh giá này. Summary trước sửa và Assessment-note.txt được lưu cùng bằng chứng.

Kiểm tra log sau đó phát hiện MissionSmokeScenario còn đọc Room trong vài frame cleanup sau khi Host thoát, tạo NullReferenceException chỉ khi bật cờ smoke test. Đã thêm guard và thêm điều kiện runner thất bại khi log có application exception. Bản cuối `...-r2.zip` được chạy lại lượt bốn người, bao gồm Host exit, với log sạch application exception. Các lượt 2/3 người và graphical preview trước đó vẫn chứng minh chức năng tương ứng nhưng không được coi là chạy lại trên binary r2. Code mission/HUD/asset không thay đổi giữa hai bản; xác nhận hash trong `MissionImplementationUnchangedAfterHarnessCleanup.json`. Bằng chứng trước guard được giữ tại thư mục cùng cấp `20261016-CampusRecon-Protocol13-beforeHarnessCleanup`.

## Bằng chứng cố định

`Builds/ValidationResults/RegressionRuns/20261016-CampusRecon-Protocol13/` chứa:

- `EditMode.xml`, `Tests.log`, `MissionSetup.log`, `MissionBuild.log`.
- `MissionSmoke/`, `MissionParty2/`, `MissionParty3/`: kết quả và log participant.
- `GraphicalPreview/`: Checks.json, Player.log và PNG, gồm `10-mission-active.png` và `11-mission-complete.png`.
- `ArtifactHashes.json`: code, Room prefab, mission asset và DLL runtime đã kiểm thử.
- `Preservation.json`: so sánh với `Builds/MissionBackups/20261016-110710/PreserveHashes.json`.

Hash runtime của bản đóng gói cuối và code/asset nhiệm vụ được ghi trong ArtifactHashes.json; bản trước guard harness có hash riêng ở thư mục lưu lịch sử nêu trên. Dữ liệu backup giữ Room prefab trước khi thêm NetworkBehaviour nhiệm vụ. Không thêm package mới, không tự sửa scene hoặc đăng nhập Unity Cloud.

## Thử trên máy thật tiếp theo

Dùng cùng gói Protocol 13 trên mọi máy. Vào campus qua menu, cả đội tới cổng B, nhà điều hành và Học liệu. Đối chiếu target/đếm người/hold/result trên từng máy; thử có người đứng ngoài, bị gục, rời vùng, rồi team wipe/restart. Đọc hướng dẫn `CAMPUS_RECON_SYSTEM.md`. Chỉ ghi PASS cho Relay nhiều máy sau khi thực sự chạy.
