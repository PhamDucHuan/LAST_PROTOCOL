# Basic Combat — LAST_PROTOCOL

Ngày: 2026-10-15. Unity 6000.5.4f1 / NGO 2.13.3. Protocol **4** do thêm NetworkBehaviour vào Player và prefab bia tập bắn. Dùng cùng bản CombatPlayer trên mọi máy; tạo phòng mới sau khi cập nhật.

## Phạm vi

Súng thử hitscan bắn theo tâm camera: giữ chuột trái để bắn, 25 damage, 4 phát/giây, tầm tối đa 50 m. Chưa có model súng, animation, âm thanh, giật súng, đạn vật lý, magazine/reload, tiêu hao đạn, inventory hoặc lag compensation. Đạn vô hạn phục vụ kiểm chứng combat. Chưa có Enemy AI/Boss.

Tự spawn bốn bia hình trụ ở hàng z=10, x=-9/-3/3/9. Mỗi bia có 100 HP; bốn phát trúng hợp lệ làm bia gục/đổi màu rồi tự khôi phục sau 3 giây. Bia không di chuyển/tấn công và không phải Enemy AI. Player HP không tự hồi như bia.

Friendly fire mặc định **OFF**. Đồng đội vẫn chặn đường đạn nhưng không mất HP. Có thể bật `friendlyFire` trong config trước khi tạo phòng để thử sát thương Player. Khi Player gục, Host từ chối bắn và movement; camera/Esc/Leave vẫn dùng được. Host có panel Health test cũ để reset HP trong Development.

## Vai trò script

| Script | Vai trò |
|---|---|
| `CombatRules` | Kiểm tra vector/ngắm hợp lệ, truy vấn collider gần nhất, bỏ qua riêng collider của người bắn |
| `ShotGate` | Cooldown theo đồng hồ Host và sequence chống duplicate/replay; sequence bị rate-limit vẫn bị tiêu thụ |
| `NetworkHitscanWeapon` | Owner đọc Player/Attack hiện có, gửi yêu cầu bắn có giới hạn; Host kiểm tra, raycast và áp dụng damage; phát phản hồi |
| `TrainingTarget` | HP snapshot server-write, label/màu, reset bia; tạo arena một lần trên Host |
| `WeaponHUD` | Tâm ngắm và thông tin súng cho Owner; HIT chỉ khi Host xác nhận damage; cleanup khi despawn |
| `CombatSetupTool` | Tạo prefab/material mới, bổ sung Weapon/HUD vào Player, đăng ký target trong NetworkPrefabsList; backup và idempotence |
| `CombatTests` | Aim, range/cover/trigger/collider ownership, cadence/sequence/wrap, config và installer |

Runtime ở `Assets/_Project/Scripts/Gameplay/Combat/`; HUD ở `Scripts/UI/Multiplayer/`; tool ở `Scripts/Editor/`.

## Quyền xử lý và raycast Third-Person

1. Owner chỉ đọc Attack khi đã vào gameplay, còn HP, cửa sổ có focus và cursor Locked. Sau Esc/Alt+Tab/click recapture, phải thả chuột trái trước khi có thể bắn lại, tránh click vào UI/focus phát đạn ngoài ý muốn.
2. Mỗi phát gửi camera-ray origin/direction và sequence qua owner-only RPC. Không gửi target ID, kết quả hit, damage hoặc HP. Không có RPC mỗi render frame khi không bắn.
3. Host kiểm tra sender=OwnerClientId, Player đã spawn, phase Playing, chưa gục, dữ liệu hữu hạn, độ dài hướng hợp lệ và camera origin cách mắt Player do Host quản lý tối đa cameraDistance+2 m.
4. Host dùng đồng hồ riêng để chặn tốc độ vượt cấu hình, duplicate/replay. Không tin timestamp hoặc cooldown do Client gửi.
5. Host kiểm tra đường từ mắt Player tới camera origin không đi xuyên collider. Camera ray xác định điểm ngắm; ray thứ hai từ mắt Player tới điểm ngắm quyết định va chạm gần nhất và damage. Ray thứ hai xử lý vật cản sát người, giới hạn tầm bắn từ Player và sai lệch giữa camera sau lưng với điểm bắn.
6. Bỏ qua riêng collider của người bắn và các trigger; collider môi trường/đồng đội/bia đều chặn. Default Raycast Layers được dùng, không thay Layer ProjectSettings. RaycastAll chọn hit gần nhất để không bỏ qua cover do buffer đầy.
7. Bia nhận damage bằng API server-only; Player chỉ nhận damage khi friendlyFire bật. PlayerHealth vẫn quyết định clamp/Downed. Client không gọi được API damage/heal/reset để tự ghi HP.
8. Server-only feedback RPC phát tracer và bool damage thực sự đã áp dụng cho mọi Client và Host. Tracer tồn tại 0.08 giây; Owner hiện HIT 0.2 giây nếu damage thành công. Client không phát được feedback giả qua RPC này. Không có local damage prediction.

