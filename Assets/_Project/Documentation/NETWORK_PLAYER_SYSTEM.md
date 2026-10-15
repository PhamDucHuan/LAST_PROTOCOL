# Network Player System

## Prefab và Inspector
`Assets/_Project/Prefabs/Player/NetworkPlayer.prefab`:
- Capsule mesh, URP Player material; không có CapsuleCollider trùng CharacterController.
- NetworkObject; NetworkTransform mặc định server authoritative, Interpolate bật, scale sync tắt.
- CharacterController (cao 2, radius 0.45), PlayerMovement, NetworkPlayerController.
- PlayerInput trỏ tới **asset InputSystem_Actions hiện có**, map Player, scheme Keyboard&Mouse. Ban đầu disabled; chỉ Owner bật.
- Camera Target ở phần trên Capsule, gắn vào controller.

## Input và chuyển động
Owner đọc Move (WASD), chuyển hướng input theo yaw camera, gửi RPC unreliable <=30 lần/giây. Không RPC mỗi render frame; input tiếp tục gửi kể cả khi đứng yên để chống mất gói stop. Host clamp magnitude, loại NaN/Infinity, kiểm tra sender và sequence, giới hạn nhận; quá 0.25 giây không nhận thì dừng.

Server chạy CharacterController trong FixedUpdate với fixedDeltaTime, gravity và rotation. Client có CharacterController disabled, chỉ nhận NetworkTransform interpolation. Không gửi transform từ client. Capsule có màu theo slot để phân biệt.

Controller hiện hỗ trợ WASD, giữ Left Shift để chạy và Space để nhảy. Owner gửi cờ Sprint và bộ đếm Jump kèm input; Host quyết định tốc độ, grounded/cooldown, gravity và va trần. Không combat/prediction. Xem THIRD_PERSON_CONTROLLER.md cho cập nhật protocol 2 và thông số Inspector.

## Spawn
Approval không tạo Player. Lobby không có PlayerObject. Gameplay có PlayerSpawnPoints với `MaxPlayers` Transform; Host dùng slot của RoomMember, kiểm tra PlayerObject chưa tồn tại trước khi SpawnAsPlayerObject. Người chơi rời thì NGO despawn Player theo ownership mặc định.

## Camera
Gameplay có một Camera + AudioListener + CinemachineBrain và CinemachineCamera/ThirdPersonFollow. LocalPlayerCamera bind Camera Target **chỉ trong OnNetworkSpawn của Owner**. Pivot độc lập, không parent vào transform mà Cinemachine tự di chuyển. Mouse delta không nhân deltaTime; Esc mở cursor để bấm Leave, Esc lần nữa khóa lại. Config chỉnh sensitivity/distance; camera không networked.

Mất focus/Alt+Tab sẽ mở con trỏ và chặn input gameplay của cửa sổ đó. Khi quay lại, tự khôi phục khóa chuột nếu trước đó đang điều khiển. Nếu đã nhấn Esc để mở chuột thì giữ nguyên trạng thái, có thể nhấn Esc hoặc click vùng gameplay ngoài UI để khóa lại. Bỏ delta chuột ở frame khóa lại. Host vẫn mô phỏng những Client khác khi cửa sổ Host không được focus.

Chưa xác minh cảm giác camera bằng tương tác chuột thực tế nếu chỉ chạy headless. Kiểm tra thủ công cả góc xoay, va chạm môi trường và một AudioListener sau chuyển scene.
