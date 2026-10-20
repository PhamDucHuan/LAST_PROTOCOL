# Tham khảo kiến trúc cho revision5 — 16/10/2026

Sử dụng các ảnh đã xem trong `REFERENCES.md` và nguồn chính thức bên dưới để đối chiếu đặc điểm ngoại thất. Không đưa ảnh mạng, người trong ảnh hoặc tile Google vào texture/model. Texture bề mặt được tạo mới bằng script; các kích thước vẫn là ước lượng cho map gameplay, không phải bản vẽ thi công.

| Nguồn | Điều quan sát / quyết định dựng |
|---|---|
| [Các khu thuộc trường](https://www.ctu.edu.vn/gioithieu/cac-khu.html) và [ảnh Nhà điều hành](https://www.ctu.edu.vn/images/upload/images/IMG_4835.jpg) | Khung xanh xám, các ô cửa nhiều thanh, ban công và mái sảnh thấp; giữ các khối phụ. Thêm chi tiết cửa/máy lạnh/thoát nước ở tỷ lệ ước lượng |
| [Nhà điều hành và sân trước – ISDS](https://isds.ctu.edu.vn/2023/venue) | Sân, bia đá, cờ và cây; bố cục chính vẫn theo sơ đồ người dùng |
| [Học liệu – trang địa điểm AUNILO](https://storage.ctu.edu.vn/aunilo2018/index.php/meeting-venues/map), [ảnh mặt ngoài](https://storage.ctu.edu.vn/aunilo2018/images/LRC/learning-resource-center.jpg) | Chữ đỏ Việt/Anh ở khối đứng, mảng ốp nhôm và hệ lam; biển vào sảnh được sửa để nhận diện thư viện thay chữ MEKONG quá lớn |
| [Giới thiệu Hội trường Rùa](https://dpa.ctu.edu.vn/su-kien/55-gioi-thieu-ve-hoi-truong-rua.html), [ảnh mặt trước đã xem](https://photos.wikimapia.org/p/00/01/63/61/22_big.jpg) | Dựng lại mái dốc/gable, fascia trắng, các kính đứng và mảng trang trí hình học; không dùng mái xanh bóng như nhà kính |
| [Cổng trường trong bản tin Tuổi Trẻ](https://tuoitre.vn/truong-dai-hoc-can-tho-thanh-dai-hoc-can-tho-20250715141045551.htm) | Cổng thấp, lối mở phía trên, biển trắng/chữ sẫm, nhà bảo vệ; bỏ thanh quét lớn bắc ngang lối đi. Không chép người/xe/biển số trong ảnh |
| [RLC trên trang trường](https://www.ctu.edu.vn/images/upload/images/RLC.jpg) | Cụm phòng thí nghiệm trắng với lam che nắng. Khối dựng lại là diễn giải kiến trúc, chưa có kích thước hay ảnh đủ mọi mặt |
| [CICT chính thức](https://www.cit.ctu.edu.vn/encict/), [ảnh](https://www.cit.ctu.edu.vn/encict/images/update2023/anh%20tong%20quan.jpg), [ISDS](https://isds.ctu.edu.vn/2023/) | Mái đón xanh–trắng gấp khúc và biển song ngữ; không dùng ảnh Khoa CNTT ở Khu III để khẳng định là mặt tiền Khu II |
| [KTX B – ảnh trên trang tiếng Anh CTU](https://www.ctu.edu.vn/en/index.php), [ảnh](https://www.ctu.edu.vn/images/2021/08/09/kytucxa-khub.png) | Màu sơn sáng/điểm vàng, cửa và sân cây. Bố cục số khối trong game giữ từ sơ đồ, không khẳng định dựng đủ toàn bộ phòng ở |

Một số URL ảnh mới có thể không được máy tìm kiếm tải ra; không dùng lỗi tải đó làm bằng chứng đã khảo sát đầy đủ. Năm ảnh mặt ngoài cổng, Nhà điều hành, Học liệu, Hội trường và ATL đã có bản tham khảo cục bộ và được xem trực tiếp. Các nguồn CICT cung cấp đặc điểm mái đón; phần còn lại ngoài ảnh dùng giải pháp kiến trúc tương thích và được ghi là ước lượng.

## Mức độ khớp và giới hạn

- Giữ đường bao/landmark/ba cổng của schematic revision4; không tự chuyển vị trí khoa theo thông tin hiện tại khác với sơ đồ được người dùng chọn.
- Ưu tiên hình khối và đặc điểm nhìn thấy của các công trình nhận diện chính; thêm khung cửa, lam, máy lạnh, downpipe cho các dãy còn lại.
- Khuôn viên chủ yếu là ngoại thất. Cửa kính nhìn thật hơn không có nghĩa đã có nội thất chơi được phía sau.
- Các nhãn CTU trong game dùng chữ và hình khối dựng riêng; không tuyên bố mô phỏng đúng logo hay mọi dòng chữ đang có tại trường.
- Hệ vật liệu gồm albedo + normal và roughness/metallic tham số; không gọi đây là photogrammetry hay texture PBR scan ngoài đời.
- Dải sáng MEKONG và thiết bị tương lai còn ở mức nhỏ, tránh lấn át kiến trúc trường.
