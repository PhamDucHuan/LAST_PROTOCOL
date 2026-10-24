# Lộ trình triển khai theo LAST_PROTOCOL.docx

Cập nhật 16/10/2026. Canon hiện hành: [STORY_CANON.md](STORY_CANON.md), dựa trên bản Word người dùng chọn tại `C:/Users/ADMIN/OneDrive/Máy tính/LAST_PROTOCOL.docx`. Bản nguồn và checksum nằm trong [Design/Story](../../../Design/Story/SOURCE.json). Tài liệu này thay thế kế hoạch dựa trên `D:/CTU/DATN/Cốt truyện.docx`; bản cũ đã được giữ trong `Builds/StoryBackups/20261016-121808-CanonUpdate`.

## Điểm xuất phát thực tế

Protocol26 có [checkpoint trong phiên và tutorial chuyển chip](CHECKPOINT_CHIP.md): checkpoint giữ bản ghi, carrier đã nhận chip và tutorial; drill ở ADMIN 01 trước máy phát, chip rơi khi gục, nhặt E và revive bằng hai gesture riêng. Đây là phần đầu của D/E; class, Trace/Integrity, save đĩa và reconnect vẫn chưa có.

Protocol25 thêm [encounter bảo vệ uplink](UPLINK_DEFENSE.md): một đợt robot sau máy phát, giữ ledger qua hủy channel và dọn/reset theo lượt. Đây là lát nhỏ của F, chưa có CaptureChip, Meter hoặc Director.

Cập nhật triển khai 21/10/2026: Protocol24 ghép tập hợp → ba bản ghi → máy phát → liên lạc, với E dùng chung hồi sinh và Host xác nhận mọi credit. Xem [RESTORE_UPLINK_SYSTEM.md](RESTORE_UPLINK_SYSTEM.md). Đây là phân đoạn tuyến tính mở đầu, chưa phải toàn bộ nhiệm vụ cha.

Protocol 13 đã có nền NGO/Relay Host–Client, combat cơ bản, gục/hồi sinh và map campus. CTU_00_RECON có dữ liệu ba điểm tập hợp, trạng thái chung do Host quản lý và HUD. Đây là prototype nhiệm vụ, chưa phải một chương Cần Thơ hoàn chỉnh. Kết nối ba máy đã được người dùng xác nhận; nhiệm vụ mới và tải enemy vẫn cần đo trên máy thật.

MissionDefinition schema1 có Rendezvous/Stations và dependency tuyến tính; schema0 Recon vẫn hoạt động. Nhánh/Skipped, bốn class, checkpoint campaign bền vững, reconnect, Core vote, NOMAD và backend tài khoản chưa triển khai.

CTU_00_RECON tiếp tục phục vụ hồi quy. CTU_01_RESTORE_UPLINK hiện có bốn bước mở đầu trong **Dòng nước bị khóa**, không thêm nhiệm vụ chính thứ sáu, không thay Central Server và không cấp MEKONG Core Data sớm. Campus cứu hộ là bối cảnh hư cấu của prototype, không phải địa điểm được Word xác nhận.

## Các chặng triển khai

