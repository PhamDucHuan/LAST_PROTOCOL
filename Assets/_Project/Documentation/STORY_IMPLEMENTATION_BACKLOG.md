# Backlog triển khai cốt truyện

Cập nhật 16/10/2026 theo [STORY_CANON.md](STORY_CANON.md) và LAST_PROTOCOL.docx mới. P0: nền nhiệm vụ/campus; P1: chương Cần Thơ và nền campaign; P2: các chương đầy đủ và final. P2 là thứ tự làm sau, không có nghĩa tùy chọn. Chỉ OPT là ngoài phạm vi bắt buộc.

Protocol24 bổ sung tập hợp → ba bản ghi → máy phát → liên lạc. **DONE prototype** không có nghĩa campaign/graph phân nhánh đã hoàn chỉnh. Phạm vi và bằng chứng: [RESTORE_UPLINK_SYSTEM.md](RESTORE_UPLINK_SYSTEM.md); Recon cũ vẫn phục vụ hồi quy.

Protocol25 bổ sung [một đợt ba robot bảo vệ uplink](UPLINK_DEFENSE.md), kích hoạt sau máy phát, không respawn khi hủy tương tác.

Protocol26 bổ sung [checkpoint trong phiên và tutorial chuyển chip](CHECKPOINT_CHIP.md): giữ từng ID dữ liệu, drill trước máy phát, eject/nhặt bằng E, revive không lấy lại chip; carrier hiện tại mới channel uplink. Chưa có save đĩa, class, Trace/Integrity hay reconnect campaign.

