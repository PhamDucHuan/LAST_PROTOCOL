# Multiplayer UI Setup

Editor tool tạo uGUI Canvas + TextMeshPro + InputSystemUIInputModule. UI dùng font LiberationSans SDF và atlas đã tạo sẵn trong TMP Essential Resources chính thức, ở `_Project/Settings/TextMeshPro/Resources/Fonts & Materials`; giấy phép ở `_Project/Art/Fonts`. Không bắt buộc import toàn bộ TMP Essential Resources hoặc ghi đè font sẵn có.

TMP Settings, font mặc định/fallback, line-breaking, style và shader cần thiết được cung cấp trong `_Project/Settings/TextMeshPro`. Đây là tài nguyên của package Unity đã cài, không phải một package bên thứ ba mới. Không thêm shadergraph HDRP/URP mẫu của TMP.

## Các panel
- MainMenu: title LAST_PROTOCOL, Create Room, Join Room, Quit.
- Join: input tối đa 6 ký tự alphanumeric, Join, Back; status ở footer.
- Room/Lobby: code, Copy Code, count /4, roster có Host/Client và Ready, nút Ready/Not Ready, Start chỉ Host, Leave.
- Error: thông báo và Back / Try Again. Back cleanup về Menu; người dùng có thể tạo/join lại.
- Gameplay: HUD count, hướng dẫn WASD/chuột/Esc, Leave Session.

`MultiplayerRoomUI` subscribe MultiplayerSession.Changed/ErrorRaised; roster/phase changes phát sự kiện qua RoomState. Không polling UI trong Update. Nút thao tác khóa khi async operation/shutdown; Start chỉ bật khi Host, đủ số người và tất cả Ready. LoadingGame khóa Leave để tránh chen ngang scene event; timeout của NGO sẽ xử lý client không load được.

## Gắn thủ công nếu thay UI
Gắn MultiplayerRoomUI lên một object luôn active; kéo các panel, TMP_InputField, TMP_Text và Button vào đúng trường. Không đặt controller lên panel bị ẩn. Không thêm listener Inspector gọi lại Create/Join nếu script đã bind runtime, để tránh double execution.

Tên người chơi MVP sinh theo slot (Player 1–4); chưa có nhập nickname. Trong scene gameplay không tạo NetworkManager hoặc Room lần nữa. InputSystemUIInputModule được Editor Tool tạo; kiểm tra EventSystem có đúng một instance trong scene.

Layout dùng CanvasScaler 1280x800, panel 820x650. Cần kiểm tra trực quan ở độ phân giải mục tiêu, trạng thái code dài/lỗi và khi cursor đang khóa; headless validation không chứng minh UI đẹp hay thao tác chuột đúng.
