# Ngoại vi campus theo Google Maps — revision7 / Protocol17

> **Bản hiện tại:** [ngoại vi và bờ hồ revision8 / Protocol18](CAN_THO_STREETSCAPE.md). Thêm nhà trong hẻm, cây, ghế và đèn; giữ mạng đường/campus revision7. Số liệu bên dưới thuộc lịch sử.

Khảo sát Google Maps ngày16/10/2026; hoàn thiện và kiểm tra ngày17/10/2026. Map giữ nguyên campus theo sơ đồ người dùng, chỉnh lại phần ngoài trường để bám quan hệ đường, hồ/rạch và khu dân cư đã quan sát.

## Các thay đổi

- Đường3/2 tiếp tục là mặt tiền campus, giữ ba cổngA/B/C và tuyến Recon.
- Thay ba hẻm mẫu thẳng bằng mạng đường: Búng Xáng/Hẻm51 ở cạnh bên trường, Mạc Thiên Tích và Trần Văn Hoài trong khu đối diện; Mậu Thân nối vòng phía đông. Đầu bắc có một tuyến dẫn hướng Nguyễn Văn Cừ được giản lược.
- Bổ sung vùng nước phía tây/tây bắc, liên hệ cảnh quan Búng Xáng/Rạch Rau Muống; có đường dạo, lan can, khu ăn uống và lối nối với đường ven hồ.
- Tái bố trí167 căn nhà hiện có theo các hướng phố và rút chiều sâu nhà cho phù hợp thửa nhỏ. Nhà có collider xoay cùng model, ban công, mái, cửa hông và mặt tiền hướng ra đường.
- Giữ chi tiết tương lai ở mức đạo cụ: đèn năng lượng, tủ cứu trợ. Chưa thêm nhiệm vụ, NPC, xe cộ hoặc tương tác mới.
- Nền khám phá mở rộng940×840 đơn vị game. Biên thế giới dời theo nền mới; hàng rào campus giữ nguyên.

## Mức độ giống thực tế

Đã xem trực tiếp Google Maps ở cả chế độ đường và ảnh vệ tinh: [campus](https://www.google.com/maps/@10.0299337,105.7706153,16z), [Công viên Bờ Hồ Búng Xáng](https://www.google.com/maps/search/?api=1&query=10.0316129,105.7641478). Nguồn và các giới hạn nằm trong `ArtSource/References/CanThoCampus/REFERENCES_PERIPHERY.md`.

Model bám quan hệ vị trí và hướng tuyến. Khoảng cách, chiều rộng đường, đường cong chi tiết, thửa nhà và cao độ được nén/ước lượng để vừa campus đang có. Hướng bắc địa lý không trùng trụcZ game. Chưa phải tái dựng1:1 hoặc dữ liệu GIS. Không đặt địa chỉ, thương hiệu thật lên từng căn; không nhúng ảnh hoặc tile Google vào game.

## Cách chơi thử

1. Giải nén cùng gói **Protocol17** trên tất cả máy. Host tạo phòng mới, chọn Cần Thơ, mọi người Ready rồi Deploy.
2. Từ cổngB ra đường3/2, đi về phía cổngC và tiếp tục tới nhánh có bảng **BÚNG XÁNG / HẺM51**.
3. Theo tuyến này dọc cạnh campus tới bờ nước và khu ăn uống. Có thể đi vòng lên đầu bắc, qua hướng Mậu Thân rồi về đường3/2 để quay vào cổngB.
4. Ở khu đối diện trường, thử các ngã Mạc Thiên Tích và Trần Văn Hoài. Nhân vật vẫn dùng điều khiển và hệ thống chiến đấu/hồi sinh hiện có.
5. Vùng nước chưa có bơi/lặn; lan can ngăn đi xuống nước và NavMesh loại trừ mặt nước. Nhà chỉ có ngoại thất.

## Nguồn và công cụ

- `Design/Maps/CanThoCampusII/PERIPHERAL_PLAN.json`: bản vẽ tay bằng tọa độ game; không phải dữ liệu xuất từ Google.
- `Tools/Blender/AlignCampusPeriphery.py`: nhận nguồn revision6 và plan, lưu candidate mới, không ghi đè.
- `Tools/Blender/PeripheralGeometry.py`: kiểm tra lô nhà/đường, xoay mặt tiền và giữ khoảng trống cho đường đi.
- `Tools/Blender/ValidatePeripheralPreservation.py`: đối chiếu mesh campus, collider, vật liệu và việc nhà không chồng đường/nước/nhau.
- Nguồn chỉnh sửa: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`; model Unity trong `Assets/_Project/Art/Environments/CanThoCampus`.
- Editor `LastProtocol > Maps > Apply Can Tho Detailed Art Revision`: nâng scene đã cài; từ chối nếu scene đang Play hoặc có thay đổi chưa lưu.
- Editor `LastProtocol > Maps > Build Windows Campus Periphery`: build ra `Builds/CanThoPeripheralPlayer` của project đang chạy lệnh.
- `CanThoPeripheralTests` và `CanThoNeighborhoodTests`: kiểm tra mạng đường, nước, lô nhà, collider, capsule và vị trí probe camera.

## Kiểm tra và giới hạn

Kết quả thực tế ghi ở `CAN_THO_PERIPHERAL_TEST_RESULTS.md`. Smoke localhost không thay cho Relay Internet. Cần kiểm tra lại RTT/loss và FPS trên các máy của nhóm; có thể dùng các công cụF8/F9 đang có. Chưa triển khai LOD/streaming mới, nội thất, giao thông hoặc enemy wave ngoài trường.

Backups: `Builds/CanThoBackups/20261016-PeripheralLayout`. File đã kiểm tra và bằng chứng: `Builds/CanThoMap/20261016-PeripheralLayout`. Không xóa bản build/source cũ trong đợt này.
