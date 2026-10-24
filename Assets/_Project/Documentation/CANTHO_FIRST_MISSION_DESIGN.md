# Phân đoạn mở đầu tại campus Cần Thơ

Protocol26 bổ sung [checkpoint trong phiên, một chip và relay drill](CHECKPOINT_CHIP.md). Drill sau ba bản ghi, trước máy phát; carrier gục → đồng đội thu hồi E → nhả E để revive; chỉ carrier hiện tại channel uplink. Vị trí hồi, giữ ID dữ liệu và carrier khi retry được chốt trong tài liệu đó. Class/Trace/Integrity, save bền vững và cứu dân còn thiếu.

Protocol25 đã thêm [encounter robot bảo vệ uplink](UPLINK_DEFENSE.md) vào đoạn bốn bước.

Triển khai 21/10/2026, Protocol24: theo yêu cầu người dùng, đã ghép đoạn bốn bước tập hợp → ba bản ghi → máy phát → liên lạc. [Hướng dẫn](RESTORE_UPLINK_SYSTEM.md) mô tả phạm vi build đó; Protocol26 bổ sung như trên. Sáu mục tiêu và cứu dân bên dưới vẫn là thiết kế đích.

Cập nhật thiết kế 16/10/2026 theo [STORY_CANON.md](STORY_CANON.md) / LAST_PROTOCOL.docx mới. Phần sáu mục tiêu dưới đây là **thiết kế đích, chưa triển khai đầy đủ**; phân đoạn bốn bước Protocol24 được ghi riêng ở trên. Không thay nội dung chính thức của Word bằng ý tưởng mới.

## Vị trí trong chương Cần Thơ

Nguồn xác định nhiệm vụ đầu **Dòng nước bị khóa**: cứu Free Settlement, khôi phục máy phát và hướng dẫn revive/chuyển chip. Campus có thể dùng cho một phân đoạn khôi phục liên lạc và tiếp cận người sống sót trong nhiệm vụ đó. Việc đặt một trạm hỗ trợ, khu trú tạm hoặc tuyến cứu dân ở CTU là bổ sung hư cấu cần thể hiện rõ, chưa phải địa điểm được nguồn xác nhận. Không khẳng định trường thật có Core hay phòng lab bí mật.

Giữ ID kỹ thuật `CTU_01_RESTORE_UPLINK`, tên UI dự kiến **Khôi phục liên lạc**. Dùng parent mission ID riêng cho Dòng nước bị khóa khi tạo campaign catalog, tránh thêm nhiệm vụ chính thứ sáu. Chuỗi canon vẫn là Dòng nước bị khóa → Những cánh đồng không người → Áp lực → Dây chuyền thép → Central Server.

`CTU_00_RECON` hiện có chỉ là prototype ba Rendezvous ở Protocol13; [hướng dẫn hiện tại](CAMPUS_RECON_SYSTEM.md) vẫn đúng với build đó. Recon không tự trở thành phân đoạn sáu mục tiêu dưới đây sau khi đổi tài liệu.

ZERO HOUR đã xảy ra. Khoa là carrier đầu tiên; Maya, Sofia và Idris có vai trò theo class. Chip Tier1 hỗ trợ Control Node và Core Sync cơ bản. Việc chưa tiếp cận MEKONG ở phân đoạn này do phòng thủ/Directive Lock và tiến độ nhiệm vụ, không phải Tier1 bị cấm Sync mọi City Core.

Đoạn campus kết thúc với tuyến liên lạc/dữ liệu cục bộ đã phục hồi, không có Signed MEKONG Key/Raw Hydro Fragment, không nâng Tier2 và không khôi phục toàn MEKONG. Những kết quả đó thuộc Central Server sau Boss và Core decision.

## Lát chơi sáu mục tiêu đề xuất

Tọa độ dưới đây là mốc Recon hiện có để khảo sát vị trí, chưa phải terminal/rescue prefab đã dựng. Khi triển khai phải kiểm tra collider, nền, NavMesh, camera và đường đi, không chặn tuyến chơi hiện tại.

