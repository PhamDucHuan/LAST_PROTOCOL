# Asset Việt Nam miễn phí — tải ngày 20/10/2026

Đã lưu đủ 4 lựa chọn được yêu cầu. Bản thích nghi từ cả bốn nguồn đã được tích hợp vào map revision 10: hai nhà Hội An, hai Asian House, ba cụm bàn phở và ba cây. Nguồn gốc giữ trong ArtSource; chi tiết chỉnh sửa và kiểm tra tại `Assets/_Project/Documentation/CAN_THO_VIETNAM_ASSETS.md`.

## Đã tải

### Hoi An Ancient House Model — buiductam1202

- Nguồn: https://www.cgtrader.com/free-3d-models/architectural/architectural-street/hoi-an-ancient-house-model
- Tải sau khi người dùng đăng nhập CGTrader, từ trang tải chính thức của mẫu miễn phí.
- Thư mục `HoiAnHouse/`: `HoiAnHouse_M2.fbx`, `HoiAnHouse_M2.blend`, `Texture_M2.rar`.
- Texture đã giải nén tại `HoiAnHouse/Extracted/Texture_M2/`, gồm 30 ảnh JPG.
- Trang nguồn ghi Royalty Free License (no AI). Nguồn điều khoản: https://www.cgtrader.com/pages/terms-and-conditions
- Bản dùng trong map đã chuyển URP, giữ tường và mái nền gốc, bỏ lớp ngói nổi dày và cập nhật collider.

### Polygon World – Vietnam — PolyOne Studio

- Nguồn: https://assetstore.unity.com/packages/3d/environments/polygon-world-vietnam-328183
- Đã thêm bản miễn phí vào thư viện tài khoản Unity do người dùng đăng nhập.
- Tải bằng Package Manager > My Assets trong Unity hiện có; sao chép gói vào `PolygonWorldVietnam/Polygon World - Vietnam.unitypackage` (7.013.374 byte).
- Kiểm tra cấu trúc tar/gzip đọc được, 244 mục trong archive. Trích dùng cây `SM_Tree_v01` cùng texture cho map.
- Giấy phép theo trang nguồn: Standard Unity Asset Store EULA, https://unity.com/legal/as-terms
- Ảnh `PolygonWorld_Downloaded.png` cho thấy Unity đã chuyển sang nút Import.

### Phở Dining Set — AlleyBits

- Nguồn: https://alleybits.itch.io/asian-street-food-pack#demo
- Chỉ tải demo miễn phí `AsianStreetFoodPack_PhoDiningSet_Free.zip`; không mua bộ đầy đủ.
- Thư mục `PhoDiningSet/`, bản giải nén trong `PhoDiningSet/Extracted/AsianStreetFoodPack_PhoDiningSet/`.
- Có 42 FBX, GLB/OBJ và texture; README tác giả ghi 9 kit, 18.010 tam giác.
- Giấy phép gốc nằm trong thư mục giải nén: `LICENSE.txt`. Cho phép dự án cá nhân/thương mại; không phân phối lại tệp asset thô hoặc dùng làm dữ liệu huấn luyện.
- Giữ các tệp nguồn trong phạm vi dự án; không đẩy lên kho asset công khai.

## Đã tải: Asian House — HUY TRAN

- Nguồn: https://www.blendkit.com/asset-gallery-detail/d0c4d779-5fbb-433d-80e0-c1a4a0e619b8/
- Người dùng đã hoàn tất điều khoản và kích hoạt tài khoản Blendkit.
- Tải bằng API chính thức, theo cơ chế trong mã nguồn client https://github.com/BlenderKit/bk_client/blob/main/client/download.go ; API xác nhận đúng asset, `isFree: true`, `canDownload: true` và giấy phép `royalty_free`. Không cần cài add-on hoặc lấy thông tin đăng nhập của người dùng.
- Thư mục `AsianHouse/` có `AsianHouse_HUY_TRAN_blend.blend` (90.279.403 byte) và `AsianHouse_HUY_TRAN_gltf.glb` (9.047.184 byte).
- Kích thước hai tệp khớp thông số API; GLB đọc được cấu trúc glTF 2.0 và không cần tài nguyên ngoài.
- `AsianHouse/SOURCE_AND_LICENSE.json` ghi nguồn, tác giả, mã phiên bản, giấy phép và SHA256. Điều khoản Royalty Free: https://www.blendkit.com/docs/licenses/
- Đây là nhà châu Á truyền thống theo mô tả tác giả. Bản dùng trong map đã chuyển URP, giảm chi tiết cổng và cập nhật collider; không phải bản sao kiến trúc của một địa chỉ cụ thể tại Cần Thơ.

`DOWNLOAD_MANIFEST.json` ghi kích thước và SHA256 của các tệp tải gốc. Đây là kiểm tra tệp tải, không phải kiểm chứng tích hợp hoặc hiệu năng trong Unity.

## Bổ sung ngày 20/10/2026 — 20 Houses và Town House

Hai bộ sau đã tải qua nút Free Download chính thức bằng tài khoản CGTrader đang đăng nhập, tổng 7 tệp nguồn, 35.711.904 byte. Chưa tích hợp hai bộ này vào map.

### Low Poly Modern House Pack — 20 Houses — ilkhom23

- Nguồn: https://www.cgtrader.com/free-3d-models/exterior/house/lowpoly-modern-house-pack-20-houses-low-poly ; item 3315233.
- Thư mục `ModernHouses20/`: FBX, Blender, OBJ, MTL, `textures.rar`, `previews.rar` gốc.
- Đã giải nén tại `ModernHouses20/Extracted/`, gồm model, 5 phiên bản JPG của texture màu ở các độ phân giải và 17 ảnh xem trước.
- Tệp MTL tải lên có tên khác tên mà OBJ tham chiếu. Bản đã chuẩn bị có tên `Extracted/LowPoly_Modern_House_obj.mtl` đúng với OBJ; giữ nguyên tệp tải gốc.
- Trang nguồn ghi Royalty Free License (no AI); chi tiết nguồn và hash tại `SOURCE_AND_LICENSE.json` trong thư mục bộ asset.

### Residential Building — Town House — narkr

- Nguồn: https://www.cgtrader.com/free-3d-models/exterior/house/residental-building-town-house ; item 713042.
- Thư mục `TownHouse/`: gói đầy đủ `Residental_building_full_file.zip` (18.101.550 byte), kiểm tra CRC đạt.
- Đã giải nén tại `TownHouse/Extracted/Residental_building/`: FBX, Blender, OBJ/MTL, DAE, 3DS, MAX và 3 texture PNG (màu, normal, specular).
- Trang nguồn ghi Royalty Free License (no AI); chi tiết nguồn và hash tại `SOURCE_AND_LICENSE.json` trong thư mục bộ asset.

`MODERN_HOUSES_DOWNLOAD_CHECKS.json` ghi kiểm tra tệp, archive và texture. Mỗi thư mục có ảnh `CGTrader_DownloadPage.png` của trang tải chính thức. Cần kiểm tra tỉ lệ, vật liệu URP, collider và ngân sách hình học khi tích hợp; kết quả tải không thay thế kiểm tra trong Unity.
