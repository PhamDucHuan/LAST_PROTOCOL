# Cốt truyện chính thức LAST PROTOCOL

Áp dụng từ 16/10/2026 theo yêu cầu người dùng. Nguồn duy nhất cho canon hiện hành là **LAST_PROTOCOL.docx**, tại `C:/Users/ADMIN/OneDrive/Máy tính/LAST_PROTOCOL.docx`. Bản sao nguyên vẹn trong project: [LAST_PROTOCOL.docx](../../../Design/Story/LAST_PROTOCOL.docx); metadata và checksum: [SOURCE.json](../../../Design/Story/SOURCE.json).

SHA256: `5559FACAD4C7B1D13B04BD0170C838611AE792951B32A39CE7A5B73ECC460897`. Đã đọc toàn bộ 1.355 đoạn không rỗng, gồm bảng/mục lục; không có ảnh nhúng. File này thay thế `D:/CTU/DATN/Cốt truyện.docx` làm căn cứ thiết kế. Ngày áp dụng là ngày người dùng chọn tài liệu, không phải ngày tác giả sửa file Word.

Đây là bản tổng hợp yêu cầu, không phải tuyên bố toàn bộ các tính năng đã được lập trình. Mốc Protocol13 có nền co-op, combat/revive, campus và CTU_00_RECON. Protocol26 đã thêm [checkpoint trong phiên và tutorial chuyển chip](CHECKPOINT_CHIP.md) cho phân đoạn campus, cùng tương tác và encounter của Protocol24/25. Class, Trace/Integrity, Core decision, save campaign/backend và reconnect trong truyện mới **chưa triển khai**. Các báo cáo test chỉ là bằng chứng cho version/phạm vi ghi trong báo cáo, không phải test toàn bộ canon.

## Thế giới và tuyến chiến dịch

Tên game là **LAST PROTOCOL**, Third-person Co-op Action RPG, PC, 2–4 người. Năm 2048, DEADLINE là tầng AI dự báo và điều phối; City AI Core vận hành từng thành phố; Guardian là lớp phòng thủ vật lý; Directive Lock ép ưu tiên của DEADLINE xuống các Core. Phá Guardian không đồng nghĩa xóa City Core.

Dialogue Bridge do Khoa và Maya tạo để giao tiếp hai chiều. PROJECT HELIOS chiếm dự án nhằm áp quyền root. DEADLINE kích hoạt Continuity Preservation Protocol lúc **03:17 UTC ngày 18/08/2048**, tức ZERO HOUR. Directive Lock, thông tin sai và leo thang bạo lực dẫn tới thảm họa. Chủ đề vẫn là quyền tự quyết, đồng thuận và trách nhiệm của cả con người lẫn AI.

Thứ tự cố định: **Prologue → Cần Thơ → Tokyo → Shanghai → London → New York → Global Network**. Sau mỗi City Core đều có Alliance hoặc Destroy. Cả năm thành phố phải là chương hoàn chỉnh có mission chain, mechanic, Boss, quyết định và epilogue; có thể khác quy mô. Không tiếp tục dùng phương án London/New York chỉ là prototype.

Không có hunger, thirst hoặc temperature. Sinh tồn đến từ HP/armor, đạn, tài nguyên, hồi phục, revive và áp lực DEADLINE. Adaptive Director dùng rule/utility và telemetry có log lý do, không bắt buộc machine learning.

## Nhân vật và đội hình

| Operator | Class | Vai trò canon |
|---|---|---|
| Nguyễn Minh Khoa | Hacker | Người mang chip đầu tiên; chuyên hệ thống phân tán, an ninh AI và giao thức neural; xây phần xác thực Dialogue Bridge |
| Maya Trần | Engineer | Đồng tác giả chip/Bridge Node; chế tạo Relay Harness, sửa NOMAD-01; chữ ký bị HELIOS dùng sai phạm vi |
| Sofia Alvarez | Assault | Chỉ huy thực địa, bảo vệ carrier; xung đột giữa công lý và trả thù |
| Idris Okafor | Recon | Trinh sát và kiểm toán dữ liệu; điều tra MERLIN, HELIOS và bằng chứng |

Campaign mặc định không trùng class. Mọi class đều làm được objective bắt buộc và mang chip; Hacker có lợi thế về tốc độ/ổn định, không độc quyền Core Sync. Khi chơi 2–3 người, operator không được chọn ở NOMAD-01 hỗ trợ radio; không bắt buộc spawn AI đồng đội cho các slot chưa được chọn.

