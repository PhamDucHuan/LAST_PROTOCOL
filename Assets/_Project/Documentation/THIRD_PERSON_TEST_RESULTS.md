# Third-Person Controller — kết quả kiểm chứng

Ngày: 2026-10-15. Unity 6000.5.4f1, NGO 2.13.3, protocol LAST_PROTOCOL:2. Scope: Capsule đi/chạy/nhảy, server authority, cursor/focus; không thêm combat/AI/animation/package.

## Kết quả thực tế
| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Runtime/editor compile + NGO IL post-processing | PASS, không lỗi/warning C# mới | ControllerBuild.log |
| Unity EditMode | PASS — 22/22, failed=0, skipped=0, exit=0 | EditMode.xml |
| Khoảng cách đi với dt 0.01, 0.02, 1/30 | PASS | PlayerMovementTests |
| Sprint speed từ config, clamp oversized input | PASS | PlayerMovementTests |
| Nhảy grounded, bỏ qua yêu cầu trên không, đáp đất | PASS | PlayerMovementTests |
| Độ cao nhảy và va trần | PASS | PlayerMovementTests |
| Chặn config không hợp lệ/NaN/Infinity | PASS | PlayerMovementTests |
| Setup chạy lại giữ nguyên scene/prefab/config/list | PASS | SetupToolTests |
| Build Windows Development Third-Person | PASS, exit=0 | ControllerBuild.log |
| 1 Host + 3 Client loopback | PASS | Các JSON Smoke ghi 4 player trên cả bốn tiến trình |
| Sprint/jump được đồng bộ đến mọi tiến trình | PASS | Mỗi tiến trình: sprintedPlayers=4, jumpedPlayers=4, excessiveJumpHeight=false |
| Room Full / Room Closed | PASS | fifth.json / closed.json |
| Host thoát, các Client về Menu | PASS | Cả ba Client sawHostDisconnect=true, suite exit=0 |

Suite kết thúc exit=0, toàn bộ sáu kết quả host/client1/client2/client3/fifth/closed đều Passed=true trong `Builds/ValidationResults/Smoke/Summary.json`. Log/XML trước lượt chạy được lưu trong `Builds/ValidationResults/History`.

## Chưa kiểm thử trong lượt này
- Bấm Shift/Space, Alt+Tab, Esc và chuột trực tiếp trong cửa sổ game; headless harness dùng input tự động.
- Controller protocol 2 trên nhiều PC qua Relay, packet loss/jitter thực tế và cảm giác độ trễ. Người dùng đã báo kết nối mạng ổn ở phiên trước; điều đó không thay thế QA input mới.
- Release build, animation/model nhân vật và gamepad camera.

## File thay đổi
- MultiplayerConfig.cs + asset config: tốc độ sprint, độ cao nhảy, trọng lực, cooldown, kiểm tra finite và protocol 2.
- PlayerMovement.cs: sprint/jump/gravity/terminal velocity/ceiling collision, trạng thái server cho bước animation sau.
- NetworkPlayerController.cs: frame sampling, jump counter, Sprint trong RPC, kiểm tra owner/phase/focus/rate/sequence.
- LocalPlayerCamera.cs: mở cursor khi mất focus/pause, tự khôi phục capture khi quay lại nếu trước đó đang điều khiển; giữ ý định mở chuột bằng Esc, hỗ trợ click ngoài UI để tiếp tục.
- MultiplayerRoomUI.cs: hướng dẫn Shift/Space trong HUD.
- MultiplayerSetupTool.cs: menu build Third-Person vào thư mục riêng; không ghi đè scene/prefab.
- PlayerMovementTests.cs, MultiplayerSmokeHarness.cs và hai script validation/smoke: physics + network regression, lưu log cũ vào History.
- NETWORK_PLAYER_SYSTEM.md, MULTIPLAYER_TEST_CASES.md, TEST_RESULTS.md, THIRD_PERSON_CONTROLLER.md và báo cáo này.

## Chạy bản mới
1. Dừng Play Mode, chờ Unity compile.
2. Dùng `LastProtocol > Player > Build Windows Third-Person Demo`, hoặc bản đã build tại `Builds/ValidationProject/Builds/ThirdPersonPlayer`.
3. Mở cùng bản protocol 2 trên Host và Client; cùng PC dùng `--lp-auth-profile client2` cho Client.
4. Create/Join, cả hai Ready rồi Host Start. WASD đi, giữ Left Shift chạy, Space nhảy, chuột xoay, Esc mở/khóa cursor.
5. Alt+Tab rồi quay lại: phải tự tiếp tục di chuyển/xoay camera nếu trước đó đang điều khiển. Nhấn Esc mở chuột, Alt+Tab rồi quay lại: phải giữ chuột mở; nhấn Esc hoặc click vùng gameplay ngoài UI để tiếp tục. Nút Leave vẫn bấm được, không bị thao tác click khóa chuột chặn. Camera và input chỉ thuộc cửa sổ local.

Không trộn bản MVP cũ protocol 1 trong SmokePlayer với bản mới. Không cần cấu hình lại Cloud chỉ vì nâng controller.

## Bản sửa Alt+Tab — 2026-10-15
- Người dùng báo quay lại cửa sổ không di chuyển được. Nguyên nhân: mất focus mở khóa cursor, nhưng không khôi phục capture; NetworkPlayerController chỉ nhận input khi cursor Locked.
- Đã sửa LocalPlayerCamera: lưu ý định capture riêng với focus/pause; khôi phục khi quay lại và ở frame Update kế tiếp; giữ trạng thái mở chuột chủ động bằng Esc, hỗ trợ click gameplay ngoài UI, bỏ mouse delta ở frame capture lại.
- Đã chạy lại ControllerBuild trên bản sao kiểm chứng: Unity exit=0, không có error CS/warning CS. Bản mới ở `Builds/ValidationProject/Builds/ThirdPersonPlayer`.
- Không đổi protocol/RPC, không đổi scene/prefab/ProjectSettings/package. Các kết quả 22 tests và smoke ở trên thuộc lượt trước; không chạy lại trong bản sửa này.
- Alt+Tab thực tế, thứ tự focus/pause trên Windows, Esc và bấm Leave: **CHƯA KIỂM THỬ** bằng tương tác trực tiếp; thực hiện bước 5 ở trên trên cả Editor và standalone.
