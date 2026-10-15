# LAST_PROTOCOL — Multiplayer architecture

## Phạm vi MVP
Unity 6000.5.4f1 / URP, Windows PC, 1 Host + tối đa 3 Client. Host là gameplay server; Relay chuyển tiếp dữ liệu DTLS. Production UI luôn kết nối qua Relay. Không có Photon, Mirror, standalone Relay SDK hoặc backend tự viết.

MVP sử dụng Relay API **bên trong Multiplayer Services SDK**, theo [hướng dẫn Unity](https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo). Không tạo `ISession` của MPS hoặc Unity Lobby Service: danh sách/Ready/phase của phòng được đồng bộ bằng NGO. Join Code là Relay Join Code, không phải MPS session code. Tránh hai hệ thống cùng khởi động NGO.

## Các module
| Script | Trách nhiệm |
|---|---|
| MultiplayerConfig | MaxPlayers duy nhất, tick rate, timeout, tốc độ, camera, môi trường UGS |
| ConnectionPolicy | Chuẩn hóa/kiểm tra code, kiểm tra protocol, giới hạn kết nối, kiểm tra input hữu hạn |
| RelayConnectionService | Initialize UGS, anonymous sign-in, allocation/join, cấu hình DTLS |
| MultiplayerSession | Vòng đời phiên, single-flight, approval, callback kết nối, cleanup, chuyển scene/spawn |
| NetworkRoomState / RoomMember | NetworkList thành viên, slot, Host/Ready; NetworkVariable phase |
| PlayerSpawnPoints | Bốn vị trí spawn có slot ổn định |
| NetworkPlayerController | Owner input, RPC 30 Hz, validate/rate limit/sequence, server simulation |
| PlayerMovement | CharacterController/gravity/rotation trên server |
| PlayerHealth / HealthState | HP/current/max/Downed theo snapshot do Host ghi; chặn movement khi gục |
| PlayerHealthView / PlayerHealthSetupTool | Thanh máu/HUD theo sự kiện, công cụ test Host Development và cài prefab có backup |
| NetworkHitscanWeapon / CombatRules / ShotGate | Owner yêu cầu bắn, Host kiểm tra aim/LOS/cadence/sequence và quyết định damage |
| TrainingTarget / WeaponHUD / CombatSetupTool | Bia tập bắn đồng bộ, crosshair/HIT/tracer, prefab migration có backup |
| RobotEnemy / EnemyState / EnemyRules | Một robot server-authoritative, NavMesh patrol/chase/melee, HP/death/respawn |
| RobotEnemyView / EnemySetupTool | Visual/HP/state của robot, bake NavMesh sân mẫu, đăng ký prefab có backup |
| LocalPlayerCamera | Cinemachine chỉ theo owner, chuột orbit, Esc mở/khóa cursor |
| MultiplayerRoomUI | UI theo sự kiện, Ready, Copy Code, quyền Start/Leave |
| MultiplayerSetupTool | Tạo asset/scene không ghi đè; validate, build Windows |
| MultiplayerSmokeHarness | Kiểm thử loopback opt-in chỉ Editor/Development Build |

## Quyền xử lý
Host ghi roster, phase, transform và HP. Client gửi **hướng di chuyển**, không gửi vị trí cuối cùng. Server kiểm tra sender, hữu hạn, sequence, độ lớn <= 1, tần suất, input timeout và khả năng hoạt động theo Health. NetworkTransform mặc định server authoritative + interpolation. Chưa có prediction/reconciliation. Health, combat hitscan và Enemy đầu tiên đã triển khai; protocol hiện tại là 7. Chỉ Host quyết định hit/damage và chạy NavMeshAgent/AI; friendly fire mặc định OFF giữa Player. Xem PLAYER_HEALTH_SYSTEM.md, BASIC_COMBAT_SYSTEM.md và FIRST_ENEMY_SYSTEM.md.

Anonymous Authentication xác thực với UGS, Relay kiểm soát truy cập allocation. Connection Approval kiểm tra wire protocol, phòng mở và số slot đã reserve; payload không phải bằng chứng danh tính UGS. Không có backend xác thực player token độc lập trong MVP. Người có Join Code có thể yêu cầu vào phòng khi còn chỗ. Không gửi token UGS/Relay vào log hoặc roster.

