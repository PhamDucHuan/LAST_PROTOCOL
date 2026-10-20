# Map Cần Thơ và Đại học Cần Thơ Khu II

> **Bản hiện tại:** [nhà bổ sung trên đất cũ revision9 / Protocol19](CAN_THO_BUILDING_INFILL.md). Thêm 1.100 nhà, tổng 1.351 nhà ngoại vi; giữ nguyên nền 940 × 840 m, campus, đường và hồ. [Kết quả kiểm tra](CAN_THO_BUILDING_INFILL_TEST_RESULTS.md).

> **Lịch sử revision8:** [ngoại vi và bờ hồ / Protocol18](CAN_THO_STREETSCAPE.md). Thêm nhà trong hẻm, cây, ghế và đèn.

> **Lịch sử:** [ngoại vi theo Google Maps revision7 / Protocol17](CAN_THO_PERIPHERAL_LAYOUT.md). Chỉnh mạng đường, bổ sung bờ hồ và sắp lại167 nhà; campus/Recon giữ nguyên. Các bản dưới đây là lịch sử.

> **Lịch sử:** [ngoại thất theo ảnh revision5 / Protocol15](CAN_THO_ARCHITECTURE_REALISM.md). Bố cục schematic revision4 giữ lại, nâng kiến trúc, vật liệu, cây và collider. Các đoạn protocol/stat phía dưới là lịch sử.

> **Lịch sử 16/10/2026:** [bố cục sơ đồ Campus II revision4](CAN_THO_SCHEMATIC_LAYOUT.md), Protocol14. Đã sắp lại nhà/đường/hàng rào theo ảnh mới của người dùng và bổ sung các khu còn thiếu. Phần bên dưới là lịch sử bản dựng đầu, gồm protocol và thống kê cũ; dùng hướng dẫn revision4 khi chơi hoặc chỉnh map hiện tại.

**Cập nhật hiện tại:** đã có art revision 2 chi tiết và phong cách MEKONG tương lai. Xem `CAN_THO_FUTURE_ART.md` và `CAN_THO_FUTURE_TEST_RESULTS.md`. Các số liệu/bối cảnh của bản đầu bên dưới được giữ để tham khảo lịch sử; bản build mới dùng protocol 9.

Map đầu tiên của thành phố Cần Thơ trong LAST_PROTOCOL là một bản dựng 3D có thể khám phá, lấy bố cục từ sơ đồ Khu II do người dùng cung cấp. Các công trình được tạo bằng Blender, xuất FBX và tích hợp vào Unity URP. Hai sân test cũ được giữ nguyên.

Đây là bản môi trường prototype theo phong cách low-poly. Kích thước, khoảng cách và hình dáng kiến trúc được giản lược cho gameplay; không phải dữ liệu địa chính hay mô hình khảo sát chính xác. Các tháp/console MEKONG là chi tiết hư cấu của game. Không có nhiệm vụ, boss hay chức năng hack mới trong đợt làm map này.

## Căn cứ thiết kế