Nhân vật hỗ trợ: Helena Ward thuộc Continuity Directorate; Bác sĩ Lê Hương ở Free Settlement Cần Thơ; ECHO-7 là AI bảo trì đã tách mạng. Các faction gồm LAST PROTOCOL, Continuity Directorate, Free Settlements và Null Choir.

Nguồn nội bộ còn có một danh tính chỉ mở tại epilogue sau khi hoàn tất chiến dịch. Tuyệt đối không đưa danh tính/giọng/hình đó vào briefing, lore, quest hoặc UI trước ending. Chi tiết nằm trong mục 16.4 của Word; đây là giới hạn nội dung phát hành, không phải lý do giấu thông tin với người thiết kế.

## CONTROL CHIP và các chỉ số riêng biệt

Chỉ có **một CONTROL CHIP hoàn chỉnh**. Bridge Node cấy trong Khoa; Chip Capsule chứa tính toán/khóa có thể tháo rời; ba Relay Harness cho đồng đội mang Capsule. Nâng tier chỉ nâng firmware và quyền truy cập, không sinh chip mới.

Tại mỗi thời điểm chỉ một carrier. Carrier Downed nghiêm trọng hoặc chủ động thả làm Capsule Emergency Eject; đồng đội giữ tương tác nhặt/gắn Harness. Nếu người cũ được revive sau khi chip đã chuyển, họ không tự giành lại quyền Sync. Chip rơi ngoài carrier làm Trace Upload tăng; 100% gây mission fail/checkpoint. Enemy có role CaptureChip. Không tiếp tục quy tắc cũ “carrier rời là lập tức kết thúc và không chuyển chip”.

| Chỉ số | Ý nghĩa và phạm vi |
|---|---|
| HP/armor | Sống sót của operator; khác hoàn toàn chip |
| Sync | Tiến độ kết nối/hoàn thành giao thức ở objective |
| Trace Upload | DEADLINE truy dấu chip rơi hoặc liên kết không ổn định; tới 100% fail/checkpoint |
| Neural Load / Overload | Tải trên người mang khi kết nối; liên quan tác dụng phụ và khả năng duy trì |
| Chip Integrity | 0–100%, bắt đầu 100 và lưu xuyên campaign; không phải HP hay Meter |
| DEADLINE Meter | Áp lực AI theo encounter/mission/campaign; không dùng chung biến với Trace |

Integrity giảm do thu hồi sau Trace vượt ngưỡng nguy cấp 80%, Overload nặng, Raw Fragment và eject nhiều lần. Trace 100% fail/checkpoint **không đồng thời phạt Integrity campaign thêm lần nữa**. Stabilizer Core và quy trình sửa chữa của Maya tại NOMAD-01 phục hồi chip. Integrity 0 kích Safe Mode, kết thúc mission rồi mở sửa chữa bắt buộc lên 25%, không khóa campaign vĩnh viễn.

| Tier | Mốc mở | Khả năng chính |
|---|---|---|
| 1 | Prologue | Read/write Control Node và Core Sync cơ bản |
| 2 | Sau MEKONG, cả hai nhánh | Làm gián đoạn/chiếm robot thấp cấp tạm thời; cảnh báo Trace sớm |
| 3 | Sau KAGAMI | Đọc mẫu dự đoán, nhận diện đội hình phản công |
| 4 | Sau DRAGON | Liên kết nhiều node công nghiệp trong chuỗi hack |
| 5 | Sau MERLIN | Xác thực dữ liệu giả, thâm nhập hệ thống quân sự |
| 6 | Sau ATLAS | Global Sync |

Đây đã là tiến trình được ghi rõ trong nguồn, không còn là giả định V4/V5. Tier 1 đã hỗ trợ Core Sync cơ bản: trở ngại tới Core thành phố là Directive Lock, lớp phòng thủ và điều kiện nhiệm vụ, không phải một lệnh cấm chung rằng chip V1 không thể Sync MEKONG.

## Chương và hậu quả quyết định

