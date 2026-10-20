# Kiểm tra map ngoại vi và bờ hồ — revision8 / Protocol18

Hoàn thiện ngày 17/10/2026. Ứng viên cuối: `Builds/CanThoMap/20261017-Streetscape/Candidate02`. Kiểm tra trong `Builds/ValidationProject`, sau đó tích hợp đúng các file đã kiểm tra vào project chính. Không điều khiển hoặc đóng Unity Editor đang mở của người dùng.

| Hạng mục đã chạy | Kết quả | Bằng chứng trong thư mục task |
|---|---|---|
| Xuất Blender | PASS: 49.503 mesh nguồn, 430 renderer gộp, 581.597 triangle, 1.245 collision box, 40 landmark | `Candidate02/BuildStats.json`, `Blender02.log` |
| Bảo toàn nguồn | PASS: toàn bộ 39.635 mesh nguồn cũ, collider cũ và 167 nhà cũ giữ nguyên | `PreservationFinal.json` |
| Bố trí nhà | PASS: tổng 251 nhà, các lô không chồng nhau/campus/nước, hành lang đường chính và hẻm thông thoáng | `PreservationFinal.json` |
| Texture/budget | PASS: 21 PNG đóng gói và đường dẫn tương đối, dưới 500 renderer/600.000 triangle | `PreservationFinal.json` |
| Import/scene/NavMesh | PASS, process exit0 | `Refresh.log`, `Refresh-ExitCode.txt` |
| Unity EditMode | **123/123 PASS**, không fail/skip; process exit0 | `EditModeFinal.xml`, `EditModeFinal.log` |
| Hẻm mới | PASS: 7 hẻm có kết nối đường chính, NavMesh toàn tuyến; capsule đi hai chiều, không kẹt hoặc rơi khỏi nền | `CanThoStreetscapeTests` trong XML |
| Cây/ghế/đèn | PASS: các vị trí metadata có BoxCollider tương ứng trong scene | `CanThoStreetscapeTests` trong XML |
| Hồi quy map | PASS: nhà cũ/mới đúng collider, tuyến capsule quanh ngoại vi, cổng/rào/spawn, landmark/camera, nước không walkable, budget và installer chạy lại | Các test Neighborhood/Peripheral/Fence/Route/Schematic/Architecture |
| Windows build | PASS, process exit0 | `WindowsBuild.log`, `WindowsBuild-ExitCode.txt` |
| Co-op direct localhost | **6/6 tiến trình PASS**: Host+3Client, người thứ5 bị từ chối, phòng đã bắt đầu từ chối người vào thêm | `RuntimeEvidence/MissionSmoke/Summary.json` |
| Di chuyển/nhiệm vụ | PASS: đồng bộ di chuyển/xoay/chạy/nhảy, chọn scene/Ready, nhiệm vụ chung/hoàn thành/wipe/restart | JSON/log Host và 3Client trong MissionSmoke |
| Graphical runtime | **95/95 PASS**, complete=true, không failure; process exit0 | `RuntimeEvidence/GraphicalPreview/Checks.json`, `Player.log` |
| Ảnh Unity | PASS, capture exit0, 18 PNG; đã xem hẻm, overview và các góc hồ/khu ăn uống | `UnityCaptureFinal/`, `CaptureFinal.log` |
| Tích hợp | PASS: 224 asset/meta đúng hash; giữ GUID scene/model/nav/menu, 8 file được bảo vệ và 69 file C# khớp bản kiểm tra | `PromotedAssets.json`, `FinalVerification.json` |
| Đóng gói | PASS: 376 file build, manifest/hướng dẫn, đọc lại toàn bộ entry nén để kiểm tra hash/size | `Package.log`, `FinalVerification.json` |
| Relay Internet/FPS máy nhóm | **CHƯA KIỂM THỬ cho Protocol18** | Cần cùng gói mới trên các máy thật |

95 kiểm tra graphical gồm menu/chọn màn/Ready/Deploy/return, bộ lấy mẫu FPS, agent đi 7 chặng campus và 23 chặng ngoại vi, 4 camera probe, nhiệm vụ và HUD. Hẻm mới được kiểm tra bằng NavMesh và CharacterController trong EditMode; chưa thử enemy AI chiến đấu trong từng hẻm. Agent trong fixture chạy nhanh để rút ngắn thời gian, không đổi tốc độ enemy gameplay.

Lượt smoke này tập trung di chuyển/scene/nhiệm vụ. Các trường health/combat/enemy/revive trong JSON mặc định có thể false vì không bật fixture tương ứng; không xem chúng là PASS cho việc thử lại chiến đấu. Không sửa code gameplay/mạng ngoài tăng protocol để ngăn ghép hai bản hình học khác nhau.

## Sửa lỗi trong lượt hoàn thiện

Ảnh ứng viên đầu cho thấy khe tam giác nền đất tại khúc cua ven hồ, do hai dải mặt đường/vỉa hè kết thúc ở góc khác nhau. `SealPeripheralBends.py` bổ sung 58 mặt vá bo tròn ở các điểm gấp của polyline. Không đổi mesh/collider gốc. Ứng viên cuối đã được import, bake, chạy lại đủ 123 test, chụp và build trước khi thử runtime.

Kiểm tra bảo toàn được tăng phạm vi từ campus lên toàn bộ mesh cũ và collider cũ. Tối ưu việc lấy collection bằng bảng tra một lần để tránh truy vấn lặp trên gần 50.000 đối tượng; không bỏ bớt đối tượng hoặc điều kiện kiểm tra.

Trong bản hướng dẫn đóng gói thử có nhắc thùng rác nhưng các vị trí dự kiến đều bị kiểm tra khoảng trống loại bỏ. Đã bỏ mô tả này và đóng gói lại hướng dẫn đúng với nội dung thực tế. Gói thử được giữ riêng ở `DraftPackage/`, không dùng làm bản phát hành. Bộ file game không đổi giữa hai lần nén.

## Gói dùng để thử

`Builds/Distributions/LAST_PROTOCOL-Campus-Protocol18-Streetscape-20261017.zip`

- Dung lượng: 101.029.820 byte.
- SHA256: `772F76936496DED3DA226254EA0C1CBE97BD1C9D3680A41E58F6E6534A34B902`.
- Host và Client phải cùng Protocol18, giải nén đủ thư mục và tạo phòng/Join Code mới.

Chưa phải tái tạo địa hình/công trình tỷ lệ1:1, chưa khảo sát cao độ/thửa nhà ngoài thực tế, chưa có nội thất/giao thông AI hoặc LOD/streaming mới. Các hẻm và nhà bổ sung là bố trí gameplay. Số tam giác/renderer đạt giới hạn không chứng minh FPS ổn định trên mọi máy; cây alpha làm tăng overdraw nên cần đo FPS/RTT/loss trên các máy mục tiêu.