| ID kỹ thuật | Mục tiêu | Vị trí gợi ý | Host xác nhận |
|---|---|---|---|
| CTU01_01 | Tập hợp, nhận tín hiệu cứu hộ | Cổng B, X0 Z−40 | Đội tới vùng hợp lệ; bắt đầu briefing, chưa cấp reward campaign |
| CTU01_02 | Thu ba bản ghi cục bộ | Ngoài nhà điều hành, quanh X32 Z−21 | Data node ID khác nhau, mỗi node chỉ cộng một lần; nội dung hướng tới nơi cần cứu/cấp điện |
| CTU01_03 | Khởi động máy phát/cấp điện phụ | Rìa sân trên tuyến sang Học liệu | Hold hợp lệ, kiểm tra nguồn điện/switch và phase; Maya có lợi thế nhưng class khác vẫn làm được |
| CTU01_04 | Mở tuyến an toàn cho cứu hộ | Gần X40 Z−22 | Encounter riêng hoàn tất, công tắc phòng thủ mở; gắn cứu dân vào objective của nhiệm vụ cha |
| CTU01_05 | Sync uplink và giữ tuyến | Sảnh Học liệu, gần X57 Z−42 | Carrier channel, đồng đội phòng thủ; xử lý eject/pickup/chuyển và tiếp tục theo checkpoint đã chốt |
| CTU01_06 | Tập hợp tại điểm rút/chuyển nhiệm vụ | Sân trong cổng B, vị trí cần kiểm tra | Hoàn tất phân đoạn một lần, lưu kết quả cục bộ/campaign đúng phạm vi, không tự kết thúc cả chương |

Ba data node và sáu mục tiêu là lựa chọn sản xuất cho map hiện có, không phải số lượng canon. Các bản ghi chỉ cung cấp bối cảnh cứu dân/hạ tầng; không cấp sớm HELIOS Directive, Original Dialogue Protocol, Civilian Cost Model hoặc tiết lộ danh tính dành cho epilogue.

Đây chưa phải toàn bộ Dòng nước bị khóa: nhiệm vụ cha vẫn cần kết quả cứu Free Settlement, máy phát và tutorial revive/chuyển chip rõ ràng. Nếu đặt thêm người sống sót ở campus không hợp lý, đưa checkpoint cứu dân ra khu trú kế tiếp; không đánh dấu mission chính Complete chỉ bằng sáu objective campus.

## Tương tác và objective

Dùng interaction contract Can/Begin/Update/Cancel/Complete với stable ID. Client gửi request; Host kiểm tra người gửi/ownership, còn sống, khoảng cách, phase, trạng thái nguồn và rate limit. Host tính thời gian channel; UI chỉ hiển thị state, không tự hoàn thành khi thanh chạy hết.

Cần một action lock chung với revive/combat/movement. Một lần giữ E không vừa cứu đồng đội vừa đọc terminal. Target selection, thứ tự ưu tiên và hủy hành động phải rõ; input mất focus không để server giữ channel vô hạn. Không thêm PlayerInput/AudioListener/NetworkManager cạnh tranh.

Objective graph mới dùng Locked/Available/Active/Completed/Failed/Skipped và dependency; MissionPhase cấp mission hiện tại tiếp tục phân biệt None/Active/Completed/Failed. Dữ liệu Rendezvous cũ cần migration/version. Đổi chữ trong tài liệu chưa biến MissionDefinition hiện có thành graph.

Nên làm INT-01/02 và một data node trước; sau khi authority/duplicate request được kiểm tra mới nhân ba node và ghép máy phát. Không cần triển khai inventory đầy đủ để có interaction mở đầu.

## Chip, gục và reconnect

- Chỉ một Capsule và một carrier. Khoa mang ở đầu campaign; mọi class có Harness và dùng được. Host không mặc nhiên là carrier.
- Carrier gục nặng/chủ động thả → Emergency Eject; đồng đội hold nhặt/gắn. Hai request đồng thời phải được Host xử lý nguyên tử.
- Revive người cũ sau transfer không giành lại quyền Sync. Mất carrier ở phase bảo vệ phải có đường thu hồi, không tự fail chỉ vì người đầu tiên gục.
- Capsule rơi làm Trace tăng; Trace100 dẫn fail/checkpoint. Integrity được lưu xuyên campaign và không bị cộng thêm phạt trùng tại cùng sự kiện Trace100.
- Sync, Trace, NeuralLoad, Integrity, HP và Deadline Meter là state khác nhau. ChipTier không phải số bản sao chip.
- Client mất kết nối: active operator tạm do AI điều khiển trong reconnect window; trả ownership đúng stable profile nếu trở lại; quá hạn thì auto transfer chip theo quy tắc. Không dùng “carrier disconnect → fail ngay” làm thiết kế campaign.
- Slot class không chọn từ đầu ở NOMAD hỗ trợ radio; không nhầm với AI tiếp quản active operator bị mất mạng.