- Sơ đồ Khu II trong ảnh người dùng cung cấp: vị trí tương đối các cổng, cụm trường/khoa, ký túc xá và các trục đường.
- `DEADLINE_LAST_PROTOCOL_Quy_Trinh_Do_An.docx`: Cần Thơ là thành phố đầu tiên, AI tên MEKONG, thế giới chia thành từng thành phố semi-open-world. Chỉ dùng tài liệu làm bối cảnh, không tự triển khai các hệ thống khác được liệt kê trong đó.
- [Trang chính thức Đại học Cần Thơ](https://www.ctu.edu.vn/gioithieu/cac-khu.html), tham khảo ngày 2026-10-15: Khu II ở đường 3/2; các công trình ATL, RLC, nhà điều hành, thư viện, hội trường và ký túc xá. Không sao chép model/texture thương mại hoặc tải thêm package.

## Nội dung

Khu vực dựng khoảng **560 × 430 m** gồm campus và dải nhà phố phía trước. Cảnh có ba cổng A/B/C, đường 3/2, đường nội bộ, vỉa hè, vạch đường, Hội trường Rùa, Nhà điều hành, Trung tâm Học liệu, ATL, RLC, cụm CNTT và Truyền thông, Nông nghiệp, Khoa học Tự nhiên, Kinh tế, Bách khoa, Thủy sản, Môi trường, Sư phạm, các dãy nhà học, ký túc xá A/B và khu thể thao.

Cây bóng mát, cây hồng, một số cây dạng cọ, ghế, đèn đường, mái che, cửa sổ và biển tên tiếng Việt tạo thêm chi tiết. Nhà phố, cửa hàng phía ngoài là các mô hình tổng quát; tên và mặt tiền không tái hiện từng cửa hàng thật. Môi trường hiện chỉ chơi ở ngoài trời; nhà có khối va chạm, chưa có nội thất hoặc tầng có thể đi vào.

## Các file chính

| File trong project | Vai trò |
| --- | --- |
| `ArtSource/Blender/CanThoCampus/CanThoCampus.blend` | Nguồn Blender, chia collection theo cụm công trình để chỉnh sửa |
| `Tools/Blender/BuildCanThoCampus.py` | Dựng cảnh và tạo dữ liệu bố trí bằng script |
| `Tools/Blender/FinishCanThoCampus.py` | Gộp mesh theo collection/vật liệu và xuất FBX; render từ nguồn |
| `Tools/Blender/PolishCanThoCampus.py` | Bản sửa giao lộ và góc máy từ lần dựng đầu; luôn tạo output mới |
| `Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.fbx` | Model Unity dùng trong scene |
| `Assets/_Project/Art/Environments/CanThoCampus/CanThoCampus.layout.json` | Bảng màu, collider, vị trí công trình và các điểm kiểm tra đường đi |
| `Assets/_Project/Materials/CanThoCampus/` | Vật liệu URP Lit, ánh xạ theo tên vật liệu Blender |
| `Assets/_Project/Scenes/CanThoCampus/CanThoCampus.unity` | Màn chơi riêng với môi trường, ánh sáng, camera, HUD và bốn điểm spawn |
| `Assets/_Project/Settings/CanThoCampusNavigation.asset` | NavMesh riêng cho Host điều khiển robot |
| `Assets/_Project/Prefabs/UI/CanThoOperationsMenu.prefab` | Menu có ba lựa chọn, giữ nguyên prefab menu cũ |
| `Assets/_Project/Scripts/Editor/CanThoMapSetupTool.cs` | Công cụ thiết lập và kiểm tra map; giữ nguyên scene đã có khi chạy lại |

Model xuất có **179 renderer**, khoảng **136.076 triangle** từ **6.028 object** nguồn. Collider chính gồm **208 BoxCollider**; NavMesh còn có bốn vùng loại trừ vị trí mục tiêu bắn được spawn lúc chạy. Đây là số liệu asset, chưa phải kết quả đo FPS hoặc số draw call thực tế.

## Chơi thử

1. Chờ Unity import/compile xong. Mở `Assets/_Project/Scenes/MainMenu/MainMenu.unity`.
2. Play → **PLAY CO-OP** → chọn **CAN THO / CAMPUS II** → **CREATE ROOM**.
3. Một Client dùng cùng bản build nhập Join Code. Tất cả Ready; Host Deploy.
4. Bốn điểm spawn nằm gần cổng B. Đi theo trục chính vào campus; khu robot/mục tiêu test nằm quanh vùng giữa gần Nhà điều hành và Hội trường Rùa.
5. WASD di chuyển, chuột xoay camera, LMB bắn, giữ E hồi sinh. Khi cần dùng chuột UI, nhấn Esc. Điều kiện bắt đầu mặc định vẫn là ít nhất hai người.

Build mới dùng **protocol 8** để tránh kết nối với bản cũ không có map. Host và Client cần cùng bản. Khi chuyển sang máy khác, chép cả thư mục chứa `.exe`, thư mục `_Data`, DLL và những file đi kèm.

## Thiết lập và chỉnh sửa

Trong Unity, **LastProtocol → Maps → Install Can Tho Campus** kiểm tra vật liệu, scene, menu và Build Settings. Công cụ không ghi đè scene, NavMesh, prefab menu hoặc vật liệu đã tồn tại. Config chỉ đổi tham chiếu menu sang prefab mới; Build Settings chỉ bổ sung scene. Có bản sao lưu dưới `Builds/CanThoBackups`. Công cụ từ chối Play Mode hoặc scene chưa đặt tên chưa được lưu.

**LastProtocol → Maps → Build Windows Can Tho Demo** tạo Windows Development build với MainMenu đầu danh sách, hai sân test và CanThoCampus. Không cần thay scene hiện tại của người dùng để dựng map: quá trình tạo mới sử dụng scene riêng; kiểm thử tự động của đợt này chạy trên `Builds/ValidationProject`.

Để tạo một phiên bản art mới bằng Blender:

```powershell
.\Tools\Blender\Build-CanThoCampus.ps1 -Render
```

Output nằm trong một thư mục timestamp mới dưới `Builds/CanThoMap`; lệnh không tự thay art Unity. Sửa vị trí bằng script nên giữ hệ tọa độ Unity ở các hàm bố trí. Hàm `cv` chuyển sang Blender `(-x,-z,y)`; xuất FBX Forward -Z, Up Y, Scale 1, FBX Units Scale. Không đưa `.blend` vào Assets để tránh Unity tự chuyển đổi phụ thuộc cài đặt Blender trên từng máy.

Khi sửa nguồn trong Blender, cần xuất FBX và cập nhật collider/layout cùng NavMesh nếu di chuyển công trình. Bản installer hiện giữ scene đã tồn tại để bảo vệ chỉnh sửa; nó không tự tái tạo collider/NavMesh sau mỗi lần sửa nguồn. Hãy sửa trong bản sao scene hoặc tạo phiên bản map mới trước khi thay đổi bố cục lớn. `.meta` phải được giữ cùng asset để không mất tham chiếu.

## Giới hạn hiện tại

Robot, máu, bắn và hồi sinh dùng các hệ thống sẵn có. Chưa thêm enemy khắp campus, AI Core tương tác, nhiệm vụ chiếm lại trường, traffic, NPC hoặc nội thất. Các collider hộp là xấp xỉ, chưa bao phủ từng chi tiết trang trí như cột hành lang hay từng múi mái. Cần tiếp tục chơi thử các góc hẹp, camera gần tường và địa hình sau khi chỉnh art.

Vật liệu màu phẳng native URP; chưa có bộ texture PBR, lightmap bake, LOD, streaming hay occlusion bake. Chưa đo hiệu năng trên bốn máy thật qua Internet. Không coi ảnh render Blender là bằng chứng game chạy thành công; kết quả Unity và network test được ghi riêng trong `CAN_THO_MAP_TEST_RESULTS.md`.
