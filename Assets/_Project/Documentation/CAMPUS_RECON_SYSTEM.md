# Nhiệm vụ tiền trạm campus

> **Bản hiện tại:** [ngoại vi và bờ hồ revision8 / Protocol18](CAN_THO_STREETSCAPE.md). Thêm nhà trong hẻm, cây, ghế và đèn; giữ mạng đường/campus revision7. Số liệu bên dưới thuộc lịch sử.

> **Bản chơi hiện hành: Protocol17** — [ngoại vi theo Google Maps](CAN_THO_PERIPHERAL_LAYOUT.md). Không đổi tâm hoặc điều kiện Recon; dùng cùng gói17 trên các máy. Các thông báo build cũ dưới đây là lịch sử.

> **Bản chơi mới nhất: Protocol15, ngoại thất revision5** — [hướng dẫn](CAN_THO_ARCHITECTURE_REALISM.md). Tâm và logic Recon giữ nguyên; dùng cùng build15 trên tất cả máy. Các mốc13/14 dưới đây là lịch sử triển khai.

> Map hiện hành dùng [bố cục revision4, Protocol14](CAN_THO_SCHEMATIC_LAYOUT.md). Logic Recon được triển khai từ Protocol13 giữ nguyên; ba tâm tập hợp vẫn ở vị trí cũ và đã được kiểm tra lại với map mới. Phần hướng dẫn Protocol13 dưới đây là mốc triển khai ban đầu; khi kết nối thực tế mọi máy phải dùng cùng Protocol14.

> Cập nhật nguồn cốt truyện ngày 16/10/2026: [STORY_CANON.md](STORY_CANON.md) theo LAST_PROTOCOL.docx mới. Hướng dẫn dưới đây vẫn mô tả **runtime Protocol 13**, không phải toàn bộ cơ chế campaign mới. CTU_00_RECON là prototype ba điểm tập hợp; chip transfer, bốn class, checkpoint/reconnect và Core vote chưa có. CTU_01 trong [thiết kế campus](CANTHO_FIRST_MISSION_DESIGN.md) là phân đoạn đề xuất của **Dòng nước bị khóa**, chưa triển khai. Quy tắc campaign mới không được suy ra từ hành vi fail/restart tạm thời của Recon.

Ngày 16/10/2026, Protocol 13. Hoàn thiện bước đầu của lộ trình cốt truyện: dữ liệu nhiệm vụ, tiến độ chung do Host quản lý và HUD. Nhiệm vụ `CTU_00_RECON` là một lượt trinh sát có thể hoàn thành; nhiệm vụ sáu mục tiêu `CTU_01_RESTORE_UPLINK` trong tài liệu thiết kế vẫn là bước sau.

## Cách chơi

1. Tất cả máy dùng cùng bản Protocol 13, chọn **CAN THO / CAMPUS II**.
2. Host tạo phòng; 1–3 Client Join, cả đội Ready và Host Deploy.
3. HUD góc trên trái hiện **CAMPUS RECON**. Tới vòng màu cyan tại cổng B.
4. Toàn bộ người trong phiên phải còn sống và cùng trong vùng đủ **2 giây liên tục**. Thiếu một người hoặc có người gục thì chưa tiến; rời vùng trong lúc giữ sẽ đặt thời gian giữ về 0.
5. Cả đội lần lượt tới nhà điều hành và sảnh Trung tâm Học liệu. Đích hiện tại có vòng cyan và nhãn trong thế giới.
6. Sau điểm thứ ba, HUD hiện **RECON COMPLETE**. Người chơi tiếp tục khám phá/chiến đấu hoặc dùng nút Leave Session hiện có.

Rendezvous dùng bán kính 6 m và dung sai chiều cao 2,5 m quanh tâm cấu hình. Tọa độ và thời gian nằm trong `CampusRecon.asset`, không hardcode trong controller. Không cần đủ bốn class; điều kiện tính theo số người thực sự kết nối và mức tối thiểu của phiên. Mặc định chơi 2–4 người; solo chỉ dùng fixture kiểm thử hoặc cấu hình riêng.

Nếu cả đội gục khi nhiệm vụ còn hoạt động, HUD báo thất bại. Host dùng nút restart round hiện có để hồi đội và bắt đầu lại nhiệm vụ từ điểm đầu. Đây là **restart toàn lượt**, chưa phải save/checkpoint của campaign. Nếu số người còn lại dưới mức tối thiểu, nhiệm vụ báo thiếu người; cần rời phiên và lập lại đội. Host mất kết nối kết thúc phiên theo cơ chế hiện có.

## Vai trò từng file

