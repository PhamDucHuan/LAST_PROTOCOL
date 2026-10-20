# Asset Việt Nam miễn phí — tải ngày 20/10/2026

Đã lưu đủ 4 lựa chọn được yêu cầu. Các tệp được giữ trong ArtSource, chưa nhập vào Assets hoặc thay đổi scene map.

## Đã tải

### Hoi An Ancient House Model — buiductam1202

- Nguồn: https://www.cgtrader.com/free-3d-models/architectural/architectural-street/hoi-an-ancient-house-model
- Tải sau khi người dùng đăng nhập CGTrader, từ trang tải chính thức của mẫu miễn phí.
- Thư mục `HoiAnHouse/`: `HoiAnHouse_M2.fbx`, `HoiAnHouse_M2.blend`, `Texture_M2.rar`.
- Texture đã giải nén tại `HoiAnHouse/Extracted/Texture_M2/`, gồm 30 ảnh JPG.
- Trang nguồn ghi Royalty Free License (no AI). Nguồn điều khoản: https://www.cgtrader.com/pages/terms-and-conditions
- Chưa kiểm tra vật liệu trong Unity hoặc chạy tệp Blender. Cần chuyển URP, tạo collider và LOD trước khi dùng rộng.

### Polygon World – Vietnam — PolyOne Studio

- Nguồn: https://assetstore.unity.com/packages/3d/environments/polygon-world-vietnam-328183
- Đã thêm bản miễn phí vào thư viện tài khoản Unity do người dùng đăng nhập.
- Tải bằng Package Manager > My Assets trong Unity hiện có; sao chép gói vào `PolygonWorldVietnam/Polygon World - Vietnam.unitypackage` (7.013.374 byte).
- Kiểm tra cấu trúc tar/gzip đọc được, 244 mục trong archive. Chưa import vào project.
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
- Đây là nhà châu Á truyền thống theo mô tả tác giả; chưa kiểm chứng vật liệu URP, collider, LOD hoặc hiệu năng trong project.

`DOWNLOAD_MANIFEST.json` ghi kích thước và SHA256 của các tệp tải gốc. Đây là kiểm tra tệp tải, không phải kiểm chứng tích hợp hoặc hiệu năng trong Unity.
