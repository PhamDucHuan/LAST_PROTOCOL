# Player Health — LAST_PROTOCOL

Ngày: 2026-10-15. Unity 6000.5.4f1 / NGO 2.13.3. Module Health ban đầu dùng protocol 3; project hiện tại dùng protocol **6**: Player prefab có thêm NetworkBehaviour, phải dùng cùng bản mới trên Host và Client.

## Phạm vi

HP mặc định 100. HP = 0 là **Downed/Gục**, chặn đi/chạy/nhảy trên Host, vẫn áp dụng gravity và giữ camera/Leave Session hoạt động. Đã có combat hitscan, Enemy và hồi sinh đồng đội (giữ E 3 giây, 2,5 m, hồi 40% HP). Cả đội gục thì Host restart test. Xem DOWNED_REVIVE_SYSTEM.md. Chưa có animation gục, chết vĩnh viễn hoặc nguồn sát thương môi trường tự động.

## Script và luồng dữ liệu

| Script | Vai trò |
|---|---|
| `HealthState` | Snapshot current/maximum, suy ra Downed, clamp damage/heal; snapshot được serialize nguyên khối |
| `PlayerHealth` | NetworkVariable private: mọi người đọc, chỉ Server ghi; API server-only và sự kiện Changed |
| `NetworkPlayerController` | Chặn input khi local gục; Host còn kiểm tra tại nhận RPC và FixedUpdate, không tin trạng thái Client |
| `PlayerHealthView` | Thanh máu world-space cho mọi Player, HUD cho Owner; cập nhật HP theo Changed; cleanup khi despawn |
| `PlayerHealthSetupTool` | Thêm Health/View và font vào prefab hiện có, backup trước khi lưu; chạy lại không tạo trùng hoặc ghi lại prefab đã đúng |
| `PlayerHealthTests` | Biên HP, số âm, overflow, trạng thái gục, server-only guard khi chưa spawn, setup idempotence |

Host khởi tạo mỗi Player bằng `MultiplayerConfig.playerMaxHealth`. Server gameplay tương lai gọi `TryApplyDamageServer(amount)` sau khi tự xác nhận trúng đòn hoặc điều kiện môi trường. Không có RPC cho Client chỉ định lượng damage, target hay HP. Chỉ có server gameplay đủ tin cậy được gọi API này; API chưa kiểm tra hitbox/range/team vì chưa triển khai combat.

NetworkVariable dùng `NetworkVariableReadPermission.Everyone` / `NetworkVariableWritePermission.Server`. `Changed` chỉ phát khi snapshot thay đổi; View còn đọc snapshot lúc spawn để hiển thị đúng trạng thái ban đầu. Không gửi RPC mỗi frame để cập nhật thanh máu. Billboard vẫn cập nhật hướng theo camera mỗi LateUpdate.

`TryHealServer(amount)` chỉ hồi HP cho Player còn sống, không vượt maximum và không hồi sinh Player gục. Damage/heal <= 0 bị bỏ qua; damage rất lớn clamp về 0, không âm hoặc tràn int. Nhân vật gục không despawn. Camera, con trỏ Esc/Alt+Tab và giao diện rời phòng vẫn thuộc Owner.

Host có thể quyết định toàn bộ trạng thái thế giới theo kiến trúc Host–Client hiện tại; hệ thống này không ngăn Host gian lận.

## Cấu hình và Editor

