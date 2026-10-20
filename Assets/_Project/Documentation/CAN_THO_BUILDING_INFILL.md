# Nhà bổ sung trên nền đất hiện có — revision9 / Protocol19

Yêu cầu ngày 20/10/2026: lấp công trình vào đất trống, tiếp tục đối chiếu Google Maps, không mở rộng map. Mô hình giữ nguyên nền 940 × 840 m, đường, mặt nước, campus, 251 căn nhà và các tuyến chơi của revision8.

## Nội dung dựng

Thêm 1.100 nhà vào các ô đất còn trống, nâng tổng nhà ngoại vi lên 1.351. Các dãy có nhà 1–4 tầng, mái tôn đỏ/xanh/xám và mái bằng, mặt tiền cửa cuốn, cửa kính, khung cửa, ban công và điều hòa. Một số nhà có ban công/mái hiên hình học, bồn nước và pin năng lượng trên mái. Các công trình chỉ có ngoại thất.

Nhà được đặt trong các ô phía Búng Xáng, sau các dãy đường3/2, Mạc Thiên Tích, Mậu Thân và hướng Nguyễn Văn Cừ. Hướng/mật độ được đối chiếu bằng mắt với ảnh vệ tinh Maps; tọa độ từng thửa và cao độ là bố trí phục vụ gameplay, không phải bản sao1:1. Xem [nguồn tham khảo](../../../ArtSource/References/CanThoCampus/REFERENCES_BUILDING_INFILL.md).

Không thêm đường chính hay hẻm mới. Sân thể thao, khoảng xanh trong campus, hồ và đường dạo hiện có được giữ. Các khoảng trống giữa hàng nhà tiếp tục dùng mặt đất đang có để đi bộ. Nhà mới có collider thân riêng, được đưa vào lần bake NavMesh mới.

## Nguồn và cách dựng

- `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`: nguồn có thể chỉnh sửa, giữ các mesh cũ và thêm collection 67 theo vùng.
- `PlanExistingLandInfill.py`: chọn footprint trong ranh đất cũ, tránh campus/nước/đường/công trình/cây và các điểm kiểm thử hiện có.
- `FillExistingCampusLand.py`: mở revision8, vẽ atlas mặt tiền và dựng nhà, ghi vào thư mục ứng viên mới. Không chạy trên nguồn revision9; dùng bản revision8 trong backup để dựng lại.
- `ValidateExistingLandInfill.py`: so sánh mọi mesh/UV/transform cũ, collider và dữ liệu đường; kiểm tra biên đất, lô nhà và budget.
- `DrawExistingLandInfillPlan.py`: xuất sơ đồ SVG, nhà mới màu đất nung và nhà cũ màu xám.

Mặt tiền dùng một atlas 2048 có normal map, do script tự vẽ. Bốn nhóm vùng dùng cùng material để hạn chế renderer. Các mesh nhà có UV riêng, không lấy ảnh Maps làm texture. Giới hạn map vẫn dưới 500 renderer và 600.000 tam giác; con số này không thay thế phép đo FPS trên máy chơi thật.

Backup trước lượt này: `Builds/CanThoBackups/20261020-BuildingInfill`. Ứng viên và bằng chứng kiểm tra: `Builds/CanThoMap/20261020-BuildingInfill`. Kết quả thực tế ghi trong `CAN_THO_BUILDING_INFILL_TEST_RESULTS.md` sau khi chạy.

## Chơi và build

Mở `MainMenu`, chọn **CAN THO / CAMPUS II**. Mọi máy phải dùng cùng Protocol19, tạo phòng/Join Code mới sau khi cập nhật. Không trộn build18 trở về trước vì hình học/collider khác nhau.

Menu build: **LastProtocol → Maps → Build Windows Campus Building Infill**. Công cụ validation chạy trên `Builds/ValidationProject`, không đóng hay điều khiển Editor đang mở của người dùng.

CTU_00_RECON, combat, gục/hồi sinh và các scene khác giữ cách chơi hiện tại. Các nhà mới không có nhiệm vụ/loot/nội thất riêng; lượt này tập trung hoàn thiện môi trường trên đất đã có.