| File | Trách nhiệm |
|---|---|
| `Gameplay/Missions/MissionDefinition.cs` | ScriptableObject cho mission ID, nội dung và chuỗi vùng tập hợp; validator ID trùng/sai vị trí/thời gian |
| `Gameplay/Missions/MissionSnapshot.cs` | Snapshot nguyên tử: mission ID, phase, objective, inside/required, thời gian giữ và số lượt |
| `Gameplay/Missions/MissionRules.cs` | Quy tắc tiến độ thuần để test: cả đội liên tục, thay đổi roster, fail và completed |
| `Gameplay/Missions/NetworkMissionState.cs` | NetworkBehaviour riêng trên Room; Host kiểm tra Player thật, ghi snapshot và xử lý reset |
| `UI/Missions/MissionHUD.cs` | Canvas local, marker local và cập nhật theo `Changed`; không RPC hoàn thành mục tiêu |
| `Editor/MissionSetupTool.cs` | Tạo dữ liệu/material và thêm component vào Room prefab có backup; chạy lại không ghi đè |
| `Networking/Session/MissionSmokeScenario.cs` | Fixture Development opt-in kiểm tra nhiều Client, downed/partial/hold/reset |
| `UI/Missions/MissionPreviewScenario.cs` | Fixture đồ họa solo kiểm tra HUD, kết quả, reset và chụp UI thật |
| `Tests/EditMode/MissionTests.cs` | Test dữ liệu, rule tiến độ, vị trí NavMesh và setup chạy lại |

`MultiplayerSession` chỉ gọi `BeginServer` sau sự kiện NGO xác nhận load gameplay và spawn Player. `NetworkRoomState` không nhận thêm logic nhiệm vụ; component nhiệm vụ nghe `RoundRestarts` để reset cùng lượt. HUD không có input gameplay và không tạo thêm NetworkManager/AudioListener.

## Authority và cập nhật

Snapshot dùng NetworkVariable server-write/everyone-read. Host đọc vị trí đã mô phỏng trên server và `PlayerHealth.CanAct`, không nhận số người trong vùng hoặc yêu cầu hoàn thành từ Client. Không có RPC Client để nhảy objective.

Host kiểm tra khoảng 10 lần/giây và chỉ đồng bộ snapshot khi thay đổi. UI cập nhật theo sự kiện. Một frame bị chậm không cộng toàn bộ thời gian đứng yên vào hold: mỗi sample cộng tối đa 250 ms; thay đổi số người cũng phá hold liên tục. Mission completed không tự hoàn thành lại hoặc trao thưởng.

HUD dùng tiếng Anh theo UI hiện có. Không cần đồng bộ camera hoặc marker qua NetworkTransform: mọi máy dựng marker local từ cùng asset và objective đã đồng bộ.

Tham khảo API đã dùng: [Unity NGO 2.13 NetworkVariables](https://docs.unity.cn/Packages/com.unity.netcode.gameobjects@2.13/manual/basics/networkvariable.html).

## Unity Editor và build

Asset đã cài trong project:

- `Assets/_Project/ScriptableObjects/Missions/CampusRecon.asset`.
- `Assets/_Project/Materials/Missions/Destination.mat`.
- Component `NetworkMissionState` trên `Assets/_Project/Prefabs/Networking/NetworkRoomState.prefab`.

Menu **LastProtocol → Missions → Install Campus Recon** kiểm tra/cài lại an toàn. Tool giữ mission tùy biến, không sửa scene đang mở và backup Room prefab trước khi thêm component. Không chạy cài đặt khi đang Play hoặc đang chỉnh Room trong Prefab Mode.

Thêm NetworkBehaviour lên Room làm thay đổi layout mạng nên protocol tăng từ 12 lên **13**. Tất cả Host/Client phải build/copy cùng bản mới; không trộn với zip Protocol 12.

```powershell
& Tools/Run-MultiplayerValidation.ps1 -Stage MissionSetup
& Tools/Run-MultiplayerValidation.ps1 -Stage Tests
& Tools/Run-MultiplayerValidation.ps1 -Stage MissionBuild
$player = 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\MissionPlayer\LAST_PROTOCOL.exe'
& Tools/Run-MultiplayerSmoke.ps1 -Player $player -LevelTest -Level 2 -MissionTest
& Tools/Run-MissionPartySmoke.ps1 -Player $player -Players 2
& Tools/Run-MissionPartySmoke.ps1 -Player $player -Players 3
& Tools/Run-MenuPreview.ps1 -Player $player -Level 2 -CampusRoute -Mission
```

Batch chỉ chạy bản sao validation. Khi tự dùng tool trong Editor, asset mới tạo tại project đang mở; tool không tự lưu hay thay scene hiện tại.

## Phạm vi đã giới hạn

Chưa có thu thập data, tương tác bảng điện, carrier, CONTROL CHIP, wave nhiệm vụ, phần thưởng hoặc lưu campaign. Recon không trao Core Data/chip V2, không khôi phục MEKONG và không đổi unlock thành phố.

Robot và bia tập hiện có vẫn phục vụ kiểm tra combat trên campus. Recon không dùng kill count; chúng không làm tăng tiến độ nhiệm vụ. Tách chúng khỏi encounter campaign là việc của giai đoạn tiếp theo, trước khi thêm wave hoặc mục tiêu tiêu diệt.

Các bước tiếp theo là `INT-01 → INT-02 → INT-03`: điều phối phím E với revive, kiểm tra tương tác trên Host, rồi ba bản ghi dữ liệu và bảng điện cho CTU_01. Không gắn nút giả nhặt data hoặc channel chip vào bản Recon này.

Kết quả chạy thực tế được ghi riêng trong `CAMPUS_RECON_TEST_RESULTS.md`; không coi thiết kế hoặc test thuần là đã chơi qua Relay trên các máy thật.
