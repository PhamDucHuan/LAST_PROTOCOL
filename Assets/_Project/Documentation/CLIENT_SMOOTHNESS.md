# Client smoothness — Protocol 12

## Tình trạng báo cáo

Ngày 16/10/2026: người dùng đã chạy trên **3 máy thật**, kết nối được nhưng Client vừa khựng chuyển động vừa giật hình/camera. Chưa có log FPS/RTT từ các máy đó để xác định tỷ lệ đóng góp của CPU, GPU và đường truyền. Không coi kết nối thành công là kiểm thử hiệu năng thành công.

## Thay đổi

- `LocalPlayerCamera`: đọc chuột ở Update, cập nhật pivot ở LateUpdate với execution order -50, sau NGO PreLateUpdate và trước CinemachineBrain LateUpdate. Khi Bind, Brain được đặt LateUpdate để không theo nhịp vật lý khi target đã được nội suy theo render frame. Giữ cơ chế lấy lại chuột sau Alt+Tab.
- Player và Robot `NetworkTransform`: giữ **server authority**, interpolation bật; dùng Lerp mới của NGO 2.13, delta không tin cậy với axial resync có sẵn của NGO. Ngưỡng vị trí 1 cm, góc 0.5 độ; smoothing 50 ms. Không đồng bộ scale. Tool tạo prefab mới dùng cùng `NetworkMotionSettings`; không ghi đè prefab hiện có khi chạy lại setup.
- `NetworkPlayerController`: bỏ việc loại lệnh mới chỉ vì đến cách lệnh trước dưới 16.7 ms. Jitter có thể khiến nhiều lệnh hợp lệ đến cùng frame. Vẫn loại sequence cũ/trùng, xác nhận Owner, kiểm tra giá trị hữu hạn, clamp input và khóa khi gục. RPC chỉ cập nhật ý định; FixedUpdate trên Host quyết định quãng đường/tốc độ. Giữ tick/input 30 Hz, timeout input 250 ms, không gửi bù hàng loạt mỗi frame.
- `ClientPerformance`: tự gắn vào session, không thêm NetworkBehaviour hay đối tượng mạng. Build giới hạn 60 FPS và tiếp tục xử lý khi mất focus. Không ép giới hạn FPS của Editor.
- Bảng local hiển thị FPS trung bình mỗi 0.5 giây, frame lâu nhất trong khoảng đó và RTT transport. Client hiển thị RTT đến Host; Host hiển thị RTT cao nhất trong các Client. `--` nghĩa chưa có số đo, không phải ping bằng 0.
- **F8**: bật/tắt LIGHT. Clone URP asset trong RAM, render scale tối đa 70%, bóng tối đa 25 m, một cascade, shadow map tối đa 1024; tắt post processing trên camera local. Tắt LIGHT trả lại cấu hình cũ. Không lưu vào ProjectSettings, scene hoặc URP asset.
- **F9**: ẩn/hiện bảng số đo. Chế độ mặc định NORMAL. Không tự lưu sở thích hay tự hạ chất lượng giữa trận.
- Protocol tăng lên **12**: tất cả máy phải dùng cùng bản mới; bản 11 được giữ lại.

## Thử lại trên ba máy

1. Đóng bản cũ trên cả ba máy, giải nén cùng một bản Protocol 12 vào thư mục mới. Host cũng chạy exe để tách chi phí Unity Editor khỏi phép thử.
2. Dùng cùng map, cùng độ phân giải (khởi đầu 1280×720), cùng vị trí cổng B. Chờ khoảng 10 giây sau khi vào map.
3. Ghi FPS, peak frame ms và RTT trên Host và từng Client khi đứng yên, xoay camera, chạy và bắn. Mỗi đoạn khoảng 30 giây.
4. Bật F8 trên Client, lặp lại đúng tuyến để so sánh. LIGHT chỉ ảnh hưởng máy đang bấm, không hạ hình ảnh của người khác.
5. FPS thấp cả lúc đứng yên: kiểm tra cấu hình CPU/GPU, độ phân giải, tải nền; nếu LIGHT không cải thiện rõ cần profiling CPU/map. Host FPS thấp có thể làm mọi Client thấy cập nhật thiếu đều.
6. FPS ổn mà chỉ chuyển động mạng khựng: xem RTT, độ biến thiên và thử mạng dây/Host khác để khoanh vùng. RTT cao vẫn làm input chậm vì đây là server-authoritative movement chưa có prediction/reconciliation.
7. Thử Alt+Tab vào lại, Esc/click để lấy chuột, Client Leave, Host Leave và tạo phòng mới. Không mở scene Gameplay trực tiếp để kiểm thử kết nối.

RTT ở bảng là số đo transport, không phải thời gian hoàn chỉnh từ nhấn phím đến hình ảnh. Bảng không đo packet loss hay percentile frame time. LIGHT giúp giảm tải render nhưng không bảo đảm xử lý được nút thắt CPU hoặc đường truyền.

## Kiểm thử tự động

Các lệnh chạy trên bản sao `Builds/ValidationProject`, không đóng hoặc lưu scene đang mở của người dùng:

```powershell
& Tools/Run-MultiplayerValidation.ps1 -Stage Tests
& Tools/Run-MultiplayerValidation.ps1 -Stage CanThoBuild
$player = 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\CanThoPlayer\LAST_PROTOCOL.exe'
& Tools/Run-MultiplayerSmoke.ps1 -Player $player -LevelTest -Level 2 -CombatTest
& Tools/Run-MultiplayerSmoke.ps1 -Player $player -LevelTest -Level 2 -NetworkStress
& Tools/Run-MenuPreview.ps1 -Player $player -Level 2 -CampusRoute
```

`-NetworkStress` chỉ hoạt động trong CLI smoke fixture của Development Build: Unity Transport simulator thêm 60 ms send delay, jitter ±20 ms và mất 3% gói gửi trên mỗi process. Hai chiều đều áp dụng; RTT thực tế được ghi vào JSON. Không dùng API DebugSimulator đã deprecated, không gọi UGS/Relay. Test này xác nhận chức năng khi đường truyền giả lập xấu, không chứng minh bản mới đã hết giật trên máy thật và không thay thế phép thử Internet.

Ở bài stress, packet báo lý do từ chối của NGO có thể đến sau sự kiện đóng transport. Hai người thử join trái điều kiện chỉ được đánh giá là không kết nối được và nhận lỗi; thông báo chính xác `Room Full` / `Room Closed` được kiểm tra riêng ở bài không giả lập. Đây là giới hạn hiển thị lý do khi mạng xấu. Khi Host process bị tắt đột ngột và thông báo disconnect không đến nơi, Client có thể phải chờ timeout transport 30 giây; fixture stress giữ Client sống thêm 40 giây để kiểm tra đường này.

Kết quả chạy thực tế được ghi riêng trong `CLIENT_SMOOTHNESS_TEST_RESULTS.md` sau khi kiểm thử.

## Tài liệu API

- [NGO 2.13 NetworkTransform](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@2.13/api/Unity.Netcode.Components.NetworkTransform.html): unreliable deltas, interpolation types và smoothing.
- [Client-side interpolation](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@2.13/manual/learn/clientside-interpolation.html): nội suy đánh đổi độ trễ để làm mượt. Prediction/reconciliation là bước riêng nếu input qua Internet vẫn phản hồi chậm sau khi FPS và đường truyền ổn định.

Chưa triển khai prediction trong thay đổi này; Host vẫn quyết định di chuyển, va chạm, máu và sát thương.
