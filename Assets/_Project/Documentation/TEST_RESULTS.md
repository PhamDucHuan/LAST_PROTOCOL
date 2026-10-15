# LAST_PROTOCOL — Test results

Ngày kiểm chứng: 2026-10-14. Unity 6000.5.4f1, NGO 2.13.3, Unity Transport 6.5.0, MPS 2.4.0. Kiểm chứng trên Windows bằng bản sao `Builds/ValidationProject`; không cấu hình tài khoản Cloud tự động.

Đây là kết quả Multiplayer MVP ban đầu. Cập nhật controller ngày 2026-10-15 và kết quả của source mới xem `THIRD_PERSON_TEST_RESULTS.md`; không dùng số 15/15 bên dưới để suy luận test controller mới đã PASS. Người dùng đã báo kết nối mạng ổn; chưa có đủ bằng chứng để ghi PASS cho mọi case bốn PC khác mạng.

## Kết quả đã chạy
| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Runtime/editor compilation + NGO IL post-processing | PASS | Unity batch/Test Runner/build logs |
| Editor asset validation | PASS | Scene references, một AudioListener/scene, prefab components, bốn spawn point, serialized prefab registration |
| EditMode tests | PASS — 15/15, 0 failed/skipped | `Builds/ValidationResults/EditMode.xml` |
| Setup chạy lại không ghi đè | PASS | SetupToolTests so sánh byte scene/prefab/config/prefab list, build list và active scene |
| Windows development build | PASS | `Builds/ValidationResults/Build.log`, exit 0 |
| Host + lần lượt 1/2/3 Client loopback | PASS | Smoke orchestration chờ count 2/3/4 trước khi thêm Client kế tiếp |
| Người thứ năm loopback | PASS — Room Full | Smoke/fifth.json |
| Join sau Start Game | PASS — Room Closed | Smoke/closed.json |
| Ready + NGO chuyển gameplay scene | PASS | Cả bốn tiến trình đạt InGame |
| Bốn PlayerObject, không spawn trùng | PASS | Cả bốn tiến trình ghi maximumPlayers = 4 |
| Đồng bộ position và rotation | PASS | Cả bốn tiến trình quan sát movedPlayers = 4, rotatedPlayers = 4 |
| Local ownership/server movement/camera binding | PASS trong harness | localOwnedPlayer, serverAuthority, cameraBound = true trên cả bốn tiến trình |
| Host thoát, Client về Menu với lỗi disconnect | PASS loopback | Cả ba Client sawHostDisconnect, Error; không lỗi script trong player log |
| Client Leave rồi join lại ở Lobby | PASS loopback | Lifecycle/client.json: leaveCompleted, reconnected |
| Count giảm khi Client rời | PASS loopback | Lifecycle/host.json: departuresObserved |
| Host Leave rồi tạo phiên mới | PASS loopback | Lifecycle/host.json: recreatedRoom, currentMembers = 1 |
| Một NetworkManager sau cleanup | PASS loopback | Lifecycle host/client managerCountAfterLeave = 1 |

Smoke summary: `Builds/ValidationResults/Smoke/Summary.json`; lifecycle summary: `Builds/ValidationResults/Lifecycle/Summary.json`. Test CLI không gọi Authentication hoặc Relay; Unity engine/package analytics có thể vẫn ghi log mạng độc lập.

## Chưa xác minh
- **CHƯA KIỂM THỬ** Create/Join qua Authentication + Relay thật, lấy code và kết nối 4 PC khác mạng.
- **CHƯA KIỂM THỬ** Relay capacity/reuse slot, mã Relay sai/hết hạn, quota/service outage và mất Internet thực tế.
- **CHƯA KIỂM THỬ** UI bằng chuột, Copy/Paste, khóa nút khi double-click, hình ảnh/collision camera, cảm giác latency ở FPS/độ phân giải thực tế.
- **CHƯA KIỂM THỬ** release build, reconnect trong trận hoặc host migration (hai tính năng sau ngoài phạm vi MVP).

## Tiêu chí nghiệm thu
1. Compile: PASS.
2–4. Host tạo Relay room/code và ba Client join qua Internet: CHƯA KIỂM THỬ.
5–10. Giới hạn, PlayerObject, ownership, movement, local camera binding và roster: PASS loopback; cần QA Internet/UI để nghiệm thu sản phẩm.
11–13. Leave/Host close/scene transition: PASS loopback.
14. Policy lỗi, Room Full, disconnect, cleanup: PASS phần đã chạy; lỗi Relay/mất Internet chưa chạy.
15. Tài liệu: đã tạo đầy đủ.

**Chưa tuyên bố hoàn thành nghiệm thu Multiplayer qua Internet.** Người dùng cần thực hiện UNITY_RELAY_SETUP.md và ma trận kiểm thử 4 PC.

## Lỗi đã phát hiện và sửa
- Editor timeout NGO dùng int; sửa kiểu theo source package.
- TMP font tạo động ban đầu không có atlas/settings đầy đủ; UI chuyển sang SDF/atlas chính thức, tài nguyên và giấy phép được đặt trong _Project. Artifact lỗi chỉ của bản sao kiểm chứng được giữ ngoài Assets.
- Đăng ký prefab bằng runtime Add không được serialize: tạo MvpNetworkPrefabs asset và sửa tham chiếu MainMenu do lần triển khai này tạo. Test mạng đầu tiên thất bại; các lượt sau mới được ghi PASS.
- Editor Tool đóng scratch scene trước khi tạo scene additive tiếp theo; bảo vệ scene hiện tại và yêu cầu người dùng tự lưu scene untitled.

Build Development cuối được đặt tại `Builds/ValidationProject/Builds/SmokePlayer`. Chép cả thư mục khi chuyển sang PC khác.
