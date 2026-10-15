# Multiplayer test cases

## Quy ước kết quả
PASS chỉ khi đã chạy bài kiểm tra tương ứng. Local loopback không xác minh Relay/Internet. Automated asset validation không thay thế QA thao tác UI/camera bằng chuột. Kết quả thực tế cập nhật trong IMPLEMENTATION_REPORT.md và TEST_RESULTS.md khi có.

## Automated checks
- Unity compile runtime/editor/NGO IL post-processing.
- EditMode: normalize/validate code, slot cuối/người thứ năm, phòng đóng, sai protocol, input NaN/Infinity, config sai.
- Setup idempotence: chạy lại không đổi byte scene/prefab/config hoặc build list/active scene.
- Controller physics: khoảng cách theo timestep, tốc độ sprint/clamp input, nhảy/đáp đất/không air jump, va trần và cấu hình không hợp lệ. Smoke mới quan sát sprint/jump của bốn người trên cả bốn tiến trình.
- Health: HP khởi tạo, damage/heal không dương, overkill/overflow, Downed không hồi sinh bằng heal, server-only guard và installer idempotence. `Tools/Run-MultiplayerSmoke.ps1 -HealthTest` kiểm tra snapshot HP/Downed/reset trên bốn tiến trình, Client không ghi HP, input khi gục bị Host chặn và di chuyển trở lại sau debug reset. Xem PLAYER_HEALTH_TEST_RESULTS.md để biết kết quả thực chạy.
- Combat: aim finite/origin hợp lệ, cooldown/sequence/wrap, raycast closest cover/range/trigger/ignore owner, config và installer idempotence. `Tools/Run-MultiplayerSmoke.ps1 -CombatTest` bao gồm Health regression, bốn Owner bắn/gây damage, HP bia/gục/reset, reject bắn khi gục/aim lỗi, friendly fire OFF và spam năm request chỉ một phát hợp lệ. Xem BASIC_COMBAT_TEST_RESULTS.md.
- Enemy: melee alive/LOS/range, input/config, API trước spawn, NavMesh/path quanh bia, installer idempotence. `Tools/Run-MultiplayerSmoke.ps1 -EnemyTest` bao gồm baseline rồi thử patrol/chase/melee/retarget/né windup/returning/Client bắn robot/dead/respawn và server-only agent/HP. Kết quả: FIRST_ENEMY_TEST_RESULTS.md.
- Windows development build.
- Development-only loopback harness: `--lp-smoke host|client --lp-result <json path> --lp-duration 45 --lp-auto-start 4 --lp-start-delay 20`. Không flag thì harness không hoạt động. Production build giữ component serialized vô hiệu hóa để tránh Missing Script; toàn bộ logic test/CLI bị loại khỏi build. Host/Client loopback dùng port 7777, không gọi Authentication/Relay.