## Vòng đời
`Offline/Error -> Initializing -> Creating/Joining -> Connecting -> Lobby -> LoadingGame -> InGame -> Leaving -> Offline`.

- Create: UGS/Auth -> allocation `MaxPlayers - 1` -> code -> DTLS -> StartHost -> callback -> spawn Room.
- Join: code chuẩn hóa -> UGS/Auth -> join allocation -> DTLS -> StartClient -> callback xác nhận. StartClient trả true chưa phải Connected.
- Hai thao tác đồng thời bị chặn. Service task có timeout; kết quả đến muộn bị bỏ, lỗi muộn được observe. Request UGS không có cancellation thực sự; allocation chưa bind sẽ hết hạn theo Relay.
- Leave: ngừng approval, hủy thao tác, Shutdown NGO, chờ shutdown xong, xóa Room, trở về MainMenu. Không tái khởi động trong lúc shutdown.
- Host thoát: Client xử lý disconnect, cleanup và về Menu với thông báo. Không có host migration.

## Lobby và scene
Tất cả người chơi, kể cả Host, phải Ready. Mặc định tối thiểu 2 người để Start; có thể đổi config thành 1 khi cần solo. Chỉ Host được Start. Khi Loading/Playing, phòng đóng với người mới; Client rời khi Playing không được thay bằng người mới trong MVP này.

Start cũng yêu cầu số slot đã approval, số client đã kết nối và số thành viên roster khớp nhau; không bắt đầu khi một client đang hoàn tất lobby synchronization.

Menu chứa NetworkManager/Session persistent; Room được spawn với `destroyWithScene=false`. Approval luôn `CreatePlayerObject=false`. Sau `LoadEventCompleted` của NGO, Host spawn mỗi PlayerObject đúng một lần ở slot tương ứng. Player thuộc gameplay scene; manager/room không được đặt lại trong gameplay scene. Client load timeout bị ngắt kết nối.

## Mở rộng co-op
Đã có combat hitscan, bia tập bắn và một robot AI trên sân mẫu; chưa triển khai inventory, Boss AI hoặc quest. Khi mở rộng, dùng NetworkBehaviour cụ thể trước khi tạo interface:
- Health/damage đã có API server-only và đồng bộ HP/Downed. Combat và revive đều validate trên Host. Giữ E 3 giây, trong 2,5 m và có LOS để cứu đồng đội gục với 40% HP; channel bị hủy khi gián đoạn. Team wipe do Host phát hiện và reset vòng thử nghiệm. Xem DOWNED_REVIVE_SYSTEM.md.
- Enemy robot hiện chạy server, Client hiển thị transform/HP/mode; Boss/waves/nhiều loại enemy và hệ thống spawn theo nhiệm vụ là bước sau. NavMesh hiện dành riêng cho sân mẫu, chưa phải pipeline bản đồ đầy đủ.
- Inventory: server quyết định item ownership; tách dữ liệu riêng và dữ liệu công khai.
- Mission/quest: server ghi tiến độ chung, điều kiện hoàn thành và phần thưởng.
- CONTROL CHIP / AI Core: server giữ client đang tương tác, điều kiện có chip, khoảng cách, thời gian channel và tiến độ. Một người channel, đồng đội bảo vệ; mất kết nối/downed/rời phạm vi thì server hủy channel hoặc xử lý theo thiết kế. Client chỉ hiển thị tiến độ đã đồng bộ.

Giới hạn: Host là điểm lỗi duy nhất, Host có thể gian lận, không dedicated server/host migration, không reconnect giữa trận, không lưu tiến độ, không bù trễ combat. Relay không thay thế server authority.


## Menu và chọn màn
Host chọn một trong hai scene whitelist (Training Grounds / Containment Yard), đồng bộ bằng SelectedLevel server-write. Đổi màn trong Lobby đặt toàn bộ Ready về false. StartGame kiểm tra scene tồn tại trong build rồi tải bằng NGO, chỉ spawn sau LoadEventCompleted của scene đã chọn. Menu prefab dùng chung Session hiện có, không thêm NetworkManager; xem MENU_LEVEL_SELECTION.md.