| Chặng | Đầu ra có thể kiểm tra | Điều kiện trước khi chuyển tiếp |
|---|---|---|
| A. Tương tác dùng chung | Một bộ điều phối Interact/Revive; ID ổn định; Host kiểm tra khoảng cách, trạng thái, phase, tốc độ gửi | Một lần giữ phím chỉ tạo một hành động; không tương tác từ xa hoặc hoàn thành hai lần |
| B. Nhiệm vụ dạng graph | Objective Locked/Available/Active/Completed/Failed/Skipped; dependency, nhánh và dữ liệu; chuyển được prototype cũ | Validator phát hiện ID/trỏ sai, objective không tới được và thiếu đường Complete; Client không tự chuyển node |
| C. Bốn operator | Khoa/Hacker, Maya/Engineer, Sofia/Assault, Idris/Recon; không trùng class mặc định | Mọi mục tiêu bắt buộc chơi được với 2 người; class là lợi thế, không là khóa cứng; slot không chọn ở NOMAD hỗ trợ radio |
| D. CONTROL CHIP | Một Capsule, Khoa mang đầu tiên; Harness cho mọi class; eject, nhặt/chuyển, Trace, Sync, Overload, Integrity tách biệt | Hai người nhặt cùng lúc chỉ một người thắng; revive không giành lại chip; Trace100 không phạt Integrity hai lần; Safe Mode có đường sửa |
| E. Checkpoint và reconnect | Snapshot có version, wipe phục hồi, AI tạm điều khiển operator mất mạng, trao lại ownership, chuyển chip khi hết hạn | Không nhân Player/chip/loot/reward; profile/operator ID không phụ thuộc NGO ClientId; có chính sách Host recovery rõ |
| F. Encounter và Meter | Enemy nhiệm vụ riêng, NavMesh, ngân sách spawn, robot CaptureChip, ba scope Meter; telemetry theo encounter | Wave không spawn lại khi ngắt channel; enemy có đường đi; đo FPS/RTT trên 2/3/4 máy |
| G. Năm nhiệm vụ Cần Thơ | Dòng nước bị khóa → Những cánh đồng không người → Áp lực → Dây chuyền thép → Central Server | Có cứu dân, máy phát, tutorial revive/chuyển chip, nông nghiệp, 3 trạm bơm và lựa chọn dây chuyền |
| H. MEKONG GUARDIAN và Core vote | Gatekeeper/3 ổn áp, Flood Logic, Core Exposure; vote Alliance/Destroy sau Boss, hòa do carrier phân xử, xác nhận trước commit | Cả hai nhánh hoàn thành chương; carrier gục ở Core Exposure vẫn chuyển chip tiếp tục được |
| I. Progression và reward | Alliance nhận Signed MEKONG Key; Destroy nhận Raw Hydro Fragment; cả hai nâng Tier2, mở Tokyo; state hậu quả | Save/retry/reconnect không cấp lặp; quyết định campaign bất biến; guest không bị ghi đè campaign riêng |
| J. NOMAD, ARKLINE và backend | Hub/loadout/sửa chip, Departure Operation; ASP.NET Core + MySQL, account, progression/reward transaction | All-Destroy vẫn có đường ARKLINE; local cache không ghi đè server save online; API kiểm tra quyền profile và chống cấp thưởng lặp |
| K. City Framework và Director | CityDefinition, Mission/Boss/Decision/WorldState, công cụ validator; rule/utility Director có whitelist, cap và log | Tạo được một city mẫu không copy cả hệ thống; counter không triệt tiêu tuyệt đối build của người chơi |
| L. Bốn chương quốc tế | Tokyo, Shanghai, London, New York đều có chain/mechanic/Boss/quyết định/epilogue | Kiểm tra Alliance và Destroy từng chương; Global Access Key lấy trước quyết định ATLAS |
| M. Revisit, Replay, Global | Revisit thế giới thật; Replay mô phỏng ECHO-7; Guardian Proxy; final 4 phase và 3 ending | Không đổi canon khi Replay; không farm key; Sync token số tách Capsule vật lý; điều kiện ending có cấu hình |
| N. Khóa nội dung và nghiệm thu | Tối ưu, accessibility, save migration, crash/recovery, tài liệu và build | Có bằng chứng test cho bản cụ thể, không đánh dấu PASS bằng suy luận |

Phân đoạn tương tác A và dependency tuyến tính của B đã có ở Protocol24. Phần graph phân nhánh, C/D/E và encounter/checkpoint vẫn cần hoàn thiện trước khi tuyên bố vòng nhiệm vụ chính hoàn chỉnh. Đo FPS/RTT trên máy thật trước khi tăng tải enemy hoặc mở thêm city.

## Phạm vi chiến dịch không được cắt nhầm

| Chương | Baseline sản xuất từ nguồn | Mốc chip sau Core, ở cả Alliance/Destroy |
|---|---|---|
| Prologue | Giới thiệu Bridge Node/Capsule, đội và NOMAD | Tier1: Control Node và Core Sync cơ bản |
| Cần Thơ | 4 district, 5 nhiệm vụ chính, chương lớn nhất | MEKONG → Tier2 |
| Tokyo | 3 district, 5 nhiệm vụ chính | KAGAMI → Tier3 |
| Shanghai | 2–3 district, 5 nhiệm vụ chính | DRAGON → Tier4 |
| London | 2 district, 5 nhiệm vụ chính | MERLIN → Tier5 |
| New York | 2 district + Command Tower, 5 nhiệm vụ chính | ATLAS → Tier6 |
| Global Network | Chương cuối riêng: System, Reflection, Argument, Last Sync | Giải quyết chiến dịch và epilogue |

Quy mô district có thể điều chỉnh trong phạm vi nguồn; cả năm thành phố vẫn phải có vòng chơi đầy đủ. Mục tiêu dùng lại 70–80% framework là mục tiêu kiến trúc, chưa phải số đo hiện tại. Khi trễ, giảm side quest/biến thể/cutscene/độ rộng, không bỏ các thành phố hoặc chip transfer, co-op 2–4, bốn class, save decision, Director có log.