Đây là Host authoritative theo trạng thái physics hiện tại, chưa rewind hitbox/lag compensation hoặc chống aimbot. Hướng ngắm là input do Owner gửi; giới hạn origin và cover không xác thực ý định con người. Host vẫn có thể gian lận theo giới hạn kiến trúc hiện tại.

## Cấu hình

`Assets/_Project/Settings/MultiplayerConfig.asset`:

| Trường | Mặc định |
|---|---:|
| weaponDamage | 25 |
| weaponRange | 50 m |
| weaponShotsPerSecond | 4 (hợp lệ 1–10) |
| friendlyFire | false |
| targetMaxHealth | 100 |
| targetResetSeconds | 3 |

Max HP bia đọc khi spawn; các thông số combat đọc từ config của Host. Dùng cùng config trên các máy để HUD/local rate-limit khớp. Chỉnh trước khi chạy phiên; chưa có UI chỉnh thông số giữa trận hoặc cơ chế đồng bộ asset config.

Player prefab thêm NetworkHitscanWeapon và WeaponHUD; Weapon giữ tham chiếu TrainingTarget prefab và ShotTracer material. HUD dùng font LiberationSans SDF sẵn có. TrainingTarget.prefab và ShotTracer.mat là asset mới, không thay material Player gốc. MvpNetworkPrefabs.asset được bổ sung target, giữ Player/Room và các entry cũ.

## Editor và chạy thử

- Asset đã được tích hợp vào project. Cài lại nếu cần: dừng Play, đóng Prefab Mode, lưu scene chưa có tên rồi chọn `LastProtocol > Combat > Install Hitscan and Training Targets`.
- Tool không lưu/ghi đè scene; prefab và prefab list được backup trước khi thêm component/entry, ở `Builds/CombatBackups/`. Chạy lại không tạo trùng hoặc ghi lại asset đã cấu hình đúng.
- Build trong Editor: `LastProtocol > Combat > Build Windows Combat Demo` -> `Builds/CombatPlayer/LAST_PROTOCOL.exe`.
- Build tự động kiểm chứng: `Tools/Run-MultiplayerValidation.ps1 -Stage CombatBuild` -> `Builds/ValidationProject/Builds/CombatPlayer/LAST_PROTOCOL.exe`.

1. Dừng phiên cũ, chờ Unity compile; Host tạo phòng mới từ MainMenu. Client mở CombatPlayer cùng protocol 4, cùng PC truyền `--lp-auth-profile client2`.
2. Join/Ready/Start. Kiểm tra đúng bốn bia hình trụ phía trước, mỗi bia 100 HP; chỉ một tâm ngắm trên mỗi máy.
3. Xoay chuột đặt tâm vào bia, nhấn/giữ chuột trái. Kiểm tra HP 100/75/50/25/0, tracer, HIT và tự reset bia trên mọi máy.
4. Bắn xuống đất/bắn trượt: có tracer nhưng không HIT/damage. Bắn qua Player khác khi friendlyFire OFF: Player không mất HP và chặn đạn.
5. Dùng panel Health Host cho Player gục; Player đó không bắn được. Reset HP rồi thử bắn lại.
6. Esc mở cursor: không bắn khi click Health panel/Leave. Alt+Tab quay lại: cần thả nút trái rồi bấm lại nếu đã giữ nút lúc chuyển cửa sổ.
7. Nếu cần kiểm tra sát thương đồng đội, dừng phiên rồi bật friendlyFire ở config Host, tạo phòng mới và kiểm tra PlayerHealth/Downed. Mặc định của bộ tự động là OFF.

## Kiểm thử tự động

Chạy tuần tự từ root project:

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage CombatBuild
.\Tools\Run-MultiplayerSmoke.ps1 -CombatTest
```

Combat smoke bao gồm toàn bộ Health smoke/controller/connection regression. Harness chỉ bật với CLI Development flag: thử bắn khi gục, aim NaN/zero/origin xa, friendly-fire OFF; mỗi Owner gửi burst năm request trong một frame nhưng Host chỉ chấp nhận một; sau đó bắn thêm theo cadence để hạ bia và quan sát reset. Mọi tiến trình phải thấy đủ bốn người bắn và gây damage, đúng bốn bia. Harness dịch chuyển bốn Player về bốn lane riêng bằng server NetworkTransform sau phần movement test để kết quả raycast độc lập; thao tác này không tồn tại trong phiên chơi thường.

Log/JSON: `Builds/ValidationResults/CombatSmoke/`. Kết quả thực chạy: BASIC_COMBAT_TEST_RESULTS.md. Headless smoke không thay thế kiểm tra tâm ngắm/tracer/HIT bằng mắt hoặc mouse input thật qua Relay trên nhiều PC.
