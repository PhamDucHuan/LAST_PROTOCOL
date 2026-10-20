# Client smoothness — kết quả Protocol 12

Ngày: 16/10/2026. Unity 6000.5.4f1, NGO 2.13.3, Transport 6.5.0, Cinemachine 3.1.7. Các bài tự động chạy trên bản sao validation và exe Windows Development, không điều khiển scene đang mở của người dùng.

| Kiểm tra | Kết quả thực tế |
|---|---|
| Compile + Windows build Protocol 12 | PASS, Unity exit 0 |
| EditMode | 91/91 PASS |
| Mạng localhost bình thường, campus + combat | 6/6 kết quả participant PASS |
| Host + 3 Client: movement, rotation, sprint, jump, ownership, scene, Ready reset | PASS ở cả hai lượt mạng |
| Máu/gục/reset, sát thương/target/reset, chặn spam/sai aim/friendly fire | PASS ở lượt combat mạng bình thường |
| Người thứ năm: Room Full, join sau Start: Room Closed | PASS thông báo chính xác ở mạng bình thường |
| Giả lập 60 ms send delay, jitter 20 ms, 3% send loss trên mỗi process | 6/6 kết quả participant PASS; 4 người đã được xác nhận kết nối và vào gameplay |
| Người thứ năm và người join muộn trong mạng giả lập | Không kết nối được, nhận lỗi; không bảo đảm giữ được chính xác lý do NGO khi packet bị mất |
| Host process tắt đột ngột trong giả lập | Cả 3 Client nhận biết ngắt kết nối, trở về Error; lượt này cần chờ timeout transport khoảng 30 giây |
| Đồ họa thật D3D11: menu, camera, NavMesh campus, LIGHT/NORMAL | 50/50 kiểm tra PASS, không failure |
| LIGHT tạo URP clone trong RAM, render scale giảm, post processing tắt | PASS |
| NORMAL khôi phục pipeline/post processing trước đó | PASS |
| FPS sampler hoạt động, exe cap 60 FPS | PASS; chưa đo mức tăng FPS trên máy người dùng |
| Giảm lag trên 3 máy thật qua Relay ở Protocol 12 | **CHƯA KIỂM THỬ** |
| So sánh FPS/frame time/độ trễ input trước và sau trên cùng phần cứng | **CHƯA KIỂM THỬ** |
| Packet loss/latency thực tế của các máy người dùng | **CHƯA ĐO** |

RTT **cao nhất được ghi nhận trong cả lượt giả lập**: Host 366 ms; Client 1/2/3 lần lượt 191/189/186 ms. Đây là peak của phép thử localhost có simulator, bao gồm giai đoạn khởi tạo, không phải RTT trung bình, không phải số đo Relay của người dùng và không dùng làm bằng chứng giảm độ trễ.

## Bằng chứng

Thư mục cố định: `Builds/ValidationResults/RegressionRuns/20261016-ClientSmoothness-Protocol12/`.

- `EditMode.xml`, `Tests.log`, `CanThoBuild.log`.
- `CombatSmoke/`: JSON và log của Host, 3 Client, người thứ năm và người join muộn.
- `NetworkStressSmoke/`: JSON và log mạng giả lập.
- `GraphicalPreview/`: 50 check, log, ảnh render.
- `ArtifactHashes.json`: SHA256 code, prefab và runtime DLL đã kiểm thử.
- `SceneHashes.json`, `Preservation.json`: xác nhận các scene, model campus và navigation đã kiểm tra vẫn giữ nguyên.

Hai lần thử stress trước đó dừng do giả định của fixture: chỉ giữ Client sống thêm 5 giây khi Host tắt đột ngột, và yêu cầu lý do Room Closed luôn đến trước đóng transport dù có giả lập mất gói. Log được giữ trong `Builds/ValidationResults/History`. Fixture cuối tăng thời gian sống của Client và tách tiêu chí từ chối kết nối khỏi lý do hiển thị; bài mạng bình thường vẫn yêu cầu đúng Room Full/Room Closed. Không thay đổi gameplay để làm bài thử vượt qua.

Sau hai lượt smoke đã PASS, kiểm tra log phát hiện lời gọi `FindObjectsByType<T>(FindObjectsSortMode.None)` trong ClientPerformance đã deprecated ở Unity 6.5. Bản đóng gói cuối `...-r2.zip` chỉ thay bằng `FindObjectsByType<T>()`; không thay đổi code mạng hay prefab so với lượt smoke. Đã chạy lại 91 EditMode, build và 50 kiểm tra đồ họa trên bản cuối. `NetworkImplementationUnchangedAfterApiRefresh.json` xác nhận các file networking/prefab tương ứng không thay đổi. Bằng chứng binary của lượt smoke trước được giữ ở thư mục cùng cấp `20261016-ClientSmoothness-Protocol12-beforeAPIRefresh`; không coi các lượt smoke đó là chạy lại trên binary r2.

## Các giới hạn còn lại

Đây là các thay đổi nhằm giảm nguồn gây khựng và cung cấp số đo để chẩn đoán tiếp. Chưa triển khai prediction/reconciliation. Client vẫn gửi input tới Host và nhận vị trí do Host tính, nên RTT cao vẫn gây trễ điều khiển. LIGHT chủ yếu giảm tải render; chưa có CPU/GPU profiling trên ba máy của người dùng. Số check PASS không chứng minh đạt 60 FPS trên mọi máy.

Hướng dẫn kiểm thử thật, danh sách thay đổi và API: `CLIENT_SMOOTHNESS.md`.
