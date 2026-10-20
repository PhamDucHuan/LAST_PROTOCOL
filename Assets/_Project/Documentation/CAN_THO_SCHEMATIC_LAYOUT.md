# Campus II — bố cục theo sơ đồ người dùng

> **Bản hiện tại:** [ngoại vi và bờ hồ revision8 / Protocol18](CAN_THO_STREETSCAPE.md). Thêm nhà trong hẻm, cây, ghế và đèn; giữ mạng đường/campus revision7. Số liệu bên dưới thuộc lịch sử.

> Bản chơi mới nhất: [revision7 / Protocol17 — ngoại vi theo Google Maps](CAN_THO_PERIPHERAL_LAYOUT.md). Sơ đồ trong trường giữ nguyên.

> [Revision5 / Protocol15](CAN_THO_ARCHITECTURE_REALISM.md) giữ bố cục trong tài liệu này và nâng ngoại thất/vật liệu. Các số liệu build14 bên dưới ghi mốc sắp xếp schematic, không phải bản chơi mới nhất.

Art revision 4, ngày 16/10/2026, network protocol 14. Sơ đồ người dùng gửi trong lần chỉnh này là nguồn bố trí: đường 3/2 ở phía nam, cổng C–B–A từ trái sang phải, hai ký túc xá ở phía bắc, trục trung tâm và đường bao gấp khúc. Bản tham chiếu nguyên vẹn và SHA256 ở `Design/Maps/CanThoCampusII/REFERENCE.json`.

Đây là dựng lại vị trí tương đối từ sơ đồ minh họa, dùng tỷ lệ gameplay ước lượng; không phải số đo khảo sát. Không tải model, map tile, texture hoặc package mới. Giữ kiến trúc CTU đã tạo trong Blender và các chi tiết MEKONG tương lai: kim loại/kính, màn che nắng, solar array, đèn chỉ dẫn, cổng kiểm soát và tủ điện. Những vùng đang xây trong sơ đồ được diễn giải thành khu nghiên cứu/ATL đã phát triển trong bối cảnh 2048, không coi trạng thái công trường trong ảnh là hiện trạng năm 2048.

## Bố cục đã thay đổi

| Cụm trong sơ đồ | Cách thể hiện trong map |
|---|---|
| Mặt nam | Đường 3/2, ba cổng C/B/A, vạch sang đường và dãy phố ngoài trường |
| Tây bắc | KTX B, Viện nghiên cứu ĐBSCL, Thủy sản và cụm nghiên cứu RLC/Biotech |
| Bắc và đông bắc | Bách khoa, B1, nhà thi đấu cũ/mới, KTX A và phần đất cây xanh nhô lên phía bắc |
| Tây và trung tâm | Môi trường, nhà lưới, Nông nghiệp, trục nghiên cứu và đường vòng phía tây |
| Tây nam | Dự bị dân tộc, Sư phạm, C2, THPT thực hành, Hội trường Rùa, Luật, Ngoại ngữ và ATL |
| Giữa và phía đông | CNTT/Truyền thông, Văn phòng Đoàn trường, nhà học C1, KHTN, Kinh tế, Chính trị |
| Phía trước | D2, Nhà điều hành/D1, nhãn Khoa Sau đại học, Học liệu, bãi cỏ, vườn bằng, hai dãy A3 và KHXH/Nhân văn |
| Ranh giới | Hàng rào theo 26 đỉnh bố cục, có khoảng mở tại đúng ba cổng; không dùng hình chữ nhật cũ làm ranh campus |

Khối Nhà điều hành dời về X22 Z−2 để tương quan với Học liệu và bãi cỏ sát hơn sơ đồ. Vòng nhiệm vụ Recon vẫn giữ ba tâm X0 Z−40, X32 Z−21, X57 Z−42; điểm nhà điều hành nằm ở sân trước. Tuyến đi thử và bốn spawn cổng B được giữ, đã kiểm tra lại với collision/NavMesh mới. Đây là thay môi trường, không thêm mission/chip/class/backend từ cốt truyện.

## Dữ liệu và công cụ

