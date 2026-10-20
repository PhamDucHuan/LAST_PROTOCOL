# Asset kiến trúc Việt Nam cho map ĐHCT

Tra cứu ngày 20/10/2026. Giá tham khảo bằng USD, có thể thay đổi. Thông số dưới đây theo trang tác giả/nhà phát hành; chưa tải, mua hoặc thử import vào dự án.

## Ưu tiên tích hợp

- Giữ nguyên nền đất và phạm vi map hiện tại. Thay thế có chọn lọc các nhà khối đơn giản bằng mẫu chi tiết ở tuyến người chơi đi qua.
- Ưu tiên nhà phố, cửa hàng, quán ăn sinh viên và chi tiết vỉa hè quanh cổng trường. Kiến trúc Hội An chỉ phù hợp một số điểm nhấn, không đại diện chính xác cho khu ĐHCT.
- Bản đồ hiện tại khoảng 599 nghìn tam giác, sát ngân sách 600 nghìn. Cần làm LOD, giảm vật liệu và đo lại hiệu năng trước khi nhân bản asset mới. Polygon do cửa hàng công bố không nhất thiết là số tam giác.

## 1. Vietnam Street Corner – Modular Environment Pack — Dina.npa

Nguồn: https://www.fab.com/listings/c6252792-daca-44f2-942b-82e8053138ac

- Nhà phố modular, bảng hiệu, xe bán đồ ăn, bàn ghế nhựa, dây điện và vỉa hè; sát nhu cầu khu phố quanh trường.
- FBX/GLB, vật liệu PBR và texture 1K. Chưa thấy công bố số polygon hoặc LOD.
- Kiểm tra trực tiếp menu license trên Fab ngày 20/10/2026: Personal 1.110.241 VND (gồm 100.931 VND VAT); Professional 1.964.491 VND (gồm 178.590 VND VAT). Trang ghi Standard License. Personal dành cho cá nhân/nhóm nhỏ có doanh thu hoặc vốn tài trợ không quá 100.000 USD trong 12 tháng gần nhất. Chưa mua hoặc tải; chưa xác nhận gói Unity/URP sẵn.
- Đề xuất: lựa chọn đầu tiên cho mặt tiền và góc phố; kiểm tra mesh, tỷ lệ và vật liệu URP trước khi dùng rộng.

## 2. Polygon World – Vietnam — PolyOne Studio

Nguồn Unity: https://assetstore.unity.com/packages/3d/environments/polygon-world-vietnam-328183

Nguồn thông số tác giả: https://www.fab.com/listings/0d25219b-6ba3-44ed-baf7-e7cd6e8823b7?lang=de

- Miễn phí trên Unity Asset Store lúc tra cứu; Standard Unity Asset Store EULA.
- Trang Unity công bố hỗ trợ Built-in, URP và HDRP trên Unity 2022.3.3f1. Chưa kiểm chứng trên Unity 6000.5.4f1 của dự án.
- Tác giả công bố khoảng 100–1.000 tam giác/asset, texture base color 4K.
- Phong cách hoạt hình, nhiều cảnh nông thôn và công trình văn hóa. Phù hợp thử quy trình nhập miễn phí hoặc làm cảnh xa; không phải bộ nhà phố hiện đại sát ĐHCT.

## 3. Asian Street Food Carts & Stalls: Low Poly Pack — AlleyBits

Nguồn tác giả: https://alleybits.itch.io/asian-street-food-pack

- Giá từ 19,99 USD; có demo Phở Dining Set miễn phí.
- 66 kit, 337 mesh, 341 Unity prefab: xe bánh mì, quán phở, bàn ghế nhựa, mái che, biển hiệu, cửa cuốn và phụ kiện phố.
- Unitypackage, FBX, GLB, OBJ, BLEND. Công bố hỗ trợ URP/Built-in, Unity 6000.5+, thử trên 6000.5.9f1.
- Cả bộ 128.190 tam giác; 3 vật liệu, không có LOD. Không hiểu tổng số tam giác này là độ nặng từng vật thể.
- Tác giả cho phép dùng cá nhân/thương mại, không yêu cầu ghi công; cấm phân phối lại tệp asset thô. Đọc điều khoản kèm gói trước khi dùng.
- Đề xuất: ưu tiên bổ sung quán ăn sinh viên và vỉa hè; không thay thế một thư viện nhà hoàn chỉnh. Cần thử trên bản Unity hiện tại.

## 4. Hoi An Ancient House Model — buiductam1202