Word chưa chốt timer/khoảng cách/Trace. Protocol26 dùng các giá trị prototype được ghi trong CHECKPOINT_CHIP.md: recovery 2 s, drill 1.5 s, range 2.5 m, buddy 5 m, damage hủy hold. Drill tạo cơ hội an toàn học transfer; Trace chưa có nên không hiển thị timer giả.

Protocol13 chưa có transfer/reconnect/checkpoint. Hành vi Host thoát kết thúc phiên và Recon restart toàn lượt vẫn được mô tả đúng trong hướng dẫn hiện hành; không coi việc sửa thiết kế là đã sửa runtime.

## Encounter và checkpoint

Tái sử dụng hình dạng robot cho encounter nhỏ với định danh/cấu hình riêng; không gọi đó là MEKONG GUARDIAN. Tách spawner demo khỏi mission spawner. Wave có ID, cap, NavMesh đã kiểm tra và state lưu; hủy channel không spawn lại một wave đã kích hoạt.

Checkpoint mới cần lưu objective graph, collected IDs, encounter, chip, carrier stable ID, meter và reward ledger nhất quán. Confirmed loot/lore giữ; consumable theo snapshot; giữ một phần Meter; không dùng chết để farm reset. Cách phục hồi tình trạng người chơi/chip cụ thể phải chốt trước khi code, tránh snapshot chứa carrier không tồn tại.

Chip Integrity0 mở Safe Mode rồi sửa bắt buộc tại NOMAD lên25, không khóa campaign. Tránh cấp key/nâng tier như giải pháp tạm để phá softlock campus.

## Phần sau của chương

Dòng nước bị khóa dẫn sang nông nghiệp, trạm bơm và nhà máy rồi Central Server. MEKONG GUARDIAN có Gatekeeper với 3 ổn áp, Flood Logic, Core Exposure. Core vote sau Boss: mỗi active player một phiếu; hòa thì current carrier phân xử; xác nhận trước commit.

Alliance nhận Signed MEKONG Key/WaterControl/DroneSupply. Destroy nhận Raw Hydro Fragment và nâng cấp tương ứng, hậu quả vận hành nước thủ công. Cả hai đều nâng Tier2 và mở Tokyo. Không dùng Restore-only hoặc Alliance như điều kiện bắt buộc để rời Cần Thơ; NOMAD/ARKLINE luôn có đường đi tiếp kể cả all-Destroy.

## Nghiệm thu sau khi lập trình

| Nhóm | Kiểm tra phải thực hiện | Hiện trạng |
|---|---|---|
| Objective/authority | Request sai ID, xa, gục, sai phase, duplicate; hai Client cùng tương tác; graph/reset | Protocol26 kiểm tra chuỗi tuyến tính và retry trên localhost 2/4 người; graph nhánh chưa có |
| Chip | Eject/pickup đồng thời, chuyển, revive người cũ, Trace100, Safe Mode | Protocol26 kiểm tra eject/pickup/revive/retry và carrier độc lập Host; Trace100/Safe Mode chưa có |
| Mạng | 2/3/4 máy qua Relay, disconnect/reconnect, carrier timeout, Host recovery | CHƯA KIỂM THỬ cho CTU_01 |
| Encounter | Wave không lặp, cap, đường đi/camera, frame time Client/Host | Wave ba robot có kiểm tra Protocol25/26; đo tải nhiều máy thật còn thiếu |
| Save | Wipe/checkpoint, crash/retry, không nhân reward, guest canon không bị đổi | Checkpoint trong phiên và không cộng trùng dữ liệu có kiểm tra Protocol26; disk/crash/reward/guest profile chưa có |
| Canon | Đúng vị trí mission cha, đủ cứu dân/máy phát/tutorial; không cấp Core Key sớm | Cần kiểm tra nội dung khi xây level |

Protocol26 thêm một NetworkChip vào Room và nhãn build menu; scene/layout/NavMesh giữ nguyên. Công việc còn lại và ưu tiên ở [backlog](STORY_IMPLEMENTATION_BACKLOG.md).