- `Tools/Blender/AlignCampusToSchematic.py`: đọc nguồn revision3, đổi bố cục, thêm công trình phụ, tạo đường/cây/hàng rào và xuất sang thư mục mới. Từ chối nguồn sai revision hoặc output đã tồn tại.
- `ArtSource/Blender/CanThoCampus/CanThoCampus.blend`: nguồn chỉnh sửa, từng cụm ở collection riêng; file nằm ngoài Assets.
- `Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx`: mesh đã gộp theo collection/vật liệu, dùng trong scene.
- `CanThoCampus.layout.json`: giữ version1, bổ sung `euler` tùy chọn cho collision, `campusBoundary`, `fenceSpans`, `gates`, `roads`, `schematicPlots` và nguồn tọa độ. Layout cũ thiếu euler vẫn có rotation0.
- `CanThoMapSetupTool` / `CanThoDetailSetupTool`: collider và NavMesh source dùng cùng TRS; không lấy AABB của hàng rào chéo để chặn cả vùng trống. Marker landmark được cập nhật theo dữ liệu mới.
- `CanThoSchematicTests`: quan hệ vị trí theo sơ đồ, đủ landmark, gate ordering, lối vào có NavMesh/path, tâm nhiệm vụ, fence orientation và budget model.

Nguồn có 10.452 mesh object có thể sửa riêng; FBX xuất 263 mesh renderer, 258.948 triangle, 277 collision box và 40 landmark entry. Đây là số liệu asset, không phải FPS hoặc draw call đo trên máy chơi.

Collection `90_InspectionLabels` chỉ có text curve để đọc sơ đồ trong ảnh kiểm tra Blender, không nằm trong FBX hoặc scene game. Ảnh Unity thể hiện model thực tế không có các nhãn nổi này.

## An toàn và tái tạo

Bản trước ở `Builds/CanThoBackups/20261016-SchematicLayout`, gồm Blender, FBX/layout, scene/NavMesh và metadata. Dựng candidate mới, import/bake/test tại `Builds/ValidationProject`, sau đó mới tích hợp các asset đã qua kiểm tra. Giữ GUID scene/FBX/NavMesh, player/room prefab và các map khác.

Ví dụ tái tạo từ **nguồn revision3 trong backup**, không chạy lại trên nguồn revision4:

```powershell
D:\Blender\blender.exe --background --factory-startup --python-exit-code 1 --python Tools\Blender\AlignCampusToSchematic.py -- Builds\CanThoBackups\20261016-SchematicLayout\ArtSource\Blender\CanThoCampus\CanThoCampus.blend Builds\CanThoMap\NewSchematicCandidate --render
```

`LastProtocol → Maps → Apply Can Tho Detailed Art Revision` nhận revision2–4, chỉ thay nhóm môi trường thuộc tool, có backup và từ chối scene đang có thay đổi chưa lưu/PlayMode. Chạy lại cùng revision chỉ kiểm tra, không tái sinh scene. Không tự chuyển scene đang mở của người dùng.

## Chơi thử bản mới

Mở scene `Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity` để xem môi trường. Để thử co-op, bắt đầu từ MainMenu → PLAY CO-OP → CAN THO / CAMPUS II, Host tạo phòng, Client nhập mã, cả đội Ready rồi Deploy.

Dùng **cùng bản Protocol14 trên mọi máy**. Protocol13 có collider/vị trí nhà khác nên bị từ chối kết nối theo version check. `LastProtocol → Maps → Build Windows Campus Schematic` tạo build vào `Builds/CanThoSchematicPlayer`. Không sửa hoặc tắt version check để ghép build cũ.

F8 đổi chế độ nhẹ, F9 hiện/ẩn FPS/RTT, Esc mở chuột. Đi đủ tuyến Recon rồi khám phá trục tới B1/thể thao, KTX, Thủy sản, nhà lưới. Các nhà vẫn chủ yếu là ngoại thất; chưa có nội thất đầy đủ hoặc NPC cho các khoa.

Kết quả kiểm tra và đường dẫn bằng chứng trong [CAN_THO_SCHEMATIC_TEST_RESULTS.md](CAN_THO_SCHEMATIC_TEST_RESULTS.md). Chạy localhost không thay cho đo FPS/RTT và thử Internet trên nhiều máy thật.
