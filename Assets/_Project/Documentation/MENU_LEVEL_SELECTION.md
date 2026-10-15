# Menu và chọn màn chơi — LAST_PROTOCOL

Protocol **7**, Unity 6000.5.4f1 / NGO 2.13.3. Host và tất cả Client dùng cùng bản MenuPlayer mới. Bản RevivePlayer protocol 6 không tương thích với lựa chọn màn đồng bộ mới.

## Sử dụng

Mở scene MainMenu như trước rồi Play. Giao diện mới tự xuất hiện từ prefab, không cần đổi scene hoặc gắn thủ công component.

1. **PLAY CO-OP** mở chọn màn. Chọn một trong hai sân, xem mô tả và nhấn **CREATE ROOM**.
2. Host chia sẻ Join Code bằng **COPY CODE**. Client chọn **JOIN A ROOM**, nhập mã 6 ký tự và nhấn **CONNECT TO ROOM**.
3. Lobby hiển thị 4 slot, vai trò Host/Client, Ready và màn đang chọn. Host dùng **CHANGE OPERATION** để đổi màn; mọi người phải Ready lại sau khi đổi. Back ở trang chọn màn hủy lựa chọn nháp.
4. Tất cả Ready và đủ số người trong config thì Host dùng **DEPLOY SQUAD**. Client không có quyền đổi màn hoặc bắt đầu trận.
5. Cả đội được chuyển tới scene được chọn bằng NGO NetworkSceneManager. Player, súng, Enemy, gục và hồi sinh dùng chung hệ thống hiện tại.
6. **LEAVE SESSION** trở lại MainMenu. **HOW TO PLAY** hiển thị điều khiển. Lỗi kết nối có trang riêng và nút Back/Try Again. Khi đang kết nối/tải scene, overlay khóa tương tác và hiển thị trạng thái, không giả lập phần trăm tải.

Giao diện dùng tiếng Anh theo UI hiện có, chữ ASCII để dùng đúng font TMP có sẵn; tài liệu bằng tiếng Việt. Tông xanh đậm, cyan và cam; các sơ đồ sân là hình minh họa uGUI tạo bằng code, không phải ảnh gameplay hay bản đồ định vị thời gian thực.

## Hai sân chơi được

| ID ổn định | Tên | Scene | Nội dung |
|---|---|---|---|
| 0 | TRAINING GROUNDS | MultiplayerTest | Sân 40 x 40 hiện có, 4 bia và robot tuần tra; kiểm tra di chuyển/bắn/cứu |
| 1 | CONTAINMENT YARD | ContainmentYard | Sân mới có tường biên, 5 vật chắn, vạch sàn và màu ánh sáng riêng; 4 bia và robot |

Sân thứ hai có NavMesh bake riêng, tính cả vật chắn/tường và 4 bia runtime. Host chọn dữ liệu navigation của scene trước khi spawn Enemy. Không dùng NavMesh của sân trống để đi xuyên các vật chắn mới. Các sân hiện chỉ phục vụ test; chưa có nhiệm vụ, wave, mục tiêu thắng/thua hoặc phần thưởng riêng.

## Script / asset

| Thành phần | Vai trò |
|---|---|
| LevelCatalog | 2 ID cố định và whitelist scene, tiêu đề, briefing và màu nhận diện |
| NetworkRoomState.SelectedLevel | Server-write, mọi Client đọc; chỉ SelectLevelServer hợp lệ trong Lobby được thay đổi và reset Ready |
| MultiplayerSession.TrySelectLevel | Lưu lựa chọn trước khi tạo phòng; kiểm tra Host/phase khi đã có phòng; ghi nhớ lựa chọn của Host sau khi rời phòng |
| MultiplayerSession.StartGame | Kiểm tra scene có trong build, tải scene đã chọn và chỉ spawn sau LoadEventCompleted đúng scene |
| MenuFrontEnd | Điều khiển Home / Levels / Join / Lobby / Controls / Error; cập nhật theo sự kiện session, khóa thao tác khi busy |
| OperationsMenu.prefab | Canvas uGUI/TMP chỉnh sửa được trong Prefab Mode, reference resolution 1920 x 1080, Safe Frame thu nhỏ theo tỉ lệ màn hình |
| MultiplayerRoomUI | Menu cũ tự ẩn và tạo prefab mới khi config có tham chiếu; gameplay HUD hiện tên màn đang chơi |
| LevelEnvironment | ID scene và NavMesh cho Enemy trong ContainmentYard |
| MenuSetupTool | Tạo asset mới, gắn config và thêm scene mới vào cuối build list; backup trước khi thay config/build settings; chạy lại an toàn |
| MenuLevelTests | ID/scene, quyền đổi màn trước spawn, setup idempotence, giữ nguyên scene gốc, NavMesh qua cover |
| MenuPreviewHarness | Chỉ Development, opt-in; thử nút thật, render UI ra PNG, kiểm tra text overflow/bounds, vào Yard và trở về Menu qua loopback |

Không xóa/ghi đè MainMenu, MultiplayerTest hoặc SampleScene. Menu mới được tham chiếu qua `Assets/_Project/Settings/MultiplayerConfig.asset > Menu Prefab`; có thể bỏ tham chiếu để dùng lại UI cũ. Không có NetworkManager hay EventSystem thứ hai trong prefab UI.

## Chỉnh giao diện / build

- Prefab: `Assets/_Project/Prefabs/UI/OperationsMenu.prefab`. Mở Prefab Mode để chỉnh text/màu/vị trí và các binding; không đặt thêm NetworkManager vào prefab.
- Cài/validate: **LastProtocol > Menu > Install Menus and Two Test Levels**. Chạy ngoài Play Mode. Tool giữ nguyên prefab/scene đã tồn tại; không dùng Install để ghi đè các chỉnh sửa thiết kế của bạn.
- Build: **LastProtocol > Menu > Build Windows Menu Demo**. Bản build có MainMenu và cả 2 scene chơi được; Play bắt đầu từ MainMenu. Copy cả thư mục build cho máy khác.
- Bản build kiểm tra: `Builds/ValidationProject/Builds/MenuPlayer/LAST_PROTOCOL.exe`.
- Backup: `Builds/MenuBackups/`. Source integration chỉ thêm reference menu vào config và thêm ContainmentYard vào cuối EditorBuildSettings; không thay thứ tự scene cũ.

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage MenuSetup
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage MenuBuild
.\Tools\Run-MenuPreview.ps1 -FreshCapture
.\Tools\Run-MultiplayerSmoke.ps1 -LevelTest -Level 0 -CombatTest
.\Tools\Run-MultiplayerSmoke.ps1 -LevelTest -Level 1 -CombatTest
```

Các smoke dùng loopback, không gọi Authentication hoặc Relay. Bài preview dùng fixture tối thiểu 1 người **trong bộ nhớ của tiến trình kiểm tra** để chụp scene Yard; config project mặc định vẫn cần 2 người, không bị sửa thành solo. Các kết quả này không thay thế test Internet nhiều PC hoặc thao tác chuột/bàn phím của người dùng.

## Kết quả kiểm tra
Xem MENU_LEVEL_TEST_RESULTS.md cho số lượng test, giới hạn và đường dẫn ảnh chụp giao diện. Không dùng ảnh đen từ swapchain cửa sổ ẩn làm bằng chứng hình ảnh; bộ preview render Canvas thật ra texture.
