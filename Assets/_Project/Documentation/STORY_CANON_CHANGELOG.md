# Thay đổi nguồn cốt truyện — 16/10/2026

Theo yêu cầu người dùng, **LAST_PROTOCOL.docx** tại `C:/Users/ADMIN/OneDrive/Máy tính/LAST_PROTOCOL.docx` thay `D:/CTU/DATN/Cốt truyện.docx` làm nguồn hiện hành. Không sửa nội dung Word gốc. Bản sao byte-identical và checksum được giữ tại [Design/Story](../../../Design/Story/SOURCE.json). Đã đọc toàn bộ 1.355 đoạn không rỗng, không có ảnh nhúng.

Bản tổng hợp: [STORY_CANON.md](STORY_CANON.md). Ngày này là ngày áp dụng do người dùng lựa chọn, không phải khẳng định ngày tác giả chỉnh Word.

## Thay đổi ảnh hưởng thiết kế

Cột trước mô tả kế hoạch/giả định trong project trước lần cập nhật này, không khẳng định mọi ý đó đều là lời của tài liệu Word cũ.

| Trước trong kế hoạch | Theo nguồn mới / việc cần làm |
|---|---|
| Carrier gần như cố định, rời là fail | Một chip có Emergency Eject, mọi class dùng Harness, transfer/reconnect; timeout chuyển chip |
| Mô tả V1 chưa Sync được City Core nói chung | Tier1 đã có Core Sync cơ bản; defense/Directive Lock/objective là rào tiếp cận |
| Cần Thơ hướng Restore/Alliance cố định | MEKONG cũng Alliance/Destroy; cả hai cho Core Data, Tier2 và Tokyo |
| Các tier sau còn giả định | V1–V6 có mốc Prologue/MEKONG/KAGAMI/DRAGON/MERLIN/ATLAS |
| Vai trò operator chưa đầy đủ | Khoa/Hacker, Maya/Engineer, Sofia/Assault, Idris/Recon; campaign mặc định không trùng class; mọi class làm objective bắt buộc |
| Disconnect chưa có vòng phục hồi | AI tạm điều khiển active operator, ownership theo stable identity, reconnect window rồi chip transfer |
| Backend có thể để tùy chọn | ASP.NET Core + MySQL là phạm vi bắt buộc; server save online, local cache/offline, transaction/idempotency |
| London/New York có thể chỉ prototype | Cả 5 thành phố là chương chơi hoàn chỉnh, dù district có thể nhỏ hơn Cần Thơ |
| Chọn Core theo Host hoặc chỉ gợi ý | Active player vote, hòa carrier phân xử, confirmation trước commit; edge case còn cần chốt |
| Chưa rõ đường đi khi Destroy nhiều Core | NOMAD-01, ARKLINE và Departure Operation bảo đảm all-Destroy vẫn tới final |
| Replay chưa phân biệt world state | Revisit thế giới canon Host; ECHO-7 Replay là mô phỏng, không đổi decision hoặc nhân Core Key |
| Guardian xuất hiện lại dễ hiểu như hồi sinh | Guardian gốc chết; Guardian Proxy là frame mới dùng telemetry |
| Chip chỉ là thanh channel | Sync/Trace/NeuralLoad/Integrity/Deadline riêng; Integrity campaign và Safe Mode, không phạt kép Trace100 |
| Final chưa chi tiết | 4 phase System/Reflection/Argument/Last Sync; Capsule tại dock, handoff token số trong mạng |
| Ending có thể bị khóa bằng phán đoán | Good ending có baseline tác giả ghi đề xuất; HELIOS Directive lấy trước MERLIN decision, Integrity >50 đầu Last Sync |
| Campus được coi như nhiệm vụ riêng hoàn chỉnh | CTU_01 là phân đoạn đề xuất của Dòng nước bị khóa; phải có cứu dân/máy phát/tutorial, không cấp reward Core sớm |

## Tệp đã cập nhật và phạm vi

- Thêm STORY_CANON.md: canon, hệ thống, nhánh và các điểm còn cần làm rõ.
- Viết lại STORY_IMPLEMENTATION_ROADMAP.md và STORY_IMPLEMENTATION_BACKLOG.md theo phạm vi mới; giữ dấu vết các ticket prototype đã có.
- Viết lại CANTHO_FIRST_MISSION_DESIGN.md để tránh mâu thuẫn chip/MEKONG/cấu trúc chương.
- Gắn thông báo nguồn mới vào CAMPUS_RECON_SYSTEM.md, giữ hướng dẫn đúng với runtime Protocol13.
- Thêm file này và các Unity meta mới; lưu nguồn tại Design/Story/LAST_PROTOCOL.docx cùng SOURCE.json/README.md.
- Lưu tài liệu trước chỉnh tại `Builds/StoryBackups/20261016-121808-CanonUpdate`.

Không đổi C#, scene, prefab, ScriptableObject, package, ProjectSettings, protocol, build hoặc cloud configuration. Không commit/push. Không sửa báo cáo test cũ thành kết quả của canon mới.

## Còn thiếu và giới hạn

Class, chip transfer, checkpoint/reconnect, Core vote, các chương, backend và final chưa trở thành tính năng chỉ bằng cập nhật văn bản. Recon hiện tại vẫn là ba điểm tập hợp và restart toàn lượt. Thứ tự code kế tiếp là interaction/action lock, graph/data, rồi lớp chip/class/checkpoint/reconnect theo backlog.

Nguồn có ví dụ Cần Thơ WorldTier4 nhưng cũng đặt ceiling native+1; cần chốt raw/effective/reward tier khi làm scaling. Timer vote/reconnect và case carrier rời lúc hòa chưa khóa. Điều kiện ending đề xuất cần configuration, không hardcode thành luật đã duyệt.

Kiểm tra của lần cập nhật này chỉ gồm nguồn/checksum, liên kết tài liệu, metadata và hash tệp runtime được bảo vệ. Không chạy lại gameplay/network test vì không đổi runtime; các tính năng canon mới vẫn CHƯA KIỂM THỬ. Kết quả kiểm tra tệp: [CANON_UPDATE_VALIDATION.json](../../../Design/Story/CANON_UPDATE_VALIDATION.json).