Nguồn: https://www.cgtrader.com/free-3d-models/architectural/architectural-street/hoi-an-ancient-house-model

- Một nhà Hội An miễn phí; trang ghi Royalty Free License (no AI).
- FBX, BLEND, OBJ, MAX, USD; 40.004 polygon, texture 4K.
- Đề xuất: mẫu thử để đánh giá vật liệu và tạo LOD trước khi cân nhắc bộ 18 nhà. Cần giảm độ nặng trước khi lặp nhiều lần.

## 5. Hoi An Ancient House Pack — Bui Duc Tam

Nguồn: https://www.cgtrader.com/3d-models/architectural/architectural-street/hoi-an-ancient-house-pack

- Giá 30 USD lúc tra cứu; 18 mẫu nhà cổ Hội An; Royalty Free License.
- FBX, BLEND, OBJ, MAX; PBR 1K/2K/4K. Cả bộ 714.333 polygon.
- Đề xuất: chỉ chọn vài nhà có hình dáng phù hợp để tạo điểm nhấn. Cần chuyển vật liệu URP, tối ưu mesh và tạo LOD; không đưa nguyên bộ vào cảnh đang gần ngân sách hình học.

## 6. Hanoi Townhouse — DarumaNeko

Nguồn: https://www.turbosquid.com/3d-models/3d-model-hanoi-townhouse-1775220

- Giá 199 USD; Standard License. Một nhà phố chi tiết, không phải bộ nhiều nhà.
- BLEND, FBX, OBJ, GLTF/GLB, USD; 30.092 polygon, 17 vật liệu, texture đến 4K.
- Đề xuất: ưu tiên thấp do chi phí và độ nặng; chỉ cân nhắc một mặt tiền quan trọng gần người chơi.

## Kiểm tra trước khi nhập chính thức

### Lựa chọn khi chỉ dùng asset miễn phí — cập nhật 20/10/2026

1. Hoi An Ancient House Model: https://www.cgtrader.com/free-3d-models/architectural/architectural-street/hoi-an-ancient-house-model — miễn phí, FBX/BLEND/OBJ và texture 4K; 40.004 polygon, Royalty Free (no AI). Ưu tiên thử một nhà trước, giảm độ nặng và chuyển vật liệu URP.
2. Asian House của HUY TRAN: https://www.blendkit.com/asset-gallery-detail/d0c4d779-5fbb-433d-80e0-c1a4a0e619b8/ — trang ghi Free, Royalty free, 65.152 polygon, 86,1 MiB. Tác giả mô tả nhà truyền thống châu Á gặp ở Việt Nam và các nước lân cận; chưa xác nhận là nhà phố Cần Thơ. Cần lấy vào Blender rồi xuất phù hợp Unity.
3. Polygon World – Vietnam: https://assetstore.unity.com/packages/3d/environments/polygon-world-vietnam-328183 — trang Unity vẫn ghi FREE và hỗ trợ URP trên Unity 2022.3.3f1. Phù hợp phong cách hoạt hình; chưa thử Unity 6 của dự án.
4. Phở Dining Set demo: https://alleybits.itch.io/asian-street-food-pack#download-demo — chỉ tải phần Download demo, tệp AsianStreetFoodPack_PhoDiningSet_Free.zip 1,1 MB. Có 9 kit gồm bàn, ghế, tô phở, đồ gia vị và giỏ/thùng. Toàn bộ pack vẫn trả phí. Trang tác giả cho phép dự án cá nhân/thương mại; đối chiếu điều khoản kèm demo khi tải.

Các lựa chọn miễn phí này không phải bộ thay thế đầy đủ tương đương Vietnam Street Corner. Chưa tải hoặc import các lựa chọn trên vào dự án.

1. Thử một nhà và một cụm quán ăn trong scene riêng; đối chiếu kích thước với đường và nền hiện có.
2. Chuyển vật liệu sang URP/Lit hoặc shader tương thích, kiểm tra texture và tỷ lệ.
3. Giảm số vật liệu, tạo LOD và collider đơn giản; giữ asset gốc ngoài scene chạy thật.
4. Thay thế nhà tại vị trí hiện hữu, giữ lối đi, đường, mặt nước và ranh nền đất.
5. Đo số tam giác, draw call và FPS trong build nhiều người chơi trước khi nhân rộng.

Không có asset nào trong danh sách đã được kiểm chứng chạy trong project chỉ dựa trên việc xuất hiện trên cửa hàng.
