# Kết quả kiểm tra gục / hồi sinh

Ngày 2026-10-15, Unity 6000.5.4f1, NGO 2.13.3, protocol 6.

## Bằng chứng

- Windows Development build `Builds/ValidationProject/Builds/RevivePlayer/LAST_PROTOCOL.exe`: PASS, Unity exit 0; log `Builds/ValidationResults/ReviveBuild.log`.
- Runtime DLL SHA256: `4EC044DFDEFF410AC90B030C80D02ECACF674286A2D13E634BD8859B767BC3EA`.
- EditMode trên source cuối: **69/69 PASS**, Unity exit 0. Kết quả chính thức đọc `Builds/ValidationResults/EditMode.xml`.
- Loopback gục/hồi sinh: 16/16 kiểm tra server PASS, mask `65535`, được nhận trên Host và cả 3 Client. Bằng chứng trong `Builds/ValidationResults/ReviveSmoke/{host,client1,client2,client3}.json`, trường `revive`.
- Smoke hồi sinh tổng thể: **6/6 summary PASS** (Host, 3 Client, người thứ năm bị từ chối, phòng đóng sau Start); Client nhận Host disconnect và về lỗi/menu. Enemy regression với chính bản RevivePlayer: **6/6 summary PASS**; tuần tra, đuổi, windup/melee, retarget, né, quay về, Client bắn gây damage, chết/respawn đều đạt. Xem `Builds/ValidationResults/EnemySmoke/Summary.json` và log của từng bộ.

## 16 kiểm tra hồi sinh thực qua NGO

Owner Client gửi RPC thật; Host chỉ dùng fixture để đặt vị trí, gây damage và tạo vật cản cho từng ca. Không giả lập kết quả thành công và không dùng Reflection để ghi NetworkVariable.

| Bit / ca | Kiểm tra | Kết quả bản cuối |
|---|---|---|
| 0 | Bắt đầu channel nhưng chưa đủ 3 giây: mục tiêu vẫn 0 HP | PASS |
| 1 | Thả E bằng RPC reliable: hủy, không hồi HP | PASS |
| 2 | Không có heartbeat mới: hủy sau timeout | PASS |
| 3 | Đi/đặt người cứu ra ngoài range: hủy | PASS |
| 4 | Người cứu nhận damage: hủy; giữ tiếp không tự khởi động lại | PASS |
| 5 | Collider tường che LOS: không channel/hồi HP | PASS |
| 6 | Target NetworkObjectId không tồn tại: từ chối | PASS |
| 7 | Hai Client cùng cứu một mục tiêu: chỉ một channel | PASS |
| 8 | Client gửi movement qua RPC: hủy cứu và có thể di chuyển | PASS |
| 9 | Client gửi fire RPC khi channel: Host từ chối phát bắn | PASS |
| 10 | Đủ thời gian giữ E: hồi đúng 40 HP, counter tăng đúng 1 | PASS |
| 11 | Gục lại và cứu lần hai: hồi 40 HP, không kẹt lease | PASS |
| 12 | Người cứu bị gục trong channel: hủy, mục tiêu không tự hồi | PASS |
| 13 | Mục tiêu được reset Health khi đang cứu: channel cũ hủy, không hoàn thành thêm | PASS |
| 14 | Cả đội gục, debug heal bị chặn; Host reset được, double reset bị từ chối | PASS |
| 15 | Sau reset: cùng 4 PlayerObject, đầy HP, TeamWiped=false, đúng counter vòng | PASS |

Mọi tiến trình còn quan sát channel, 40 HP, team wipe và restart. Ba Client gọi trực tiếp API hồi sinh/reset vòng đều bị từ chối. Player NetworkObjectId trước/sau reset không thay đổi.

## Lần chạy chuẩn bị

Lần smoke đầu có bit 8 thất bại: fixture cho người cứu đi thẳng vào Capsule mục tiêu; channel đã hủy nhưng tốc độ cuối bằng 0 do va chạm. Đã đổi hướng input đi ra xa và chạy lại toàn bộ bản build cuối. Log lần đầu được giữ trong `Builds/ValidationResults/History/20261015-111955-381-Smoke`. Không dùng lần này làm bằng chứng PASS.

## CHƯA KIỂM THỬ

- Relay / Internet trên 4 PC với bản protocol 6.
- Thao tác giữ E, UI/nhìn tiến độ, nhảy, chuột và Alt-Tab thực bằng người dùng. CLI gọi cùng RPC nhưng không thay thế bàn phím hoặc kiểm tra hình ảnh.
- Disconnect đúng giữa channel của người cứu/mục tiêu, rồi người khác tiếp quản; cleanup đã triển khai, chưa có ca tự động riêng cho thời điểm này.
- Enemy đánh ngắt hồi sinh khi người dùng chơi thực. Smoke dùng server damage API tương tự, Enemy được kiểm tra trong bộ độc lập.
- Build Release, lag/jitter/loss có chủ đích, host migration, prediction, checkpoint và lưu tiến độ.

Các log có cảnh báo nền Unity về certificate/analytics; smoke loopback không dùng Authentication/Relay. Không coi cảnh báo này là kết quả kiểm tra Unity Cloud.
