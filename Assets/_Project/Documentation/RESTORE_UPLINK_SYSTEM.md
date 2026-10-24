# Khôi phục liên lạc — phân đoạn mở đầu campus

**Hiện hành Protocol26:** xem [checkpoint và tutorial chip](CHECKPOINT_CHIP.md). Sau ba bản ghi có relay drill ở ADMIN 01; carrier hiện tại mới giữ E tại uplink. Retry giữ ID dữ liệu và tiến độ xác nhận trong phiên; hướng dẫn Protocol24 bên dưới được giữ làm lịch sử.

Protocol25 bổ sung [encounter bảo vệ uplink](UPLINK_DEFENSE.md): một đợt ba robot xuất hiện sau máy phát; hủy E không tạo thêm địch. Kết quả Protocol24 bên dưới giữ lại làm bằng chứng lịch sử của chuỗi tương tác trước encounter.

Protocol24, 21/10/2026. `CTU_01_RESTORE_UPLINK` là nhiệm vụ mặc định khi triển khai map Cần Thơ: tập hợp → thu dữ liệu → máy phát → liên lạc. Đây là đoạn mở đầu trong **Dòng nước bị khóa**; không hoàn tất nhiệm vụ cha/chương, cấp Core Key, nâng tier hay thưởng campaign.

## Chơi thử

Mở MainMenu, chọn **CAN THO / CAMPUS II**, tạo/tham gia phòng, tất cả Ready rồi Host Deploy. Mọi người dùng cùng Protocol24, đội 2–4 người.

1. Cả đội còn sống tập hợp trong vòng cyan Cổng B, X0 Z−40, liên tục 2 giây.
2. Thu đủ ba bản ghi khác nhau ngoài nhà điều hành, thứ tự tùy chọn. Mỗi terminal giữ E 3 giây. HUD hiện số đã thu chung của đội; vòng chỉ đường chuyển tới bản ghi chưa thu tiếp theo.
3. Giữ E 4 giây tại máy phát phụ để cấp điện.
4. Giữ E 5 giây tại uplink bên lối vào Học liệu. HUD **COMMS RESTORED** xác nhận kênh cứu hộ cục bộ đã phục hồi.

| Thiết bị / stable ID | X | Z | Kết quả |
|---|---:|---:|---|
| ADMIN 01 / CTU_ADMIN_DATA_01 | 30 | −24 | Bản ghi: kênh cứu hộ mất điện |
| ADMIN 02 / CTU_ADMIN_DATA_02 | 24 | −24 | Bản ghi: vị trí máy phát |
| ADMIN 03 / CTU_ADMIN_DATA_03 | 36 | −24 | Bản ghi: uplink cần nguồn phụ |
| AUX POWER / CTU_AUX_GENERATOR | 42 | −32 | Cấp điện phụ |
| UPLINK / CTU_LIBRARY_UPLINK | 61 | −42 | Khôi phục kênh cứu hộ cục bộ |

Khoảng cách 2,5 m. Đèn xám: khóa; cyan: sẵn sàng; vàng: đang dùng; xanh lá: hoàn tất. Mọi Client dựng đèn/nhãn/HUD từ state đồng bộ. Uplink nằm bên cạnh mốc Recon X57 Z−42 để giữ lối vào cũ thông thoáng.

Hồi sinh được ưu tiên khi có đồng đội gục gần đó. Một lần giữ E chỉ chọn một hành động/đối tượng. Thả E, di chuyển/nhảy/tấn công, nhận sát thương, gục, ra xa, mất LOS, mất focus hoặc mất heartbeat đều hủy. Phải thả rồi giữ lại; tiến độ đang giữ không được giữ sau khi hủy.

## Quyền Host và dữ liệu

`PlayerInteraction` gửi owner RPC, có sequence và giới hạn tốc độ. Host kiểm tra ownership, session/level, còn sống, khoảng cách, LOS, objective hiện tại, thiết bị chưa hoàn tất và quyền đặt chỗ. Client không gửi thời gian hoàn thành hay số bản ghi.

Host đặt chỗ một actor trên thiết bị, tính thời gian và xác nhận kết quả trên luồng server. `MissionSnapshot.CompletedStations` là tập ID đã thu, bit theo thứ tự định nghĩa. Request trùng, sai bước hoặc terminal đã hoàn tất không tạo credit mới. Máy phát phụ thuộc cả ba bản ghi; uplink phụ thuộc máy phát. Không có RPC cho Client tự chuyển mục tiêu.

`MissionDefinition` schema1 thêm `Stations` bên cạnh `Rendezvous`. Chuỗi tuyến tính dùng mục tiêu trước làm dependency; Locked/Available/Active/Completed/Failed được suy ra từ snapshot/channel. Schema0 vẫn đọc asset Recon cũ, không thay GUID hay sửa asset Recon. Nhánh và Skipped chưa dùng; checkpoint/save campaign chưa triển khai.

`CTU_00_RECON` giữ lại qua cờ Development `--lp-mission-test/preview`; `--lp-interaction-test/preview` kiểm tra terminal độc lập trên Recon. Build thường dùng uplink. Wipe khi còn Active dẫn Failed; Host restart đặt lại mission, tập ID và năm thiết bị. Hoàn thành chỉ có hiệu lực trong lượt, chưa lưu xuyên phiên. Cứu dân, encounter riêng, carrier/chuyển chip, bảo vệ uplink và rút lui trong thiết kế sáu mục tiêu là phần sau.

## Bằng chứng

`Builds/Missions/20261021-RestoreUplink` lưu log cài đặt/build, XML EditMode, kết quả từng tiến trình và ảnh UI. `Tools/Run-UplinkSmoke.ps1` chạy owner RPC thật, so sánh toàn bộ mission/device snapshot lúc hoàn tất và sau restart giữa các participant. Kiểm tra localhost không thay thế nghiệm thu Relay trên nhiều máy thật.

Đã qua 140/140 EditMode và 21 tình huống mạng cho từng đội 2, 3, 4 người (9 tiến trình participant). Bao gồm khóa trước tập hợp, sai phase, xa, gục, sai target, LOS, giữ đồng thời, request lặp, ba ID riêng, hủy máy phát, máy phát trước uplink, kết quả chung, wipe/fail và hai lần restart. Mọi participant thấy đủ bốn bước và năm channel; snapshot lúc hoàn tất/reset khớp Host. Scene giữ 6.317 document cũ nguyên byte; chỉ hai document có thay đổi được cho phép (component trạm đầu và danh sách root), thêm 148 document cho bốn thiết bị. Menu chỉ đổi nhãn build, Player prefab và Recon asset nguyên byte.

Build cuối đã qua 99 kiểm tra đồ họa/UI, 13 tình huống tương tác owner RPC dùng chung revive, 16 tình huống hồi sinh trên đội bốn người (kèm movement/combat và từ chối phòng đầy/đã bắt đầu), cùng Recon hai người. Sau loạt 21 tình huống mạng chỉ rút gọn chữ briefing bị tràn trong menu, không đổi logic nhiệm vụ/tương tác. Chưa chạy Relay trên máy thật cho Protocol24.
