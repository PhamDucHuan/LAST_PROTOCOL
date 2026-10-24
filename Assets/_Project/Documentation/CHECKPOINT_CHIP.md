# Checkpoint và tutorial chuyển chip — Protocol26

## Chính sách trong phiên

Đoạn campus vẫn là tập hợp → ba bản ghi → máy phát → uplink trong nhiệm vụ đầu *Dòng nước bị khóa*. Host lưu checkpoint sau tập hợp, sau **từng ID bản ghi** được xác nhận, máy phát và uplink. Thanh E đang giữ không được lưu. Thất bại/wipe: Host bấm **RETRY CHECKPOINT (HOST)**; mọi operator hồi đầy HP và về vị trí an toàn gần mốc. Máy phát đã bật thì trận ba robot bắt đầu lại một lần trong lượt thử mới, không cộng thêm robot khi hủy E. Không giữ kill của trận đang thử; chưa có kill reward để farm.

Bitmask ID là ledger dữ liệu: restore giữ nguyên ID và thiết bị đã xong (Completions = 1), không gọi lại sự kiện hoàn thành hay cộng điểm. Cả kết thúc uplink cũng được giữ nếu squad gục sau khi đã hoàn thành. Attempt tăng để chặn RPC tương tác trễ của lượt cũ. Chỉ Host được retry, không có RPC cho client reset/heal/đổi checkpoint.

Đây là checkpoint **trong phiên Host hiện tại**, chưa phải save đĩa, backend, host migration hay phục hồi sau crash. Chưa có inventory tiêu hao, loot, Meter hoặc reward campaign để snapshot; không tự thêm kinh tế/reward mới.

## Một chip, một carrier

Chỉ một NetworkChip trên Room. Capsule rơi là hình ảnh cục bộ của cùng snapshot, không spawn thêm item mạng. Trong prototype chưa có chọn class/operator: **slot 0 đại diện carrier Khoa ban đầu**, cấu hình `initialCarrierSlot`; authority vẫn luôn ở Host, mọi slot đều có thể nhận chip. Không coi danh tính Host là Khoa trong campaign.

Carrier gục → Emergency Eject. Đồng đội còn sống trong 2.5 m giữ **E 2 giây** để nhận chip; Host kiểm tra trạng thái, khoảng cách, che khuất, reservation và thời gian. Hai người nhặt cùng lúc chỉ một người thành carrier. Nhả E, di chuyển, nhảy, bắn, nhận damage, gục, mất focus hoặc mất heartbeat thì hủy; thanh tiến độ về 0. Hồi sinh carrier cũ không chuyển chip lại. Chỉ carrier hiện tại channel được uplink; người khác phòng thủ.

Sau ba bản ghi, trước máy phát: tập trung ở **ADMIN 01**. Carrier giữ E 1.5 giây bắt đầu relay drill khi có đồng đội sống trong 5 m. Drill làm carrier gục ở khu chưa có robot; đồng đội nhặt chip bằng E, **nhả E**, rồi E lần nữa để revive (3 giây). Thu hồi thành công đánh dấu tutorial đã học và mở máy phát; revive vẫn được khuyến khích. Một lần giữ không đồng thời nhặt chip và revive.

Nếu đã chuyển chip thật thành công khi carrier gục trước đó, tutorial đã được ghi nhận và không ép lặp drill. Trong bài học đầu, việc thu hồi phải do đồng đội thực hiện.

Tutorial và lần chuyển chip đã xác nhận giữ qua retry; khôi phục cho slot vừa nhận chip sau cùng, mọi người đã hồi HP. Nếu wipe trước khi nhặt thành công, checkpoint đưa chip về carrier đã xác nhận và cho bắt đầu drill lại. Carrier disconnect thì chip eject tại vị trí cuối; khi retry nếu slot đã rời thì chọn slot đang tồn tại thấp nhất. Chưa có reconnect window/AI takeover/stable profile; không tuyên bố đã triển khai toàn bộ reconnect trong truyện.

Trace Upload, Neural Load, Integrity/Safe Mode và tự thả chip chưa triển khai trong lát cắt này. Không hiển thị timer Trace giả hoặc phạt Integrity giả.

## Kiểm chứng

EditMode: ID idempotency qua nhiều retry, tuyến phụ thuộc ở từng commit, kết thúc không bị chơi lại để cộng dữ liệu, hold không được lưu, checkpoint sai bị từ chối, gesture chip/revive, cấu hình duy nhất, NavMesh/collision vị trí bốn slot.

Fixture `--lp-checkpoint-chip-test` dùng chính policy production và owner RPC thật. Các flag lịch sử `--lp-uplink-test`, `--lp-uplink-defense-test`, `--lp-uplink-preview` giữ Protocol24/25 để kiểm tra hồi quy cũ; không bật drill/checkpoint mới. Recon/training tiếp tục cơ chế reset lượt cũ.

Kết quả Protocol26: **157/157 EditMode**, đội **2 và 4 người mỗi đội 32 tình huống**; các Client nhận đủ 7 thông báo checkpoint và cùng snapshot kết thúc. Gồm retry sau một bản ghi, wipe giữa drill, nhặt đồng thời, range/LOS/release/heartbeat/damage, không dùng cùng gesture để revive, carrier cũ được revive, retry trận bảo vệ và retry sau kết thúc mà mỗi thiết bị vẫn chỉ hoàn tất một lần.

HUD mới đạt **83 kiểm tra**, log Host/Client sạch khi rời phiên. Hồi quy: 22 tình huống defence, 16 revive trên bốn participant cùng movement/health/combat, 107 kiểm tra UI cũ. Bằng chứng chọn dùng và giới hạn từng lượt nằm trong `Builds/Checkpoints/20261022-CheckpointChip/CompletionReport.json`; lượt cuối dùng `NetworkSmoke2-Final`, `NetworkSmoke4-Final`, `GraphicalPreview-FinalClean`.

Kiểm tra localhost không thay thế kiểm tra Relay trên nhiều máy thật. Scene campus, art/layout, NavMesh, Player prefab, enemy và mission assets được giữ nguyên; Room chỉ thêm NetworkChip và menu đổi nhãn build26.
