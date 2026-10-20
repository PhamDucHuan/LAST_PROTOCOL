# Ngoại thất campus theo ảnh thực tế — revision5

> **Bản hiện tại:** [ngoại vi và bờ hồ revision8 / Protocol18](CAN_THO_STREETSCAPE.md). Thêm nhà trong hẻm, cây, ghế và đèn; giữ mạng đường/campus revision7. Số liệu bên dưới thuộc lịch sử.

> Bản chơi hiện hành: [revision7 / Protocol17 — ngoại vi theo Google Maps](CAN_THO_PERIPHERAL_LAYOUT.md). Ngoại thất campus trong tài liệu này vẫn được giữ lại; số liệu Protocol15 là lịch sử.

Ngày 16/10/2026, Protocol15. Dùng bố cục sơ đồ revision4, nâng hình khối/mặt tiền và vật liệu để các công trình nhận diện được gần với ảnh thực tế hơn. Nguồn đối chiếu, mức độ chắc chắn và giới hạn ở [REFERENCES_REALISM.md](../../../ArtSource/References/CanThoCampus/REFERENCES_REALISM.md).

## Nội dung thực hiện

| Khu vực | Hoàn thiện ngoại thất |
|---|---|
| Cổng C/B/A | Bỏ khối quét bắc ngang; trụ thấp 3,4m, biển trường, chữ Việt/Anh, nhà bảo vệ, nan cửa và lá cửa xếp bên lối mở; giữ khoảng thông 16m |
| Nhà điều hành | Khung cửa chia ô, transom, rèm phía trong, máy lạnh, downpipe, cờ màu và bề mặt sơn xanh xám/sơn sáng; giữ thân nhà và khối phụ đã dựng theo ảnh |
| Học liệu | Mảng ốp bạc chia joint, lam và kính, chữ đỏ Việt/Anh, biển thư viện tại sảnh; giữ phần mặt cong đã kiểm tra collider |
| Hội trường Rùa | Dựng lại mái dốc, gable, fascia sáng, kính/mullion đứng, hoa gió hình học, mái sảnh và cột; collider thân/mái/sảnh riêng |
| RLC | Thay khối đặc dày bằng mặt nhà phòng thí nghiệm dài và hai cánh hồi phía sau; lam che nắng, canopy và biển vào |
| CNTT/Truyền thông | Mái đón xanh–trắng, đường gấp khúc và biển song ngữ theo đặc điểm thấy trong nguồn CICT |
| Các dãy khoa/ký túc xá | Khung cửa/sill, một số máy lạnh và mảng vàng ở khu KTX; sơn, ngói và bê tông có chi tiết bề mặt |
| Cây và đường | Thân/cành và leaf-card cutout thay tán khối cũ, có cả cây tiền cảnh; nền lát, nhựa đường, ngói, cỏ/bark; đèn đường tĩnh và thoát nước nằm ngoài các điểm tập hợp |

Các thiết bị MEKONG còn ở mức nhỏ, không còn gantry lớn trên cổng. Không thêm ánh sáng động cho từng đèn đường, giao thông hoặc NPC. Các biển và thiết bị môi trường không có nghĩa đã có tương tác gameplay.

## Vật liệu

`CampusSurfaceLibrary.py` tạo **21 PNG 512×512**: 10 cặp albedo/normal và 1 atlas lá có alpha. Các chất liệu là plaster, concrete, blue paint, red paver, paving slab, asphalt, terracotta, silver cladding, grass và bark. Roughness/metallic dùng tham số; không phải vật liệu chụp scan hoặc photogrammetry.

`FinishCanThoCampus.py` hỗ trợ thuộc tính vật liệu `lp_uv_metres`: UV chiếu theo mặt phẳng trội ở tỷ lệ tile2m, tránh kéo một ảnh lên toàn bức tường lớn. Vật liệu không có thuộc tính này giữ UV cũ; leaf-card giữ UV atlas0–1.

`CanThoMapSetupTool.Palette` nhận baseMap/normalMap/normalScale/alphaClip/doubleSided tùy chọn. Texture bắt buộc trong thư mục Textures của map. URP/Lit dùng BaseMap, BumpMap, normal tuyến tính, shader keyword thích hợp; lá dùng AlphaTest/cull off. Không thêm shader hoặc package bên ngoài.

Ảnh texture được đóng gói trong `.blend` và lưu riêng cạnh nguồn để portable; Unity dùng bản PNG trong `Assets/_Project/Art/Environments/CanThoCampus/Textures/`. Không cần Blender ở máy Client.

## Nguồn và cập nhật an toàn

- Nguồn đang dùng: `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`, layout và Textures cạnh file.
- Model/sidecar: `Assets/_Project/Art/Environments/CanThoCampus/`.
- Công cụ dựng: `Tools/Blender/RefineCampusArchitecture.py`, nhận revision4 và output mới; `PackCampusSurfaces.py` và `RefineCampusCanopies.py` xử lý candidate đã có theo từng bước, không sửa nguồn đầu vào.
- Backup trước thay đổi: `Builds/CanThoBackups/20261016-ArchitectureRealism/`.
- Candidate/log/tests: `Builds/CanThoMap/20261016-ArchitectureRealism/`.
- Apply hiện hỗ trợ revision2–5; từ chối PlayMode hoặc campus đang dirty; có backup. Chạy lại cùng revision không ghi lại scene/NavMesh.

Mọi thay đổi được thử trong `Builds/ValidationProject` trước khi chép asset đã kiểm tra về project chính. Giữ GUID scene/FBX/NavMesh và giữ các map/player/room hiện có. Tâm nhiệm vụ Recon và bốn spawn không đổi. Protocol tăng15 vì va chạm cổng, Hội trường và RLC đã thay đổi.

## Chơi thử

Mở MainMenu → PLAY CO-OP → CAN THO / CAMPUS II → tạo/join phòng → Ready → Deploy. **Mọi máy dùng cùng build Protocol15**; bản14 trở về trước không khớp collider. F8/F9 vẫn phục vụ chế độ nhẹ và FPS/RTT.

Menu **LastProtocol → Maps → Build Windows Campus Architecture** xuất `Builds/CanThoArchitecturePlayer/`. Với quy trình validation, build nằm dưới thư mục đó trong ValidationProject. Xem [kết quả kiểm tra](CAN_THO_ARCHITECTURE_TEST_RESULTS.md).

Đây là ngoại thất được nâng chi tiết theo các ảnh có sẵn. Kích thước vẫn nén cho gameplay; chưa đủ ảnh mọi góc để khẳng định mô hình1:1. Các khoa ngoài vùng ảnh quan sát được vẫn có chi tiết kiến trúc ước lượng. Không thêm nội thất chơi được hay các cơ chế campaign mới.
