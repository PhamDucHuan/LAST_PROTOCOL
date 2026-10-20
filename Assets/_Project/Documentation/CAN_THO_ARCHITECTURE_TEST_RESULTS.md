# Kiểm tra ngoại thất campus revision5 / Protocol15

Ngày 16/10/2026. Bản được tích hợp là `Builds/CanThoMap/20261016-ArchitectureRealism/Candidate05/`, kiểm tra trong `Builds/ValidationProject`. Đối chiếu file và GUID trước khi chép về project chính; không chuyển scene hoặc đóng Editor của người dùng.

| Hạng mục thực hiện | Kết quả | Bằng chứng dưới thư mục task |
|---|---|---|
| Blender export cuối | PASS: 27.783 object nguồn, 235 renderer, 389.298 triangle, 297 collision, 40 landmark | `Candidate05/BuildStats.json`, `Blender05.log` |
| Texture nguồn portable | **21/21 PASS**, PNG tồn tại, đóng gói trong blend, đường dẫn tương đối, 512×512 | `SourceValidationFinal.json` |
| Unity compile / EditMode | **114/114 PASS**, không fail/skip | `EditModeFinal.xml`, `EditModeFinal.log`, exit0 |
| Normal/UV/lá | PASS: URP maps đúng, normal linear, UV lặp theo mét, foliage alpha-cutout/cull off | `CanThoArchitectureTests` và các test detail trong XML |
| Geometry / navigation | PASS: đường Recon/capsule, 40 entrance, spawn, cổng mở/rào chặn và camera sát tường | Map/Route/Fence/Schematic/Mission tests |
| Windows build | PASS, process exit0 | `WindowsBuild.log`, `WindowsBuild-ExitCode.txt` |
| Graphical runtime riêng | **70 kiểm tra PASS**, không failure, process exit0 | `RuntimeEvidence/GraphicalPreview/Checks.json`, `Player.log` |
| Host + 3 Client localhost | **6/6 Summary PASS** gồm bốn người, RoomFull và RoomClosed | `RuntimeEvidence/MissionSmoke/Summary.json`, JSON/log từng process |
| Bảo toàn và tích hợp | File tích hợp khớp bản đã test; giữ GUID và asset ngoài phạm vi | `PromotedAssets.json`, `FinalVerification.json`, backup |
| Chụp ảnh Editor cuối | Ghi được 11 ảnh, đã xem ngoại thất; **process cuối FAIL khi thoát**, không đánh dấu toàn lượt PASS | `UnityCaptureFinal/`, `CaptureFinal.log`, exit−1073741819 |
| Nhiều máy thật / Relay / FPS | **CHƯA KIỂM THỬ lại** trên revision5 | Không lấy localhost, số mesh hoặc ảnh tĩnh làm benchmark |

Thư mục task là `Builds/CanThoMap/20261016-ArchitectureRealism/`. Báo cáo xác minh cuối được ghi sau khi đóng gói. Gói phát hành có manifest/hash được đọc lại từng entry bằng `Tools/Package-WindowsPlayer.ps1`.

## Những vấn đề đã quan sát

- Candidate texture ban đầu trong Blender mất đường dẫn tương đối nên render tím. Đã đóng gói ảnh vào blend, giữ PNG cạnh nguồn và kiểm tra đủ21 ảnh ở candidate cuối. Unity dùng PNG với native URP material, không dựa vào đường dẫn ảnh nhúng của FBX.
- Tán cây tiền cảnh dạng khối được thay bằng cành và leaf cards; giữ collider cũ. Gable Hội trường đã sửa từ mặt đặc sang kính và thanh đứng bên ngoài sau khi đối chiếu ảnh.
- Lượt graphical preview đầu bị trùng port7777 với smoke đang chạy, rồi fixture không hoàn tất. Chạy lại **tuần tự**, không sửa gameplay để né lỗi; lượt sau đạt70 kiểm tra và không còn lỗi bind/null trong log của lượt được nghiệm thu.
- Một lượt Editor capture trước đạt exit0. Lượt capture cuối ghi ảnh xong rồi crash tại native `TcpMessagingSession`/allocator trong shutdown. Chưa xác định nguyên nhân gốc; không khẳng định do GPU hoặc do model. Không tính ảnh ghi ra là PASS cho process. Windows build, EditMode và player graphical sau đó đều thoát thành công.

## Điều được chứng minh và điều chưa được chứng minh

Graphical player đi qua menu/Ready/Deploy/return, kiểm tra lightweight/FPS sampler, agent kích thước enemy đi7 chặng,3 camera probe, HUD nhiệm vụ/hoàn thành/wipe/reset. Fixture solo chỉ phục vụ kiểm tra, không đổi mặc định co-op2–4.

Smoke chạy direct localhost: bốn Player riêng có ownership/camera, đồng bộ movement/rotation/sprint/jump/mission và xử lý Host disconnect; người thứ5 và người vào sau Deploy bị từ chối. Không gọi đây là Relay Internet test.

So với revision4, renderer asset giảm263→235 nhưng triangle tăng258.948→389.298. Việc thêm khung cửa/lá và normal map là chi phí chất lượng; chưa đo draw call/GPU frame time trên máy mục tiêu. Chưa có LOD/streaming đầy đủ; F8/F9 vẫn cần cho kiểm tra thực tế. Không cam kết map mới hết lag chỉ dựa vào số renderer.

Ngoại thất bám đặc điểm của ảnh có sẵn; không có bản khảo sát hoặc ảnh mọi mặt nên không khẳng định tái hiện1:1. Các dãy ngoài ảnh dùng chi tiết kiến trúc ước lượng. Chưa bổ sung nội thất chơi được hoặc các hệ thống campaign khác.

Hướng dẫn: [CAN_THO_ARCHITECTURE_REALISM.md](CAN_THO_ARCHITECTURE_REALISM.md). Nguồn ảnh: [REFERENCES_REALISM.md](../../../ArtSource/References/CanThoCampus/REFERENCES_REALISM.md).
