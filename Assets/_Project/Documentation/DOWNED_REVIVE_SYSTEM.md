# Gục và hồi sinh đồng đội — LAST_PROTOCOL

Unity 6000.5.4f1, NGO 2.13.3, protocol **6**. Tất cả Host/Client phải dùng cùng bản mới; EnemyPlayer protocol 5 không tương thích.

## Chơi thử

1. Dừng Play, đóng Client cũ, chờ Unity compile. Player prefab đã được bổ sung component bằng Editor batch trên bản sao và chuyển lại project. Có thể kiểm tra/cài lại an toàn bằng `LastProtocol > Revive > Install Downed and Revive`.
2. Dùng Editor làm Host; Client chạy `Builds/ValidationProject/Builds/RevivePlayer/LAST_PROTOCOL.exe`. Nếu dùng nhiều Client trên cùng PC, thêm `--lp-auth-profile client2`, `client3` riêng như trước. Tạo phòng mới, Join, Ready và Start.
3. Để robot đánh một người đến 0 HP. Người đó bị gục: không di chuyển/nhảy/bắn, vẫn xoay camera được. Người còn sống đứng gần, nhìn thông báo và **giữ E liên tục 3 giây**, không di chuyển. Tối đa **2,5 m**, cần không bị vật cản che. Chỉ một người cứu một mục tiêu cùng lúc.
4. Hồi sinh thành công nhận **40/100 HP**, có thể chạy/bắn trở lại. Máu hiện tại, trạng thái gục và channel do Host quyết định; người được cứu cũng nhìn thấy tiến độ.
5. Thả E, di chuyển/nhảy, bắn, Esc/Alt-Tab, mất heartbeat, ra khỏi phạm vi, mất LOS, nhận sát thương, bị gục, mục tiêu hồi phục hoặc rời phiên đều hủy quá trình. Khi bị ngắt, thả E rồi giữ lại để bắt đầu lại từ đầu. Không giữ tiến độ cũ.
6. Nếu tất cả Player đang kết nối đều gục, hiện **TEAM DOWN**. Host nhấn **RESTART TEST (HOST)**: toàn đội đầy HP, về bốn spawn point, robot reset về home và đầy HP. Giữ nguyên Player NetworkObject và scene, không spawn trùng. Client chờ Host hoặc Leave Session. Không có host migration.

`RESET ALL HP (TEST)` cũ chỉ là công cụ Development để thử Health, có thể phục hồi người gục khi trận còn hoạt động; không dùng để kiểm thử thời gian hồi sinh. Khi toàn đội gục, nút đó không còn tác dụng; dùng Restart Test. Công cụ này không có trong release.

## Cấu hình

Asset `Assets/_Project/Settings/MultiplayerConfig.asset`:

| Trường | Mặc định | Ý nghĩa |
|---|---:|---|
| reviveRange | 2.5 | Khoảng cách 3D từ transform Player |
| reviveDuration | 3 | Giây giữ liên tục, đo bằng ServerTime |
| reviveHealthFraction | 0.4 | Tỉ lệ Max HP, làm tròn gần nhất, ít nhất 1 HP |
| reviveHeartbeatTimeout | 0.5 | Hết hạn nếu Host không nhận ý định giữ E |

Input có sẵn `Player/Interact` dùng E; không sửa Input Actions. Không thêm package hay ProjectSettings. Setup tool backup Player prefab trong `Builds/ReviveBackups`, không sửa scene đang mở và không ghi lại prefab đã đúng.

## Script và authority

| Script | Trách nhiệm |
|---|---|
| PlayerRevive | Owner đọc E, gửi ý định giữ 10 Hz; Host kiểm tra owner/sender, target PlayerObject, sống/gục, range, LOS, lease, sequence và thời gian |
| ReviveState / ReviveRules | Snapshot server-write: active, target NetworkObjectId, ServerTime, duration, counter hoàn thành; quy tắc thuần kiểm thử được |
| PlayerHealth / HealthState | DamageRevision và LifeRevision để phát hiện gián đoạn; API hồi sinh internal server-only; heal thường không hồi sinh |
| ReviveHUD | HUD cục bộ, tiến độ tính từ ServerTime, thông báo gục, gợi ý E, giao diện cả đội gục và nút Host restart |
| NetworkPlayerController / NetworkHitscanWeapon | Host chặn di chuyển/bắn trong channel; input di chuyển/nhảy hủy channel trước khi điều khiển trở lại |
| NetworkRoomState | Đồng bộ TeamWiped, phát hiện cả đội gục và reset vòng thử nghiệm chỉ trên server |
| LocalPlayerCamera | Mở chuột khi team wipe, giữ ý định capture để phục hồi sau restart |
| ReviveSetupTool | Bổ sung đúng một PlayerRevive và ReviveHUD, gắn font hiện có, backup, validate |
| ReviveTests / ReviveSmokeScenario | Unit tests và kịch bản nhiều tiến trình opt-in Development |

Hai NetworkVariable marker validation được giữ trong cùng wire layout cho Editor/Development/Release, luôn bằng 0 trong phiên thường; chỉ harness Development có API truy cập. Không đặt phần khai báo NetworkVariable vào điều kiện biên dịch khác nhau giữa Host và Client.

Target lease được giữ riêng trên Host. Damage/life revision được ghi khi Host thay đổi Health; phát hiện cả trường hợp bị đánh rồi được heal trước lần Update kế tiếp. Mục tiêu chết/gục lại không được hưởng thời gian từ đời trước. RPC giữ E unreliable có sequence chung với RPC thả E reliable, tránh gói giữ cũ bật lại sau khi đã thả. Tiến độ không gửi mỗi render frame; Client không gửi lượng HP, duration hoặc kết quả thành công.

## Giới hạn MVP

- Chưa có animation ngã/quỳ/cứu, sound, VFX, invulnerability sau cứu hoặc vật phẩm tiêu hao. Capsule vẫn giữ collider và dùng HP/nhãn DOWNED thể hiện gục.
- Không bleed-out/permadeath hoặc tự hồi sinh. Một người sống còn có thể cứu đồng đội; solo gục là team wipe.
- Restart Test dùng lại scene; chưa có checkpoint/save hoặc cơ chế fail nhiệm vụ chính thức. Target tập bắn giữ lịch tự reset riêng.
- Robot vẫn hoạt động khi còn đồng đội sống; tránh bị đánh trong lúc cứu. Khi cả đội gục, AI/combat tạm dừng cho tới restart.
- Chủ động bắn sẽ hủy thao tác E phía Owner; Host từ chối mọi RPC bắn đến trong lúc channel còn active. Client giả mạo không vừa cứu vừa gây damage được.
- Hồi sinh qua Internet nhiều PC, thao tác bàn phím/Alt-Tab thực tế và disconnect đúng giữa channel cần kiểm thử thủ công. Không suy ra PASS từ bài loopback.

## Chạy kiểm tra tự động

PowerShell từ project:

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage ReviveBuild
.\Tools\Run-MultiplayerSmoke.ps1 -ReviveTest
.\Tools\Run-MultiplayerSmoke.ps1 -EnemyTest -Player .\Builds\ValidationProject\Builds\RevivePlayer\LAST_PROTOCOL.exe
```

Batch chạy trên `Builds/ValidationProject`, không đóng Editor gốc. Smoke loopback gồm Host + 3 Client, Room Full, room closed, chuyển scene, authority/movement/health/combat, và các ca hồi sinh; không gọi tài khoản Unity Cloud. `ReviveSmoke/Summary.json` và log là bằng chứng; mục chưa chạy phải ghi CHƯA KIỂM THỬ.