- `Assets/_Project/Settings/MultiplayerConfig.asset`: `playerMaxHealth = 100`, phải >= 1. Đọc khi spawn; thay đổi asset trong phiên không tự đổi HP Player đã spawn.
- `Assets/_Project/Prefabs/Player/NetworkPlayer.prefab`: giữ các component cũ, thêm đúng một PlayerHealth và một PlayerHealthView. View dùng font LiberationSans SDF hiện có.
- Đã tích hợp prefab trong project. Nếu cần cài lại: dừng Play, đóng Prefab Mode của Player rồi chọn `LastProtocol > Health > Install Health on Player Prefab`.
- Backup prefab trước thay đổi nằm ở `Builds/HealthBackups/`. Tool không sửa scene đang mở, không tạo EventSystem/AudioListener/NetworkManager mới.
- Build: `LastProtocol > Health > Build Windows Health Demo` -> `Builds/HealthPlayer/LAST_PROTOCOL.exe`.
- Build kiểm chứng tự động: `Tools/Run-MultiplayerValidation.ps1 -Stage HealthBuild` -> `Builds/ValidationProject/Builds/HealthPlayer/LAST_PROTOCOL.exe`.

## Thử trực tiếp

1. Dừng phiên cũ; Host mở MainMenu bằng Editor với code mới, Client mở bản HealthPlayer. Cùng PC truyền `--lp-auth-profile client2` cho Client. Tạo Join Code mới, Join/Ready/Start.
2. Kiểm tra mỗi người có 100/100 HP; HUD dưới bên trái chỉ hiển thị HP của Owner. Thanh máu trên đầu thể hiện đúng từng người.
3. Trên Host, nhấn Esc mở chuột. Panel `HOST TEST` dưới bên phải chỉ có trong Editor/Development Build.
4. `SELF: -25 HP`: chỉ Host mất 25 HP, mọi Client đều thấy thanh máu Host giảm. HP Client không đổi.
5. `ALL: -25 HP`: Host trừ 25 HP cho mọi Player. Nhấn nhiều lần đến 0; kiểm tra trạng thái DOWNED trên mọi máy.
6. Nhân vật gục không đi/chạy/nhảy được, nhưng vẫn xoay camera và dùng Esc/Leave. Dù toàn đội gục, phiên vẫn mở vì chưa có game-over.
7. `RESET ALL HP (TEST)`: Host đặt HP mọi người về maximum và cho phép di chuyển lại. Đây là thao tác kiểm thử, **không phải cơ chế revive trong gameplay**.
8. Alt+Tab/quay lại, Leave, Host thoát và tạo phòng mới; không còn HUD cũ hoặc Player cũ. Client không có panel debug.

Nút reset chỉ có trong Editor/Development; release không biên dịch phương thức reset hoặc panel. Không có phím tự gây damage, enemy hay combat tự động. Muốn thấy damage phải dùng công cụ thử trên Host.

## Kiểm thử tự động

Chạy lần lượt từ root project, không chạy song song trên cùng ValidationProject:

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage HealthBuild
.\Tools\Run-MultiplayerSmoke.ps1 -HealthTest
```

Smoke opt-in dùng loopback 1 Host + 3 Client: 100 HP -> 75 HP -> 0/Downed -> reset 100. Mọi tiến trình phải quan sát đủ bốn snapshot; Client trực tiếp gọi API ghi HP phải bị từ chối; harness cố tình tiếp tục gửi input khi gục để kiểm tra Host chặn; sau reset phải di chuyển lại được. Vẫn chạy regression room capacity, start/scene, ownership/camera, sprint/jump, Room Closed và Host disconnect.

Không có flag CLI thì harness không chạy. Log/JSON ở `Builds/ValidationResults/HealthSmoke`; XML ở `Builds/ValidationResults/EditMode.xml`. Các kết quả thực chạy và giới hạn kiểm thử ghi trong `PLAYER_HEALTH_TEST_RESULTS.md`.

## Tích hợp về sau

Combat/Enemy/environment chạy logic sát thương trên Host rồi gọi PlayerHealth. Animation và audio có thể lắng nghe Changed. Hệ thống revive sau này cần Host xác nhận khoảng cách, thời gian tương tác, người cứu còn sống và trạng thái nhiệm vụ trước khi phục hồi người gục; không mở RPC nhận lượng HP trực tiếp từ Client. Không lưu HP qua phiên hoặc đồng bộ Inventory/Quest trong module này.
