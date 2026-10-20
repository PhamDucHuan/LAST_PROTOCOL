# Đối chiếu nhà trên nền đất hiện tại — 20/10/2026

Yêu cầu: bổ sung công trình vào đất trống đang có, không mở rộng map. Đã xem trực tiếp cả bản đồ đường và ảnh vệ tinh Google Maps trong trình duyệt, không tải ảnh vệ tinh/tile vào game.

- [Khung Khu II và khu phố đối diện đường 3/2](https://www.google.com/maps/@10.0299337,105.7706153,16z): các dãy nhà hẹp, kéo dài sâu vào ô phố; hướng nhà bám các trục 3/2, Mạc Thiên Tích, Trần Văn Hoài và Mậu Thân. Ở mức phóng to nhìn được các mái tôn xám, xanh, đỏ và nhà sân thượng xen nhau.
- [Hồ Búng Xáng](https://www.google.com/maps/search/?api=1&query=10.0316129,105.7641478): bờ ngoài campus có các cụm nhà mái dài và nhà ống san sát; phía trong trường vẫn có sân thể thao, ký túc xá và khoảng xanh. Không dùng nhà dân để lấp sân bóng/campus hoặc mặt nước.
- [CTU — các khu thuộc trường](https://www.ctu.edu.vn/gioithieu/cac-khu.html): đối chiếu nhận diện campus và công trình. Giữ bố cục trường đã chốt ở các lượt trước.

Chuyển thành bản dựng: thêm nhà theo dãy trong các ô còn trống, xen 1–4 tầng, mái tôn và sân thượng. Dãy nhà có khe đi bộ và sân trước, không thêm đường chính, không đổi đất nền 940 × 840 m. Các góc định hướng/ô nhà được chọn bằng tay và lập trình trong hệ tọa độ gameplay hiện có; không phải số hóa từng thửa địa chính hoặc bản sao 1:1.

Mặt tiền và normal map được vẽ thủ tục riêng trong `FillExistingCampusLand.py`, gồm cửa cuốn, kính, khung cửa, ban công, điều hòa và vệt thời tiết. Không dùng ảnh Google làm texture, không gán tên cửa hàng/địa chỉ thật. Những mái pin năng lượng là chi tiết hư cấu của LAST PROTOCOL.