## Manual Internet matrix — ban đầu CHƯA KIỂM THỬ
| Case | Kết quả mong đợi | Trạng thái |
|---|---|---|
| Host Create qua Relay | Join Code + Waiting + 1/4 | CHƯA KIỂM THỬ |
| Host + 1 Client | Callback kết nối, roster 2/4 | CHƯA KIỂM THỬ |
| Host + 2 Client | Roster 3/4 | CHƯA KIỂM THỬ |
| Host + 3 Client | Roster 4/4 | CHƯA KIỂM THỬ |
| Người thứ năm | Bị từ chối; Room Full | CHƯA KIỂM THỬ |
| Client Leave ở Lobby | Count giảm, Client về menu | CHƯA KIỂM THỬ |
| Client mới vào slot trống ở Lobby | Count tăng, Ready mặc định false | CHƯA KIỂM THỬ |
| Join lúc Loading/Playing | Room Closed | CHƯA KIỂM THỬ |
| Host Leave/đóng app | Client cleanup về Menu và thông báo | CHƯA KIỂM THỬ |
| Chỉ Host hoặc có người chưa Ready | Start bị khóa | CHƯA KIỂM THỬ |
| >=2 người, tất cả Ready, Host Start | Tất cả load gameplay; một Player/người | CHƯA KIỂM THỬ |
| Player 1 di chuyển | Cả ba người khác thấy position/rotation | CHƯA KIỂM THỬ |
| Player 2 di chuyển | Cả ba người khác thấy position/rotation | CHƯA KIỂM THỬ |
| Player 3 di chuyển | Cả ba người khác thấy position/rotation | CHƯA KIỂM THỬ |
| Player 4 di chuyển | Cả ba người khác thấy position/rotation | CHƯA KIỂM THỬ |
| Local camera | Theo đúng owner, chuột orbit, Esc/collision đúng | CHƯA KIỂM THỬ |
| Sai code/Host cũ | Lỗi có thể trở lại Menu | CHƯA KIỂM THỬ |
| Mất Internet/Relay | Timeout/disconnect, không Loading vô hạn | CHƯA KIỂM THỬ |
| Click Create/Join liên tục | Một thao tác/một NetworkManager | CHƯA KIỂM THỬ |
| Create sau Leave | Code mới, roster sạch, không Player trùng | CHƯA KIỂM THỬ |
| Client rời khi Playing | Despawn, count giảm; chưa hỗ trợ join giữa trận | CHƯA KIỂM THỬ |
| HP trên HUD và trên đầu | HUD đúng Owner; HP của từng người giống nhau trên mọi máy | CHƯA KIỂM THỬ |
| Host thử SELF/ALL damage | Đúng người mất HP; không âm; trạng thái Downed khi về 0 | CHƯA KIỂM THỬ |
| Nhân vật gục | Không đi/chạy/nhảy; camera/Esc/Leave hoạt động | CHƯA KIỂM THỬ |
| Host debug reset HP | Mọi người thấy HP tối đa và có thể điều khiển lại | CHƯA KIỂM THỬ |
| Bắn bằng chuột trái vào bia | Tracer/HIT chỉ sau Host confirm; HP bia giống nhau trên mọi máy | CHƯA KIỂM THỬ |
| Bia HP về 0 | Đổi màu/label, tự reset sau 3 giây; không spawn trùng | CHƯA KIỂM THỬ |
| Bắn qua cover/ngoài tầm | Không damage bia sau cover hoặc ngoài 50 m | CHƯA KIỂM THỬ |
| Friendly fire mặc định OFF | Đồng đội chặn đạn, không mất HP | CHƯA KIỂM THỬ |
| Player gục bắn | Host từ chối, không tracer/HIT/damage | CHƯA KIỂM THỬ |
| Esc/UI/Alt+Tab khi đang giữ chuột | Không bắn ngoài ý muốn; thả chuột rồi bấm lại để tiếp tục | CHƯA KIỂM THỬ |
| Enemy tuần tra/phát hiện/đuổi | Cùng trạng thái/vị trí trên mọi máy, agent chỉ chạy Host | CHƯA KIỂM THỬ |
| Enemy windup rồi melee | Báo màu vàng, HP Player giảm đúng 10 nếu còn trong tầm/LOS | CHƯA KIỂM THỬ |
| Né melee hoặc có cover | Host bỏ damage sau khi kiểm tra lại lúc windup kết thúc | CHƯA KIỂM THỬ |
| Mục tiêu gục/disconnect/ra xa | Enemy bỏ mục tiêu, tìm người sống khác hoặc quay về | CHƯA KIỂM THỬ |
| Client bắn enemy | HP enemy giảm 25/phát hợp lệ; 0 HP ngừng AI/attack | CHƯA KIỂM THỬ |
| Enemy respawn sân test | Sau 8 giây, đầy HP ở home; không spawn trùng | CHƯA KIỂM THỬ |