## Quy tắc kiến trúc khi thực hiện

- Giữ NGO + Unity Transport + Relay và Host authoritative. Không đổi framework mạng hoặc package chỉ vì đổi truyện.
- NetworkRoomState quản lý phiên/lobby; component nhiệm vụ/chip/encounter riêng ghi state từ Host. UI đọc state theo sự kiện. Không đẩy toàn bộ campaign vào MultiplayerSession.
- Interaction có Can/Begin/Update/Cancel/Complete và action lock thống nhất locomotion/combat/interaction/critical; không thêm một input handler cạnh tranh với revive hiện tại.
- Migrate MissionDefinition sang objective graph có version, bảo toàn dữ liệu Recon. Thử lại scene/prefab/network serialization khi thêm schema, không sửa scene đang mở tự động.
- Chip Tier1 không bị cấm Core Sync nói chung. Directive Lock, phòng thủ và tiến độ mission tạo điều kiện tiếp cận. Tier là nâng cấp cùng một chip.
- Reconnect cần stable identity/token; không dùng lại ClientId cũ làm danh tính. Host migration hoặc recovery là một quyết định kỹ thuật riêng, chưa có sẵn trong NGO session hiện tại.
- Checkpoint lưu objective/encounter/chip/world/reward ledger nhất quán; loot/lore đã xác nhận được lưu, consumable phục hồi theo snapshot, giữ một phần Meter; Boss retry từ phase1.
- Backend ASP.NET Core + MySQL là phần bắt buộc. UGS Anonymous Authentication hiện có không có nghĩa đã hoàn thành account API. Gameplay Host gửi kết quả, backend kiểm tra quyền và transaction; không tin tùy ý lệnh cấp vật phẩm từ Host.
- Chuyển từ scene đơn sang Persistent/CityHub/District/Facility/Boss additive chỉ khi scene lifecycle được kiểm thử; không đổi ngay cơ chế NGO load scene đang hoạt động.
- Ba scope áp lực là Encounter Alert, Mission Deadline Meter, Campaign Threat. Trace/Integrity/Sync không dùng chung biến Meter.
- Chỉ reveal danh tính người đứng sau HELIOS ở epilogue; không nhét spoiler vào data log campus cho tiện thiết kế.

## Lộ trình 24 tháng trong nguồn

Đây là lịch tham chiếu tương đối của đồ án, không có nghĩa còn đủ 24 tháng từ hôm nay, cũng không suy ra tháng hiện tại chỉ bằng số lượng code đã có.

| Tháng nguồn | Trọng tâm |
|---|---|
| 1–4 | Canon, GDD/SRS, controller, combat |
| 5–8 | Enemy, class, networking, revive/chip/reconnect |
| 9–12 | Mission/checkpoint, Cần Thơ, Boss/decision, vertical slice |
| 13–15 | Backend, telemetry/Director, city tools |
| 16–19 | Tokyo, Shanghai, London, New York |
| 20–24 | Global Network, content lock, polish, tải/hiệu năng, báo cáo/build |

Vertical slice mục tiêu 20–35 phút có mission/Boss/decision/save thực, không cần nút debug để hoàn thành. Recon hiện tại chưa đạt định nghĩa đó. Pipeline thiết kế → code → test → gameplay/docs → commit → build trong Word là quy trình dự án; không phải ủy quyền tự commit/push ở lần cập nhật tài liệu này.

## Vấn đề cần chốt trước module liên quan

World Tier4 ở ví dụ Cần Thơ mâu thuẫn với native+1 ceiling; phải tách tier thô, tier giao tranh và tier thưởng trước khi code scaling. Timer vote/reconnect và carrier rời đúng lúc hòa chưa được chốt. Điều kiện LAST PROTOCOL ending ở nguồn vẫn là đề xuất; baseline là ≥3 Alliance, 3 Lore Key, loyalty Khoa/Maya và Integrity >50% tại đầu Last Sync.

Các điểm này không cản bước tương tác campus. Việc cụ thể và nghiệm thu: [STORY_IMPLEMENTATION_BACKLOG.md](STORY_IMPLEMENTATION_BACKLOG.md). Phân đoạn mở đầu: [CANTHO_FIRST_MISSION_DESIGN.md](CANTHO_FIRST_MISSION_DESIGN.md). Thay đổi canon: [STORY_CANON_CHANGELOG.md](STORY_CANON_CHANGELOG.md).