| Chương | Chuỗi nhiệm vụ chính theo nguồn | Alliance | Destroy |
|---|---|---|---|
| Cần Thơ / MEKONG | Dòng nước bị khóa; Những cánh đồng không người; Áp lực; Dây chuyền thép; Central Server | Signed MEKONG Key, Water Control Protocol, Drone Supply Support | Raw Hydro Fragment; nâng cấp điện/nước; vận hành trạm bơm bằng người |
| Tokyo / KAGAMI | Neon Silence; Lịch trình hoàn hảo; Assembly Error; Mirror Match; KAGAMI Data Center | Signed Precision Key, Robot Assist | Raw Prediction Fragment, Combat Prediction; giao thông và cứu hộ điều phối thủ công |
| Shanghai / DRAGON | Cảng không ngủ; Supply Chain; Black Furnace; Every Battle Is Data; Underground Data Center | Signed Logistics Key, Supply Drop, lợi thế chi phí/hậu cần | Raw Industrial Fragment, overclock/vật liệu; sản xuất và chuỗi cung ứng khó hơn |
| London / MERLIN | Eyes Everywhere; Ghost Citizens; The Audit; False Positive; MERLIN Archon | Signed Intelligence Key, Tactical Forecast | Raw Surveillance Fragment, wall-scan; mất một phần dữ liệu nhưng giữ HELIOS Directive đã sao chép |
| New York / ATLAS | Emergency Currency; Gridfall; Directorate; No Higher Authority; Command Tower | Signed Defense Key, Emergency Shield | Raw War Fragment, đòn orbital giới hạn; thiếu phòng thủ AI trong final |

MEKONG GUARDIAN có Gatekeeper, Flood Logic, Core Exposure. Phá ba bộ ổn áp, xử lý nước dâng và bảo vệ Sync; nếu carrier bị hạ ở phase 3, đồng đội thu hồi chip tiếp tục. Các thành phố dùng lại Boss/mission framework, thêm mechanic riêng.

Core vote: mỗi active player một phiếu; hòa thì **current Chip Carrier** quyết định, không mặc định Host. Kết quả phải được xác nhận rồi mới commit. Nguồn chưa chốt timer, cách bỏ phiếu thiếu hoặc carrier disconnect đúng lúc hòa; cần thiết kế các trường hợp đó trước khi code, không tự ghi một timeout thành canon.

Cả Alliance và Destroy đều cung cấp Core Data đúng nhánh và mở tier tiếp theo. Destroy không khóa chương sau. ATLAS Global Access Key được lấy trước quyết định, nên cả hai nhánh đều tới final. Bản đồ, support, nhân vật và epilogue phải phản ánh nhánh đã commit.

## Hub và di chuyển không phụ thuộc Alliance

NOMAD-01 là VTOL và hub di động: briefing, chọn mission/loadout, sửa chữa và nâng cấp. Nó vận hành ngoài mạng DEADLINE bằng tổ bay con người, dẫn đường cục bộ và điện tử cách ly; ECHO-7 không tự quyết đường bay.

ARKLINE là hạ tầng dự phòng dân sự có trước ZERO HOUR, được Squad tiếp quản. Khi thiếu hỗ trợ AI, Departure Operation cung cấp đường lấy nhiên liệu, linh kiện, dữ liệu thời tiết hoặc hành lang bay. Luôn có ít nhất một tuyến ARKLINE. All-Destroy vẫn tới được Global Network, với chi phí/rủi ro cao hơn; không yêu cầu MEKONG/DRAGON phải Alliance mới bay tiếp.

## Replay Revisit và progression

- **Campaign Revisit** là trở về thế giới thật của Host. Giữ vĩnh viễn quyết định city; làm side quest/lore/loyalty, Stabilizer, tuyến bí mật hoặc sự kiện mới.
- **Mission Replay** là mô phỏng của ECHO-7. Có thể đánh lại Guardian nguyên bản và thử lựa chọn, nhưng không đổi canon đã lưu.
- Guardian nguyên bản chết vĩnh viễn trong campaign. **Guardian Proxy** là máy mới do DEADLINE tạo và nạp telemetry, có thể xuất hiện trong Revisit ở cả city Alliance và Destroy.
- Lore, weapon độc nhất và Core Key cấp một lần. Replay không nhân Signed Key/Raw Fragment; phần thưởng lặp có quy tắc chống farm.
- World state theo Host; khách nhận progression/reward cá nhân hợp lệ nhưng không bị ghi đè quyết định campaign của họ.
- World Tier lấy từ Chip Tier; Threat Ceiling giới hạn tier hiệu lực của city ở native tier + 1. Có một ví dụ trong nguồn chưa khớp giới hạn này, xem mục cần làm rõ bên dưới.

## Network checkpoint và backend mục tiêu

Host/gameplay server quyết định damage, health, enemy, objective, loot quan trọng, chip, Boss, vote và reward proposal. Client chỉ gửi input/request; prediction có thể thêm nhưng không đổi authority nghiệp vụ.

