# Khảo sát ngoại vi campus bằng Google Maps — 16/10/2026

Đã mở trực tiếp Google Maps trong trình duyệt, xem cả lớp ảnh vệ tinh và bản đồ đường ở khu vực Khu II ĐHCT. Không đăng nhập, không sửa dữ liệu Google Maps, không tải tile/ảnh vệ tinh vào artwork game.

## Những điểm đã thấy trên bản đồ

- [Khung campus và đường xung quanh](https://www.google.com/maps/@10.0299337,105.7706153,16z): đường 3 Tháng2 chạy dọc mặt trước campus; Mậu Thân ở đầu phía cổngA và kéo về phía khu dân cư đối diện; Trần Văn Hoài nối đường3/2 với Mậu Thân. Mạc Thiên Tích tạo tuyến gấp khúc trong khu dân cư giữa hai trục.
- [Nút Búng Xáng–3/2](https://www.google.com/maps/search/?api=1&query=10.0247282,105.7675505): lối nối ở phía bên campus, không phải một trong ba hẻm đối diện ba cổng như bản prototype6.
- [Công viên Bờ Hồ Búng Xáng](https://www.google.com/maps/search/?api=1&query=10.0316129,105.7641478): nằm về phía tây campus. Trong khung ảnh/bản đồ campus, phần nước dài có nhãn Rạch Rau Muống; model7 thể hiện vùng mặt nước nối cảnh quan hồ/rạch phía này, không khẳng định đã đo vẽ toàn bộ lưu vực.
- Hướng Nguyễn Văn Cừ ở phía ngoài đầu bắc/tây bắc vùng hồ; game chỉ dựng một tuyến dẫn hướng đơn giản để nối vòng đường.

## Nguồn đối chiếu

- [CTU — tài liệu hướng dẫn sinh viên Khoa Môi trường](https://cenres.ctu.edu.vn/images/upload/news/tuyensinh2018/Thng-tin-hu-ch-cho-sinh-vin-TN.pdf): kết quả tìm kiếm chỉ ra sơ đồ tiếp cận khoa qua Hẻm51. Tài liệu2018, dùng đối chiếu quan hệ vị trí; không dùng làm bằng chứng tên đường hiện tại.
- [Báo Cần Thơ — Búng Xáng và địa danh có thành tố Búng](https://baocantho.com.vn/bung-xang-va-dia-danh-co-thanh-to-bung-o-bscl-a139733.html): lịch sử mặt nước sau KhuII và liên hệ Hẻm51–đường3/2–Nguyễn Văn Trỗi.
- [CTU — KhuII](https://www.ctu.edu.vn/khu-2.html): xác nhận vị trí campus trên đường3/2.

## Cách chuyển thành model

`Design/Maps/CanThoCampusII/PERIPHERAL_PLAN.json` là bản vẽ tay bằng tọa độ game từ quan hệ đường và mặt nước đã quan sát. TrụcX game chạy theo hướng đường3/2 từ phía cổngC tớiA; trụcZ đi vào campus. Hướng bắc địa lý vì vậy không trùng trụcZ.

Giữ nguyên campus theo sơ đồ người dùng và ba vị trí gate đã dùng trong gameplay. Nén khoảng cách ngoài trường để tạo vòng khám phá; chiều rộng đường, vị trí từng thửa, cao độ, chiều sâu nhà và các đoạn cua là ước lượng thiết kế. Đây chưa phải model1:1, dữ liệu GIS hoặc kết quả khảo sát GPS. Không đặt địa chỉ hoặc thương hiệu thật lên từng căn nhà.

Các chi tiết công nghệ, tủ cứu trợ, quầy hàng và lan can đi bộ được thêm cho LAST_PROTOCOL. Không dùng chúng để khẳng định hiện trạng ngoài đời.
