# Thay nhà bằng model tải về — revision 11 / Protocol 21

Ngày 20/10/2026, dùng hai bộ miễn phí đã tải để thay 247 nhà phố cũ và 80 nhà trong khu lấp đầy gần các đường chính: tổng 327 căn, đủ 21 biến thể. Các nhà Hội An, Asian House, bàn phở và cây từ revision 10 được giữ lại. Nhà lấp đầy ở xa vẫn dùng atlas nhẹ; tổng số lô nhà vẫn là 1.351.

Nguồn và tác giả:

- [Low Poly Modern House Pack — 20 Houses, ilkhom23](https://www.cgtrader.com/free-3d-models/exterior/house/lowpoly-modern-house-pack-20-houses-low-poly).
- [Residential Building — Town House, narkr](https://www.cgtrader.com/free-3d-models/exterior/house/residental-building-town-house).

Cả hai trang nguồn ghi Royalty Free License (no AI). Bản tải gốc, hash và thông tin nguồn nằm trong `ArtSource/ThirdParty/VietnamFreeAssets/ModernHouses20/` và `TownHouse/`. Đây là model nhà phổ thông được thích nghi cho map; không đại diện cho nhà thực đo tại địa chỉ ở Cần Thơ.

Mỗi nhà mới được xoay theo hướng mặt tiền và đặt trong lô cũ. Bỏ sân, cây chậu, đèn trang trí đi kèm bộ tải để không chồng cảnh quan có sẵn. Vỏ kiến trúc, cửa và mái được giữ với chi tiết giảm phù hợp game. Hai vật liệu URP dùng texture PNG tối đa 1K, palette dùng lọc Point; Town House có normal map.

Giữ nguyên nền 940 × 840 m, khuôn viên trường, đường, hồ và tuyến nhiệm vụ. Collider được cập nhật theo nhà mới; xóa 334 collider cột mái hiên cũ để tránh vật cản vô hình, rồi bake lại NavMesh. Vì collider thay đổi, Protocol tăng từ 20 lên 21; các máy chơi cùng phòng cần dùng cùng bản.

Nguồn chỉnh sửa: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`. Danh sách từng căn, mẫu và lô gốc: `ModernHouseIntegration.json` cùng thư mục. Bộ mẫu đã chuẩn bị: `ArtSource/Blender/ModernHouses/ModernHouses.Prepared.blend`.

Quy trình tái tạo: `Tools/Blender/PrepareModernHouses.py`, `ReplaceCanThoHouses.py`, `ValidateModernHouseReplacement.py`. Script thay nhà yêu cầu baseline revision 10; không chạy lại trên revision 11. Backup tại `Builds/CanThoBackups/20261020-ModernHouses/`. Ứng viên, kết quả kiểm tra và ảnh Unity tại `Builds/CanThoMap/20261020-ModernHouses/`.

Ngân sách hình học: 592.755 tam giác, 336 renderer. Đây không phải phép đo FPS. Đã sửa hướng mặt cho 195 object có phép lật trong FBX nguồn; giữ lại cửa và cửa sổ hẹp khi loại các chi tiết sân vườn. Kiểm tra hình học xác nhận mọi vertex của nhà thay thế nằm trong lô gốc và nhà chạm đất. Import, capture và EditMode chạy trong `Builds/ValidationProject`, rồi mới chép sang project chính sau khi đạt.

## Kết quả cuối cùng

- Candidate04: kiểm tra bố trí và hình học đạt, 327 căn nằm trong lô cũ, các nhóm dữ liệu không liên quan giữ nguyên.
- Unity refresh, capture và EditMode đều exit code 0; 130/130 bài kiểm tra đạt, gồm đường đi hoàn chỉnh tới mặt tiền tất cả 327 căn và kiểm tra collider của toàn bộ 1.351 lô nhà.
- Có 49 ảnh Unity, gồm ảnh riêng đủ 21 mẫu. Đã xem bảng tổng hợp cùng ảnh cận cảnh, sửa mặt tường đảo hướng và giữ cửa/cửa sổ hẹp. `ModernHouses-ContactSheet.png` là bảng kiểm tra hình ảnh.
- Đã cập nhật project chính và giữ GUID của các asset cũ; `PromotedAssets.json` ghi SHA256 các tệp cập nhật. Không build Windows hoặc đo FPS trong lượt này.
