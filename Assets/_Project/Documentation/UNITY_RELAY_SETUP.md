# Unity Relay / Authentication setup

## Thao tác tài khoản do người dùng thực hiện
1. Đăng nhập Unity Hub/Editor bằng tài khoản có quyền project.
2. Mở **Edit > Project Settings > Services**; xác nhận liên kết đúng Cloud Project LAST_PROTOCOL. Project hiện đã có cloudProjectId; tool không đổi liên kết này.
3. Trên [Unity Dashboard](https://cloud.unity.com/), chọn đúng organization/project và environment `production` (hoặc environment tương ứng trong MultiplayerConfig).
4. Kiểm tra/bật Authentication và Relay, hoàn tất điều kiện dịch vụ mà Dashboard yêu cầu. Anonymous sign-in là cơ chế MVP.
5. Xác nhận ngân sách/quota dịch vụ phù hợp. Không nhập API key hoặc mật khẩu vào script.

Các menu Dashboard có thể thay đổi; đối chiếu [Unity MPS Get Started](https://docs.unity.com/en-us/mps-sdk/get-started) và [Relay + NGO](https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo).

## Package được pin
- NGO 2.13.3 (Unity >= 6000.0); không dùng NGO 3.1.0 vì bản đó yêu cầu Unity 6000.7.
- Multiplayer Services 2.4.0, Authentication 3.8.0, Transport 6.5.0 (bản built-in được Unity 6000.5 resolve), Cinemachine 3.1.7.
- Input System 1.19.0, URP 17.5.0 và uGUI 2.5.0 được giữ.
- Không cài `com.unity.services.relay`/`com.unity.services.lobby`; Relay namespace được cung cấp bởi MPS SDK.

## Chạy qua Internet
1. Tạo/validate asset theo UNITY_EDITOR_SETUP.md, mở MainMenu.
2. Build Windows bằng menu `LastProtocol > Multiplayer > Build Windows Development MVP`.
3. Chép **toàn bộ** thư mục build cho từng máy, không chỉ file exe.
4. Host nhấn Create Room, chờ Waiting for Players và code xuất hiện; dùng Copy Code.
5. Client mở cùng build, nhập code và nhấn Join. Chờ trạng thái Connected và roster, không suy luận từ nút Join.
6. Mỗi người nhấn Ready; Host nhấn Start khi có >=2 người và tất cả Ready.
7. Kiểm thử tối đa 4 máy ở các mạng khác nhau. Không cần port forwarding cho Relay.

Anonymous identity lưu theo profile `lastprotocol` trên mỗi PC. Nếu mở nhiều bản trên một PC để thử Relay, truyền profile riêng, ví dụ `LAST_PROTOCOL.exe --lp-auth-profile player2`. Không xóa token người dùng tự động. Anonymous account không thay thế hệ thống tài khoản/lưu tiến độ dài hạn.

Sau Leave, NGO đóng kết nối và Relay xử lý vòng đời allocation. SDK không cung cấp delete allocation ở luồng này; allocation thất bại chưa bind/không còn kết nối sẽ hết hạn. Code cũ không được dùng cho phiên mới.

**Chưa xác minh dịch vụ Dashboard hoặc kết nối Relay thực tế chỉ bằng việc compile/build.** Xem ma trận kết quả kiểm thử.
