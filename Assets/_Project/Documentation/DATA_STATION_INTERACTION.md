# Trạm dữ liệu nhà điều hành

Protocol24 đã gắn trạm vào [chuỗi Restore Uplink](RESTORE_UPLINK_SYSTEM.md): cần tập hợp trước, thu đủ ba bản ghi rồi mở máy phát/uplink. Các kết quả Protocol23 bên dưới là bằng chứng lịch sử của terminal độc lập.

Trạm `CTU_ADMIN_DATA_01` ở X30 Z−24 trong `CanThoCampus`, ngoài mặt trước nhà điều hành. Tới gần 2,5 m, đứng yên và giữ E đủ 3 giây. HUD dùng chung với hồi sinh hiển thị hướng dẫn, thanh tiến độ và `DATA SECURED` khi Host xác nhận hoàn tất.

`PlayerInteraction` là bộ đọc phím E và cổng RPC chung. Ưu tiên đồng đội bị gục trong tầm, sau đó mới tới terminal. Mỗi lần giữ gắn với một loại hành động và một mục tiêu; hoàn tất, bị ngắt hoặc yêu cầu đổi mục tiêu thì phải thả E mới bắt đầu lại. `PlayerRevive` giữ logic hồi sinh và state cũ, không tự đọc E hoặc nhận RPC giữ phím riêng nữa. Combat và locomotion dùng cùng trạng thái đang tương tác.

Thả E, di chuyển/nhảy, tấn công, nhận sát thương, bị gục, mất focus/mở chuột, ra khỏi tầm, bị che tầm nhìn, hết heartbeat hoặc rời trạng thái chơi sẽ hủy lượt truy cập. Tiến độ hủy về 0. Mất người đang thao tác giải phóng trạm. Host kiểm tra ownership, ID, level, HP, khoảng cách, LOS, tốc độ và thứ tự request. Một trạm chỉ dành cho một người tại một thời điểm; hoàn tất chỉ ghi một lần cho cả phiên. Client đọc state để vẽ UI, không tự cộng tiến độ hoặc ghi kết quả.

Trạm và quyền tương tác là NetworkBehaviour mới, vì vậy Protocol tăng lên **23**; dùng cùng build cho các máy cùng phòng. Team wipe/restart thực hiện reset trạm qua `NetworkRoomState`. Cấu hình mặc định co-op 2–4 người giữ nguyên. Fixture solo chỉ dùng khi bật graphical preview.

Đây là một tương tác độc lập phục vụ bước INT-01/02 đầu tiên. Chưa ghép trạm vào objective graph, chưa thêm máy phát/chip/class/reward/save và không thay ba Rendezvous của Recon. Kết quả terminal chỉ tồn tại trong lượt chơi hiện tại.

`DataStationSetupTool.BatchInstall` dùng trong bản sao validation để thêm đúng một coordinator vào prefab Player, dựng một console URP native, thêm scene NetworkObject và bake NavMesh có collider terminal. Không chạy bộ dựng lại campus. Tool giữ model, cây, tuyến đường, camera và các đối tượng scene hiện có.

Kiểm thử và bằng chứng của lượt triển khai được lưu tại `Builds/Interaction/20261021-AdminDataStation/`. `InteractionTests` kiểm tra khóa một hành động/mục tiêu, yêu cầu thả phím và đường tiếp cận. `Run-InteractionSmoke.ps1` kiểm tra RPC Host–Client thật trên localhost; `Run-MenuPreview.ps1 -Level 2 -Interaction` chạy bộ đọc E chung và chụp HUD qua input fixture Development. Những fixture này không thay thế kiểm thử bàn phím và Relay trên nhiều máy thật.

## Kết quả bản cuối

- 136/136 EditMode đạt; install, Windows Development build và ảnh trạm đều exit0.
- Host–Client localhost: 13 tình huống tương tác đạt, gồm release, gục, sai tầm/ID, hai loại hành động cùng gesture, hồi sinh, tranh trạm, completion một lần, wipe/reset, timeout và LOS.
- Hồi quy bốn người: 16 tình huống hồi sinh cùng movement/combat và từ chối phòng đầy/đang chơi đạt.
- 58 kiểm tra graphical preview đạt; đã xem ảnh console và ảnh HUD hướng dẫn/tiến độ.
- Giữ nguyên 6.289 document scene cũ; chỉ thêm một root terminal và NavMesh liên quan. Chưa kiểm thử Relay trên nhiều máy thật.
