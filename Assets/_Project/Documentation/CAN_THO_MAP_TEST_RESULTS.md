# Kết quả kiểm tra map Cần Thơ

Ngày kiểm tra: 2026-10-15. Unity 6000.5.4f1, URP 17.5.0; Blender 5.2.2 LTS. Các phiên Unity tự động chạy trên `Builds/ValidationProject`; không dùng project chính để chạy batch hoặc thay scene đang mở của người dùng.

## Đã thực hiện

| Kiểm tra | Kết quả và phạm vi |
| --- | --- |
| Blender tạo nguồn, FBX và layout | PASS; 6.028 object nguồn, 179 renderer xuất, 136.076 triangle, 28 mục công trình/cụm nhà |
| Unity compile và Windows Development build | PASS; `Builds/ValidationProject/Builds/CanThoPlayer/LAST_PROTOCOL.exe` |
| EditMode suite | **79/79 PASS**; gồm các bộ test hệ thống trước và ba test riêng của campus |
| NavMesh map | PASS; từ vùng robot tới ba cổng, Hội trường Rùa, khu Học liệu, CNTT, RLC và khu ký túc xá; không có NavMesh trong thân Nhà điều hành/RLC |
| Scene và collider | PASS; bốn điểm spawn có nền và không nằm trong khối đặc, nhà điều hành chặn raycast, không thêm NetworkManager, không thiếu script |
| Installer chạy lại | PASS; giữ nguyên scene và asset đã tạo, giữ active scene có tên, không tạo trùng; fixture batch mở scene có tên trước khi chạy |
| Host + 3 Client trên localhost | PASS; cùng tải CanThoCampus, đủ 4 Player, owner input/server authority/camera, di chuyển, xoay, sprint, jump |
| Host đổi map trong lobby | PASS; Client thấy lựa chọn thay đổi, Ready được reset, Client không tự chọn map được |
| Người thứ năm và phòng đã đóng | PASS; Room Full / Room Closed |
| Máu và bắn trong map | PASS; bốn nhân vật nhận damage/downed/reset; mục tiêu nhận damage/down/reset; cadence, aim, downed fire, friendly fire được kiểm tra bằng fixture |
| Host đóng phiên | PASS; Client nhận disconnect và chuyển trạng thái Error/menu |
| Menu và deploy có đồ họa | **35/35 kiểm tra PASS**; chọn hàng Cần Thơ, Ready/reset, điều kiện tối thiểu, deploy đúng scene, robot khởi tạo Agent trên NavMesh, quay về menu, Join validation và lỗi |
| Hình ảnh | Đã xem ba góc Blender và ba góc Unity URP; sửa giao lộ chồng mặt và hướng mặt mái, chụp lại xác nhận mái hiện đầy đủ |
| Bảo toàn nội dung có sẵn | Hash của MultiplayerTest, ContainmentYard, SampleScene, NetworkPlayer prefab và OperationsMenu prefab giữ nguyên so với trước tích hợp; xem ghi chú MainMenu bên dưới |

Ảnh menu mới cũng đã được xem: ba hàng lựa chọn, briefing và nút Create Room nằm trong khung 1600×900, chữ đầy đủ ở ảnh lựa chọn Cần Thơ. Kiểm tra 4:3 trong harness không phát hiện text overflow hoặc phần tử ra ngoài màn hình; đây không thay thế thử chuột/bàn phím trực tiếp.

MainMenu có hash giữ nguyên tại các lần kiểm tra trước 15:59. Ở đối chiếu cuối, file chính có thời gian lưu 15:59:27 và khác đúng một dòng `ServerListenAddress`: trống → `127.0.0.1`. Pipeline tạo map không ghi file scene này trong project chính; thay đổi đang có được giữ nguyên, không hoàn nguyên. Bản build và bản validation dùng nội dung MainMenu trước thay đổi đó. Không coi MainMenu trong project chính hiện tại giống byte-for-byte với baseline.

## Giới hạn của bằng chứng

Loopback dùng Unity Transport trực tiếp, không gọi Authentication/Relay. Combat fixture tạm dừng/không spawn robot để không làm nhiễu kiểm tra máu; vì vậy không suy ra toàn bộ AI melee đã được regression test trong campus. Các bộ test trước vẫn chạy trong 79 EditMode cases, nhưng không thay thế thử mọi tình huống gameplay trong map mới.

**CHƯA KIỂM THỬ** trên bản map này: bốn máy thật qua Internet/Relay, mất mạng thật, toàn bộ chuỗi Enemy chase/melee quanh từng công trình, hồi sinh giữ E ở mọi địa hình, Alt-Tab bằng thao tác người thật, FPS/GPU/memory benchmark, toàn bộ collider của chi tiết trang trí, gamepad và mọi góc camera. Chưa có interior, nhiệm vụ, boss hoặc AI Core tương tác để kiểm thử.

## File bằng chứng và lệnh

- Art cuối: `Builds/CanThoMap/20261015-151147-Release/` chứa `.blend`, FBX, layout, BuildStats, ảnh và log Blender.
- `Builds/ValidationResults/EditMode.xml`: kết quả 79 test sau sửa tham chiếu model.
- `Builds/ValidationResults/CanThoBuild.log`: build cuối với mái đã sửa.
- `Builds/ValidationResults/LevelSmoke2/`: JSON/log từng tiến trình và Summary.
- `Builds/ValidationResults/CanThoMenuPreview/`: Checks.json và ảnh thao tác UI/ảnh game từ harness.
- `Builds/ValidationResults/CanThoMapPreview/20261015-155642/`: ba ảnh Unity cuối sau sửa hướng mặt; overview tạm tắt fog để kiểm tra toàn map, không thay đổi scene đã lưu.
- `Builds/CanThoBackups/20261015-152213-SourceIntegration/`: config/build settings trước đổi tham chiếu menu và ProtectedHashes.json.
- Snapshot bằng chứng cuối: `Builds/ValidationResults/RegressionRuns/20261015-CanTho-final/`.

Runtime DLL trước và sau lần sửa art cuối có SHA256 giống nhau: `D549D1745007ED92B0325F451769EF7D93827BE110856EDB0136B31615CB48A4`. Sau sửa art cũng chạy lại loopback trên chính binary cuối; compile/build và hình ảnh được kiểm tra lại.

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage CanThoBuild
.\Tools\Run-MultiplayerSmoke.ps1 -Player 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\CanThoPlayer\LAST_PROTOCOL.exe' -LevelTest -Level 2 -CombatTest
.\Tools\Run-MenuPreview.ps1 -Player 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\CanThoPlayer\LAST_PROTOCOL.exe' -Level 2
.\Tools\Run-CanThoCapture.ps1
```

Các vấn đề đã phát hiện trong quá trình làm và sửa: gộp mesh dùng chung bằng operator Blender không ổn định (đổi sang tạo mesh trực tiếp), scene batch chưa đặt tên cản tạo prefab (chuẩn bị scene có tên trong batch), GUID FBX do Editor chính tự import khác bản validation (đồng bộ GUID và remap, giữ `.meta`), và mặt mái quay vào trong (sửa face winding ở nguồn). Không che lỗi bằng việc tắt kiểm tra hoặc bỏ qua log test.