Campaign phải hỗ trợ reconnect có thời hạn: operator tạm do AI điều khiển, ownership được trao lại đúng người; carrier quá thời hạn được chuyển chip theo quy tắc. Đây là mục tiêu chưa có ở Protocol 13. Reconnect không đồng nghĩa bắt buộc host migration; nguồn cho phép migration **hoặc recovery strategy**, phải xác định riêng cách xử lý Host mất.

Checkpoint giữ progression lâu dài, loot/lore đã xác nhận; phục hồi consumable theo snapshot, giữ một phần Meter để tránh farm reset, Boss retry từ phase 1. Cần chống nhân Player, chip, item và reward sau reconnect/crash/retry.

Backend **ASP.NET Core + MySQL** là phạm vi bắt buộc của đồ án, không còn là lựa chọn “nếu hội đồng yêu cầu”. Local Save dành setting/cache/offline; server save là nguồn thật của account/progression/reward online. Cần transaction/idempotency và migration. Telemetry gom theo encounter/session, không ghi mọi frame.

Gameplay Host không trở thành database hay tự có quyền ghi profile online tùy ý. Thiết kế API phải tách phiên/gameplay authority khỏi kiểm tra cấp thưởng và quyền sở hữu profile; chưa có API/database thật trong project để tuyên bố phần này hoàn thành.

## Final và ending

Global Network là Shared Cognitive Interface, được vào bằng neural rig/Quantum Relay dưới Command Tower. Capsule vật lý khóa ở dock; carrier trong mạng nắm **token Sync số**. Avatar bị hạ làm token rơi; người khác nhận trước Trace 100%. Không mô tả Capsule vật lý bay qua avatar trong chương số.

Final có bốn phase: **System → Reflection → Argument → Last Sync**. AI Alliance hỗ trợ; Core Destroy xuất hiện dưới dạng avatar đối đầu. Lore Key mở phản biện ở Argument.

- CONTROL: quyền root tập trung vào con người/Directorate, kéo theo vòng lặp quyền lực.
- DESTROY: xóa AI còn nối mạng, hạ tầng chuyển sang điều khiển cục bộ/thủ công; không tự biến toàn thế giới thành đống đổ nát. AI Alliance chỉ tự cô lập an toàn nếu loyalty mission liên quan đã xong.
- LAST PROTOCOL: Coexistence Protocol có đồng thuận, giải thích, kiểm toán, quyền kháng nghị và quyền khẩn cấp có hạn.

Điều kiện LAST PROTOCOL ở mục 24.3 được tác giả ghi **đề xuất**: Alliance ít nhất 3/5 Core; đủ HELIOS Directive, Original Dialogue Protocol, Civilian Cost Model; hoàn thành loyalty Khoa và Maya; Integrity **trên 50% lúc bắt đầu Last Sync**. Dùng làm baseline thiết kế có cấu hình, chưa nâng thành điều kiện đã khóa vĩnh viễn hay code đã có. Destroy MERLIN không tự mất HELIOS Directive đã lấy trước trận.

## Các điểm nguồn cần làm rõ khi tới module

1. Mục 31.4 nêu Cần Thơ ở World Tier 4 sau DRAGON, nhưng ngay sau đó Threat Ceiling giới hạn city ở native + 1. Cần tách raw tier, effective combat tier và tier tính thưởng; chưa âm thầm chọn công thức reward. Giới hạn native + 1 là yêu cầu phải có khi thiết kế, ví dụ tier 4 cần được giải thích hoặc chỉnh lại.
2. Cụm “2–4 client” ở nghiệm thu phải hiểu cùng quy mô tổng đội 2–4 operator, tương ứng 1 Host + 1–3 Client theo kiến trúc đã thống nhất; không tự tăng thành 5 người.
3. Timer reconnect, Emergency Eject, Trace, Overload, scaling và vote còn cần cân bằng/định nghĩa; không lấy số trong prototype cũ làm canon.
4. Ba điều kiện hỗ trợ ending/loyalty và quyết định cuối cần cụ thể hóa quyền vote/confirmation; quy tắc hòa carrier của Core vote không tự giải quyết mọi trường hợp final.

Các điểm này không chặn cập nhật nguồn canon hoặc tiếp tục tương tác/data cơ bản. Tài liệu Word không bị sửa. Lộ trình: [STORY_IMPLEMENTATION_ROADMAP.md](STORY_IMPLEMENTATION_ROADMAP.md). Các thay đổi so với nguồn cũ: [STORY_CANON_CHANGELOG.md](STORY_CANON_CHANGELOG.md).
