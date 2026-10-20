# Model Việt Nam trong map — revision 10 / Protocol 20

Tích hợp ngày 20/10/2026 vào nền đất hiện có của map ĐHCT. Giữ ranh 940 × 840 m, công trình campus, đường, hẻm, hồ, spawn và các tuyến nhiệm vụ.

## Vị trí và nguồn

- Hai căn nhà Hội An thay các lô `District house 181/182`, gần khu Búng Xáng. Nguồn: [buiductam1202 — Hoi An Ancient House Model](https://www.cgtrader.com/free-3d-models/architectural/architectural-street/hoi-an-ancient-house-model), trang ghi Royalty Free License (no AI).
- Hai căn Asian House thay các lô `District house 044/086`. Nguồn: [HUY TRAN — Asian House](https://www.blendkit.com/asset-gallery-detail/d0c4d779-5fbb-433d-80e0-c1a4a0e619b8/), [Blendkit Royalty Free](https://www.blendkit.com/docs/licenses/).
- Ba cụm bàn, ghế nhựa, tô phở và gia vị tại khu ăn uống ven hồ. Nguồn: [AlleyBits — Phở Dining Set free sampler](https://alleybits.itch.io/asian-street-food-pack#demo). Giấy phép gốc trong `ArtSource/ThirdParty/VietnamFreeAssets/PhoDiningSet/Extracted/AsianStreetFoodPack_PhoDiningSet/LICENSE.txt`; cho phép sản phẩm cá nhân/thương mại, không phân phối lại asset thô.
- Ba cây từ [PolyOne Studio — Polygon World – Vietnam](https://assetstore.unity.com/packages/3d/environments/polygon-world-vietnam-328183) làm điểm nhấn cảnh quan. Áp dụng [Standard Unity Asset Store EULA](https://unity.com/legal/as-terms).

Nhà được điều chỉnh kích thước để nằm trong lô cũ, dùng ngoại thất làm điểm nhấn; không đại diện cho kiến trúc thực đo của đúng địa chỉ tại Cần Thơ. Không thêm nội thất, loot hay nhiệm vụ vào các nhà này.

## Nguồn chỉnh sửa và quy trình

- Nguồn tải gốc và SHA256: `ArtSource/ThirdParty/VietnamFreeAssets/`.
- Mô hình tích hợp chỉnh sửa được: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`.
- Bố trí và vật liệu: `CanThoCampus.layout.json`; báo cáo từng lô: `VietnamIntegration.json` cạnh nguồn Blender.
- Script: `Tools/Blender/IntegrateVietnamAssets.py`, đầu vào là bản revision 9 trong backup, không chạy lại trên nguồn revision 10.
- Ứng viên/bằng chứng: `Builds/CanThoMap/20261020-VietnamAssets/`; backup revision 9: `Builds/CanThoBackups/20261020-VietnamAssets/`.

Các bản sử dụng trong game được giảm polygon, texture tối đa 1K và chuyển sang URP/Lit. Giữ vỏ nhà Asian House; tập trung giảm hình học ở cổng kim loại quá dày. Nhà Hội An giữ nguyên tường, cửa, cột và lớp mái liền; bỏ lớp ngói nổi có hơn 76.000 tam giác. Các lớp lá chồng nhau và thân cây cũ cũng được giảm chi tiết để giữ tổng ngân sách dưới 600.000 tam giác và 500 renderer. Đây là giới hạn hình học, không phải chứng nhận FPS trên máy nhóm.

Collider của bốn căn nhà được cập nhật theo kích thước mới; bàn ăn giữ vùng cản hiện có và cây mới có collider thân. NavMesh phải bake lại và chạy kiểm tra đường đi. Phiên bản mạng tăng lên Protocol 20 vì collider đã thay đổi; các máy chơi cùng phòng phải dùng cùng phiên bản.

Menu build: **LastProtocol → Maps → Build Windows Campus Vietnam Assets**. Import, test và capture chạy trong bản sao `Builds/ValidationProject` trước khi tích hợp vào project chính.

## Kết quả kiểm tra ngày 20/10/2026

- Candidate05: 593.498 tam giác, 472 renderer, 2.348 collider hộp; ranh đất và các công trình ngoài bốn lô thay thế được giữ nguyên.
- Kiểm tra bố trí, nguồn texture PNG tối đa 1K và ngân sách hình học: đạt (`Candidate05/IntegrationValidation.json`).
- Unity EditMode: 128/128 đạt, exit code 0 (`EditModeFinal.xml`). Có kiểm tra vật liệu của bốn nguồn và NavMesh tới mặt tiền, chặn bên trong bốn căn nhà.
- Unity capture: 28 ảnh, exit code 0 (`CapturesFinal/`); đã xem gần nhà Hội An, Asian House và bàn phở, sửa mái méo và ghế cũ chồng hình.
- Đã chép kết quả vào project chính, giữ GUID cũ; danh sách SHA256 trong `PromotedAssets.json`. Không chạy thêm bản build Windows hoặc đo FPS trong lượt này.