| ID | Mức | Trạng thái | Đầu ra và nghiệm thu |
|---|---|---|---|
| NET-01 | P0 | TODO đo lại | FPS/RTT Host/Client với cùng build hiện hành trên 2/3/4 máy thật; đi/xoay/bắn/wave và log, không dùng kết quả localhost thay thế |
| MIS-01 | P0 | DONE prototype | Schema0 Recon được giữ; schema1 có Rendezvous/Stations, ID duy nhất và validator; chưa có nhánh |
| MIS-02 | P0 | DONE prototype | Host ghi snapshot/phase, tập ID và dependency tuyến tính của Restore Uplink |
| MIS-03 | P0 | DONE prototype | HUD/marker theo bước, bản ghi, checkpoint, carrier và tutorial E; chưa có class/reward UI |
| MIS-04 | P0 | TODO | Lobby phân biệt sân tập/campaign/mission; Host đổi mission reset Ready, giữ level ID hiện tại |
| MIS-06 | P0 | PARTIAL prototype | Dependency tuyến tính, schema0/1 và trạng thái suy ra đã có; nhánh/Skipped và validator graph tổng quát còn thiếu |
| INT-01 | P0 | DONE prototype | E dùng chung hồi sinh, terminal, máy phát, uplink, relay drill và nhặt chip; khóa một hành động/mục tiêu đến khi thả |
| INT-02 | P0 | DONE prototype | Host xác thực ID/ownership/khoảng cách/LOS/trạng thái/phase/spam, đặt chỗ và hoàn tất một lần cho năm thiết bị |
| INT-03 | P0 | DONE prototype | Ba ID dữ liệu riêng, máy phát và uplink theo thứ tự; không cấp Lore Key hoặc thưởng campaign |
| CLS-01 | P0 | TODO | Bốn operator/class không trùng mặc định; mọi class làm objective và mang chip; đội 2 người vẫn hợp lệ; slot chưa chọn hỗ trợ radio từ NOMAD |
| CHP-01 | P0 | PARTIAL prototype | Một chip trên Room, carrier do Host xác nhận; slot 0 đại diện Khoa khi chưa có chọn class, mọi slot nhận được; identity campaign còn thiếu |
| CHP-02 | P0 | TODO | Control Node/Core Sync cơ bản Tier1; Sync/Trace/NeuralLoad tách riêng; Host tính channel/cancel |
| CHP-03 | P0 | PARTIAL prototype | Gục → eject, hold E recovery nguyên tử, revive không giành lại; tutorial an toàn trước máy phát; tự thả và enemy CaptureChip còn thiếu |
| CHP-04 | P0 | TODO | Integrity campaign, Stabilizer/sửa, Safe Mode0→repair25; Trace100 không cộng thêm phạt Integrity cùng sự kiện |
| REC-01 | P0 | TODO | Reconnect: AI giữ active operator, ownership trả đúng stable profile, hết hạn chuyển chip; không auto fail ngay vì carrier disconnect |
| REC-02 | P1 | TODO | Chốt và test host migration hoặc recovery strategy; phân biệt Host crash với Client reconnect |
| ENC-01 | P0 | PARTIAL prototype | Prefab/spawner robot nhiệm vụ riêng; robot demo không xuất hiện đầu nhiệm vụ uplink, sân tập/Recon giữ hành vi cũ; target demo chưa tách |
| ENC-02 | P0 | DONE prototype | Wave ID + Attempt, cap ba robot, NavMesh chung và đường tới uplink; framework nhiều wave/Director còn thiếu |
| ENC-03 | P0 | PARTIAL prototype | Defend trong khi giữ E; damage hủy channel, hủy không tạo lại wave/địch chết; CaptureChip và reward ledger chưa có |
| MIS-05 | P0 | PARTIAL prototype | Retry checkpoint trong phiên giữ ID dữ liệu/tutorial/carrier đã xác nhận; wave đang thử reset; chưa có save/recovery campaign/result tổng |
| SAV-01 | P1 | TODO | Save schema/version, snapshot nguyên tử và phục hồi lỗi; world/decision campaign thuộc Host, guest profile riêng |
| SAV-02 | P1 | TODO | Reward ledger/idempotency xuyên retry/reconnect/crash, unlock city/tier một lần; tích hợp transaction backend khi có |
| MAP-00 | P1 | TODO | Prologue/NOMAD và placement campus hư cấu có briefing phù hợp; không tự biến CTU thành Core MEKONG |
| MAP-01 | P1 | TODO | Dòng nước bị khóa + Những cánh đồng không người; cứu dân/máy phát/revive/chuyển chip, agri evidence, đường đi thực |
| MAP-02 | P1 | TODO | Áp lực: 3 pump Control Node trước forced flood; class bonus không khóa cứng nhiệm vụ |
| MAP-03 | P1 | TODO | Dây chuyền thép: hack hoặc phá sản xuất có hậu quả tài nguyên/attitude; spawn theo state |
| BOS-01 | P1 | TODO | MEKONG Gatekeeper: nước/điện/shield, 3 ổn áp; Host phase/damage, telegraph rõ |
| BOS-02 | P1 | TODO | Flood Logic: van/bơm/cao độ/defend và state nước đồng bộ |
| BOS-03 | P1 | TODO | Core Exposure: Sync/defend/chuyển chip nếu carrier gục; không phải boss robot demo đổi HP |
| DEC-01 | P1 | TODO | Alliance/Destroy vote mỗi active player một phiếu; hòa carrier phân xử; xác nhận trước commit; chốt case mất người/timer |
| PRO-01 | P1 | TODO | Signed MEKONG Key hoặc Raw Hydro Fragment; cả hai nhận Core Data/Tier2 và tới Tokyo, hậu quả world state khác nhau |
| HUB-01 | P1 | TODO | NOMAD loadout/sửa/briefing, ARKLINE/Departure Operation; all-Destroy vẫn còn một tuyến bay khả thi |
| THR-01 | P1 | TODO | Encounter Alert/Mission Deadline Meter/Campaign Threat; ngưỡng 0–25/26–50/51–75/76–99/100 và sự kiện tăng/giảm |
| AI-01 | P1 | TODO | Telemetry encounter/session + rule/utility Director whitelist/cap/log lý do; counter không vô hiệu toàn bộ chiến thuật |
| NAR-01 | P1 | TODO | Lore/faction/loyalty, 3 Lore Key có đường lấy; danh tính HELIOS chỉ epilogue; không spoil trong campus logs |
| DAT-01 | P1 | TODO bắt buộc | ASP.NET Core + MySQL: account/profile/auth, schema/migration và progression/reward transaction; không coi UGS anonymous là đã xong |
| DAT-02 | P1 | TODO bắt buộc | API client/cache/offline/reconcile, quyền sở hữu profile, grant/reconnect idempotency; không nhận tùy ý reward từ Host |
| CTY-01 | P1 | TODO | CityDefinition + composition Mission/Boss/Decision/WorldState và validator; kiểm tra cả hai nhánh/reward/save/scenecatalog/budget |
| CAM-01 | P2 | TODO bắt buộc | Tokyo: 5 nhiệm vụ, PredictionZone/Mirror, KAGAMI, Alliance/Destroy/Tier3, hub và epilogue |
| CAM-02 | P2 | TODO bắt buộc | Shanghai: 5 nhiệm vụ, logistics/production, DRAGON, hai nhánh/Tier4, hậu quả |
| CAM-03 | P2 | TODO bắt buộc | London: 5 nhiệm vụ, audit/fake data, MERLIN; lấy HELIOS Directive trước Boss, Destroy không khóa good ending |
| CAM-04 | P2 | TODO bắt buộc | New York: 5 nhiệm vụ, power/military/faction/ATLAS; GlobalAccessKey trước decision, Tier6 ở cả hai nhánh |
| REV-01 | P2 | TODO | Campaign Revisit giữ canon Host; guest chỉ nhận progression/reward hợp lệ, không đổi quyết định campaign riêng |
| RPL-01 | P2 | TODO | ECHO-7 Mission Replay là simulation, không ghi đè canon/nhân Signed Key/Raw Fragment; reward chống farm |
| REV-02 | P2 | TODO | Guardian Proxy khác Guardian gốc; WorldTier/ThreatCeiling/reward rõ; xử lý mâu thuẫn ví dụ nguồn trước khi code scaling |
| END-01 | P2 | TODO | Global Network 4 phase; Capsule khóa dock, Sync token số có handoff, support tối đa 2 active |
| END-02 | P2 | TODO | CONTROL/DESTROY/LAST PROTOCOL + epilogue; baseline good ending có cấu hình, Integrity >50 tại đầu Last Sync |
| QA-01 | P0→P2 | TODO module mới | Test authority/transition/duplicate/state restore; mở rộng theo từng chặng, không coi test Recon là test full campaign |
| QA-02 | P0→P2 | TODO module mới | Gameplay thật 2/3/4 máy Relay, transfer/reconnect/crash/retry/performance; lưu build hash/log, case chưa chạy ghi CHƯA KIỂM THỬ |
| UX-01 | P2 | TODO | Input binding, accessibility, readable HUD/telegraph, error/retry flow và localization |
| OPT-01 | Sau phạm vi | Tùy chọn | Leaderboard/achievement nếu còn thời gian; không chặn nghiệm thu campaign |

## Thứ tự làm ngay sau cập nhật thiết kế

1. INT-01 → INT-02 → INT-03, cùng MIS-06 theo lát nhỏ, giữ Recon test làm hồi quy.
2. CLS-01 + CHP-01/02/03/04 và REC-01; chốt timer bằng cấu hình và test race, không ghi số tùy ý thành canon.
3. Encounter, checkpoint/result và đo thật; ghép đoạn campus vào Dòng nước bị khóa.
4. Hoàn chỉnh Cần Thơ/Boss/decision/save/hub; backend và framework trước khi nhân sang bốn chương quốc tế.

Các dependency thực tế có thể đan xen; không phải yêu cầu triển khai toàn bộ lớp abstraction trước một lát chơi được. Ticket DONE cần test và tài liệu cho đúng version. Xem [lộ trình](STORY_IMPLEMENTATION_ROADMAP.md) và [thiết kế phân đoạn campus](CANTHO_FIRST_MISSION_DESIGN.md).