## Thử trên 4 PC
Dùng cùng một build và environment trên cả bốn máy. Một Host chia code, ba Client join, tất cả Ready rồi Start. Mỗi người lần lượt di chuyển và quay, ba máy khác xác nhận. Sau đó lần lượt thử Leave, Host thoát, sai code và mất mạng. Dùng một máy/bản chạy bổ sung để thử người thứ năm.

Ghi ngày, build/source revision, máy, mạng, case, kết quả thực tế và log. Player log mặc định ở `%USERPROFILE%/AppData/LocalLow/DefaultCompany/LAST_PROTOCOL/Player.log`; nhiều bản cùng máy nên dùng `-logFile <file riêng>`. Không công khai token hoặc dữ liệu allocation trong ảnh/log.

## Gục và hồi sinh — thao tác thủ công / Internet

Các kết quả loopback tự động được ghi riêng tại DOWNED_REVIVE_TEST_RESULTS.md; không thay thế kiểm tra người dùng trên nhiều máy.

| Test | Kết quả mong đợi | Trạng thái thủ công |
|---|---|---|
| Enemy đánh đồng đội đến 0 HP | Không chạy/nhảy/bắn, camera vẫn xoay | CHƯA KIỂM THỬ |
| Đứng gần, giữ E đủ 3 giây | Tiến độ hiện ở cả hai, người được cứu có 40/100 HP | CHƯA KIỂM THỬ |
| Thả E / WASD / nhảy / bắn / bị đánh | Hủy, phải thả E rồi giữ lại, bắt đầu từ 0 | CHƯA KIỂM THỬ |
| Ra quá 2,5 m hoặc có vật cản | Host hủy/không bắt đầu cứu | CHƯA KIỂM THỬ |
| Hai người giữ E trên một mục tiêu | Chỉ một channel được nhận, HP hồi đúng một lần | CHƯA KIỂM THỬ |
| Esc / Alt+Tab khi đang cứu | Hủy, quay lại không tự cứu khi E còn giữ | CHƯA KIỂM THỬ |
| Người cứu hoặc mục tiêu disconnect giữa channel | Hủy và giải phóng lease; người khác cứu được | CHƯA KIỂM THỬ |
| Cả đội gục / người sống cuối disconnect | TEAM DOWN, Host restart, Client không có quyền reset | CHƯA KIỂM THỬ |
| Host restart test | 4 Player cũ đầy HP tại spawn, robot reset, không spawn trùng | CHƯA KIỂM THỬ |
| Sau cứu, tiếp tục đánh enemy và cứu lần hai | Điều khiển/bắn bình thường, không giữ lease cũ | CHƯA KIỂM THỬ |
## Menu và hai sân test — kiểm tra thủ công / Internet

| Test | Kết quả mong đợi | Trạng thái thủ công |
|---|---|---|
| MainMenu -> Play -> chọn một trong 2 sân | Thẻ chọn và briefing khớp; Back không thay lựa chọn đã xác nhận | CHƯA KIỂM THỬ |
| Host tạo phòng với Yard, Client join | Mọi người nhìn thấy Containment Yard trong lobby | CHƯA KIỂM THỬ |
| Mọi người Ready, Host đổi màn | Tất cả thành viên về Not Ready, Deploy bị khóa | CHƯA KIỂM THỬ |
| Client đang lobby | Không có nút Change Operation / Deploy | CHƯA KIỂM THỬ |
| Host Deploy Training / Yard | Cả 4 PC vào đúng scene; đủ 4 Player, bia và robot | CHƯA KIỂM THỬ |
| Robot trong Yard | Đi vòng vật chắn, không xuyên tường; LOS melee/gun tôn trọng cover | CHƯA KIỂM THỬ |
| Leave từ Yard / Host thoát | Trở về menu mới, không thêm NetworkManager / EventSystem | CHƯA KIỂM THỬ |
| UI 1280x720, 1920x1080, 1024x768 | Không bị cắt chữ/nút; nhập Join Code và điều hướng chuột/bàn phím được | CHƯA KIỂM THỬ |