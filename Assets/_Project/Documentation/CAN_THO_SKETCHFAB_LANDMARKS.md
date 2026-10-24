# Công trình Cần Thơ từ Sketchfab

Nguồn: AnLe, tải bằng tùy chọn GLB chính thức của Sketchfab ngày 24/10/2026. Tệp nguồn được giữ nguyên trong `ArtSource/ThirdParty/CanThoLandmarks`; manifest SHA256 và giấy phép nằm cạnh nguồn.

## Ghi công bắt buộc khi phân phối

- **Hội trường Rùa - Đại học Cần Thơ** by **AnLe**, https://sketchfab.com/3d-models/hoi-truong-rua-ai-hoc-can-tho-0e94844e9aed4def895bcaa6be712c9a
- **Nhà học A3 - Đại học Cần Thơ** by **AnLe**, https://sketchfab.com/3d-models/nha-hoc-a3-ai-hoc-can-tho-d21b1ddc9baa43d1a8649ab4ce99286c
- **Nhà thi đấu TDTT - Đại học Cần Thơ** by **AnLe**, https://sketchfab.com/3d-models/nha-thi-au-tdtt-ai-hoc-can-tho-d13c5e092e3144bda16077b2246aa33d

All three models are licensed under **Creative Commons Attribution 4.0 International**, https://creativecommons.org/licenses/by/4.0/ . Author profile: https://sketchfab.com/AnLe . Adapted for LAST_PROTOCOL; the original creator does not endorse this adaptation.

Changes: extracted the requested buildings from surrounding source geometry, removed source scale figures and surplus ground, fitted the buildings to the existing game parcels, converted materials to URP, merged meshes by collection/material, and replaced gameplay collision proxies/navigation. Horizontal and vertical scale were adjusted separately for the Turtle Hall and A3 to suit the compressed game map. The sports hall retains uniform scaling. Original GLB files remain unmodified.

## Phạm vi bản thích nghi

- Hội trường Rùa thay nhóm `24_TurtleAuditorium`; giữ hướng lối vào về phía nam, không nhập các dãy nhà và nền đi kèm.
- Cụm A3 thay nhóm `05_Classrooms_A3`, giữ các dãy trong model tác giả và khoảng sân giữa chúng. Thu vào phần đất không vướng đường trục cổng A và đường vòng phía đông; giữ hai điểm tiếp cận A3 hiện có.
- Nhà thi đấu thay nhóm `25_Sports` (nhà thi đấu mới), có mái cong và bốn khối góc. Giữ khu nhà thi đấu cũ/GDTC `13_SportEducation`.
- Giữ nguyên đường, đất, cây đã sửa, nhà phố nhập trước đó, vị trí trạm nhiệm vụ, checkpoint, AI và giao thức multiplayer 28. Không mở rộng map.
- Nhà là cảnh quan bên ngoài. Collider đơn giản ngăn đi xuyên qua; chưa xây dựng nội thất có thể đi vào.

## Kiểm chứng

Báo cáo, ảnh Unity và kết quả kiểm tra được lưu tại `Builds/CanThoMap/20261024-SketchfabLandmarks`. Chỉ quảng bá bản ứng viên sau khi kiểm tra hình học, bảo toàn nguồn, lối đi và EditMode đạt. Bản build Windows của đợt này chứa bản sao thông tin ghi công trong `CANTHO_MODEL_CREDITS.txt`.
