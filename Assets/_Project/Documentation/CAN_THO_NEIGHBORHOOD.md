# Khu phố ngoài campus — revision 6 / Protocol 16

> **Bản hiện tại:** [ngoại vi và bờ hồ revision8 / Protocol18](CAN_THO_STREETSCAPE.md). Thêm nhà trong hẻm, cây, ghế và đèn; giữ mạng đường/campus revision7. Số liệu bên dưới thuộc lịch sử.

> Bản chơi hiện hành: [ngoại vi theo Google Maps revision7 / Protocol17](CAN_THO_PERIPHERAL_LAYOUT.md). Các hẻm mẫu trong tài liệu này đã được thay bằng mạng đường theo tham chiếu; giữ tài liệu làm lịch sử bản16.

Ngày 16/10/2026. Mở rộng map Cần Thơ hiện có, chọn trong menu Operation Cần Thơ; không tạo thêm một màn độc lập.

## Nội dung

- Thay dãy nhà mẫu phía đối diện đường 3/2 bằng 167 nhà ống, cửa hàng và khối nhà trọ 2–4 tầng.
- Ba trục hẻm thông với hướng cổng C, B, A và ba đường ngang tạo đường vòng. Các hẻm này là đường thiết kế cho gameplay, không gắn tên đường thật khi chưa khảo sát vị trí.
- Khu ăn uống ngoài trời phía tây, quầy hàng, bàn ghế, mái che, cửa kính, ban công, lan can, bồn nước, điều hòa, vỉ thoát nước và biển dịch vụ chung.
- Chi tiết tương lai: tấm thu năng lượng, đèn đường thông minh, tủ cứu trợ và dải trạng thái. Đây là đạo cụ tĩnh; chưa có tương tác/hệ thống điện mới.
- Ground liên tục tăng từ 560×430m lên 560×640m trong hệ tọa độ gameplay. Biên nam thế giới được dời từ Z=-104 tới -314. Hàng rào campus và ba cổng giữ nguyên.

Khu phố lấy cảm hứng loại hình nhà và dịch vụ sinh viên quanh đường 3/2/Hẻm 51 qua ảnh tham khảo. Khoảng cách, số căn, chiều cao và bố trí được dựng cho game; không phải bản sao địa lý 1:1. Tham khảo chi tiết: `ArtSource/References/CanThoCampus/REFERENCES_NEIGHBORHOOD.md`. Không nhúng ảnh báo chí hoặc bản đồ vào asset.

## Cách chơi thử

1. Cả nhóm dùng cùng gói **Protocol16**. Host tạo phòng mới, chọn Cần Thơ, mọi người Ready rồi Deploy.
2. Từ điểm xuất phát đi ra cổng B, băng đường 3/2 và đi thẳng vào hẻm phía đối diện. Cẩn thận cửa đường vì chưa có hệ thống giao thông/NPC.
3. Đi các ngã rẽ ở Z≈-127, -190, -250. Có thể vòng sang hướng cổng C hoặc A để quay vào campus.
4. Khu ăn uống nằm ở mép tây dãy phố, X≈-280, Z≈-157. Nhà là ngoại thất kín; không có cửa vào/nội thất giả báo đã hoàn thành.
5. Kiểm tra chạy, nhảy, tấn công, hồi sinh và camera sát nhà. Enemy dùng NavMesh mở rộng; chưa tự thêm đợt enemy/quest mới ở ngoài trường.

## Nguồn và công cụ

- `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`: nguồn Blender, collection mới `50_NeighborhoodRoads`, `51/52/53_Neighborhood_*`, `55_NeighborhoodServices`.
- `Tools/Blender/ExpandCampusNeighborhood.py`: revision5 → 6, chỉ tạo output mới. Dùng `--python-exit-code 1`; Blender mặc định có thể thoát0 dù Python phát sinh exception.
- `Tools/Blender/RepairNeighborhoodFoodCourt.py`: sửa khoảng hở khu ăn uống trong candidate đã có; không ghi đè nguồn.
- `CanThoCampus.layout.json`: thêm `neighborhoodHouses`, `neighborhoodChecks`, `neighborhoodRoute`; giữ nguyên các mảng campus cũ.
- Unity `LastProtocol > Maps > Apply Can Tho Detailed Art Revision`: cài revision mới, không đổi scene hiện hành đang chỉnh. Nếu đã cài cùng revision mà thay nội dung layout, phải dùng BatchRefresh trong bản kiểm thử.
- `LastProtocol > Maps > Build Windows Campus Neighborhood`: tạo build riêng `Builds/CanThoNeighborhoodPlayer` ở project đang chạy lệnh.
- `CanThoNeighborhoodTests`: kiểm tra NavMesh, nội thất bị chặn, collider khớp nhà, ground/biên và capsule đi đường vòng.
- `CampusRoutePreview`: chỉ chạy khi có cờ kiểm thử trong Editor/Development build; bổ sung agent đi đường vòng mới và camera sát mặt tiền nhà.

## Giới hạn

Không có nội thất, dân cư/xe AI, hồ Búng Xáng, streaming hay LOD mới. Chia mesh mới theo vùng/vật liệu để culling; chưa coi số tam giác là bằng chứng FPS trên mọi máy. Bài smoke localhost không thay được Relay Internet trên ba/bốn máy. Kết quả và những việc chưa kiểm thử nằm trong `CAN_THO_NEIGHBORHOOD_TEST_RESULTS.md`.

Không dùng lại gói Protocol15 để join Host16. Thay toàn bộ thư mục game của các máy bằng cùng gói mới; không chỉ chép riêng EXE.
