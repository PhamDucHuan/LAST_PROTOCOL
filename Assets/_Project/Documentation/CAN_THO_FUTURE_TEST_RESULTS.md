# Kết quả kiểm tra campus chi tiết và tương lai

Ngày 2026-10-15; Unity 6000.5.4f1, URP 17.5.0, Blender 5.2.2 LTS, art revision 2, protocol 9. Các phiên Editor tự động chạy trên bản sao `Builds/ValidationProject`, không thay scene đang mở của người dùng để thực hiện batch.

## Kết quả đã chạy

| Kiểm tra | Kết quả |
| --- | --- |
| Blender xuất FBX/source/sidecar | PASS; 7.128 object nguồn, 213 renderer gộp, 210.168 triangle, 226 collider và 28 mục landmark |
| C# và Windows Development build | PASS; Unity exit 0, build log xác nhận thành công |
| EditMode suite cuối | **82/82 PASS**; gồm các hệ thống sẵn có, map/collider/NavMesh, vật liệu URP, layout revision 2, idempotency và raycast mặt kính cong |
| NavMesh và spawn | PASS; giữ đường tới các cổng/landmark, không đi vào khối nhà hoặc chân tháp dữ liệu, bốn điểm spawn có nền và không nằm trong vật thể |
| Kính cong khu Học liệu | PASS; ray từ (57,1.5,-42) chạm collider ở khoảng 3–4 m, bao gồm phần mặt đứng nhô ra; NavMesh đã bake lại |
| Host + 3 Client localhost | **6/6 mục Summary PASS**: Host, ba Client, người thứ năm, Client thử vào phòng đã đóng |
| Chọn màn và network scene | PASS; lựa chọn Host đồng bộ, đổi map reset Ready, Client không tự chọn, bốn máy giả lập cùng vào CanThoCampus |
| Movement và combat regression | PASS trên bốn Player: di chuyển/xoay/sprint/jump, HP damage/downed/reset, mục tiêu nhận damage/down/reset, aim/cadence/downed fire và friendly fire |
| Host thoát | PASS; các Client nhận ngắt kết nối |
| Menu graphical harness | **35/35 PASS**, không có failure: chọn Cần Thơ, Ready/reset, điều kiện deploy, đúng scene, robot Agent ở trên NavMesh, về Menu không trùng manager, Join validation, lỗi, kiểm tra khung 16:9 và 4:3 |
| Hình ảnh | Đã xem ảnh Blender và năm góc Unity: overview, cổng B, Hội trường Rùa, khu Học liệu và ATL. Kiểm tra lại sau cân chỉnh ánh sáng/reflection. Ảnh cuối từ player cũng được lưu trong CanThoMenuPreview |
| Nội dung cũ | MainMenu, MultiplayerTest, ContainmentYard, SampleScene, NetworkPlayer prefab và OperationsMenu prefab giữ hash so với backup đầu lần nâng cấp này |

Lượt test cuối có 82 test, so với 79 ở map đầu tiên. Ba test thêm kiểm tra vật liệu/obstacle mới, công cụ nâng cấp chạy lại và lỗi collider của mặt kính cong. Không bỏ qua log lỗi để đổi kết quả thành PASS.

## Bằng chứng

- `Builds/CanThoDetail/20261015-182106-Final/`: nguồn Blender, model, sidecar, stats và ảnh Blender. Sidecar đã được sửa collider kính cong trong bước hoàn thiện.
- `Builds/ValidationResults/EditMode.xml`: 82/82.
- `Builds/ValidationResults/CanThoDetail.log`, `CanThoDetailRefresh.log`: thiết lập art/ánh sáng và cập nhật collider/NavMesh.
- `Builds/ValidationResults/CanThoBuild.log`: build cuối.
- `Builds/ValidationResults/LevelSmoke2/`: JSON/log từng tiến trình, Summary cuối 6/6.
- `Builds/ValidationResults/CanThoMenuPreview/`: Checks.json cuối 35/35, ảnh menu và ảnh gameplay.
- `Builds/ValidationResults/CanThoMapPreview/20261015-191741/`: năm ảnh Unity sau cân chỉnh ánh sáng. Bước sửa collider tiếp theo không đổi mesh hoặc thiết lập hình ảnh.
- `Builds/CanThoBackups/20261015-183036-BeforeDetail/`: bản map trước nâng cấp, các asset liên quan và ProtectedHashes.json.
- Các backup bổ sung trước đổi lighting/collider nằm trong `Builds/CanThoBackups` và backup phía validation.

Một lệnh điều phối PowerShell dùng giá trị `$LASTEXITCODE` còn lại đã báo lỗi sau khi script smoke hoàn tất 6/6 PASS. Kết quả đã được đối chiếu trực tiếp trong Summary JSON; kiểm tra menu sau đó được chạy riêng và hoàn tất exit 0, 35/35 PASS. Script smoke tự ném lỗi nếu bất kỳ mục nào fail; kết quả của lượt cuối không có mục fail.

## Giới hạn

**CHƯA KIỂM THỬ** bốn máy thật qua Internet/Relay, benchmark FPS/GPU/memory, toàn bộ góc camera/chi tiết collider, mọi tình huống AI melee quanh campus và hồi sinh giữ E trên mọi địa hình. Combat smoke cô lập robot để tránh làm nhiễu test máu; kiểm tra graphical chỉ xác nhận robot khởi tạo đúng trên NavMesh. Các chức năng combat/revive trước đó được giữ và có unit tests, không coi đó là bằng chứng đã chơi thử mọi tình huống ở môi trường mới.

Xe điện, bến xe, tháp dữ liệu và camera quét là môi trường tĩnh; chưa có traffic, NPC, nhiệm vụ/hack hoặc nội thất chơi được. Cubemap reflection là gradient procedural để đọc vật liệu tốt hơn, không mô phỏng phản xạ chính xác toàn campus. Ảnh Blender không được dùng thay bằng chứng Unity.

## Chạy lại

```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage CanThoBuild
.\Tools\Run-MultiplayerSmoke.ps1 -Player 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\CanThoPlayer\LAST_PROTOCOL.exe' -LevelTest -Level 2 -CombatTest
.\Tools\Run-MenuPreview.ps1 -Player 'D:\UNITY\DATN\LAST_PROTOCOL\Builds\ValidationProject\Builds\CanThoPlayer\LAST_PROTOCOL.exe' -Level 2
.\Tools\Run-CanThoCapture.ps1
```

Với các script PowerShell tự throw khi fail, dùng exception và báo cáo JSON để đánh giá; không đọc `$LASTEXITCODE` cũ như mã trả về của một hàm PowerShell. Mọi Host/Client cần nhận cùng cả thư mục build protocol 9.
