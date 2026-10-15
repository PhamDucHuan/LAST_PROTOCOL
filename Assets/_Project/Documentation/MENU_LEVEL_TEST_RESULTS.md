# Kết quả menu và chọn 2 màn

Ngày 2026-10-15. Unity 6000.5.4f1 / NGO 2.13.3 / protocol 7.

## Đã kiểm tra

- EditMode: **76/76 PASS**, log `Builds/ValidationResults/Tests.log`, XML `Builds/ValidationResults/EditMode.xml`.
- Training Grounds: **6/6 summary PASS**, Host + 3 Client. Bằng chứng `Builds/ValidationResults/LevelSmoke0/Summary.json`.
- Containment Yard: **6/6 summary PASS**, Host + 3 Client. Bằng chứng `Builds/ValidationResults/LevelSmoke1/Summary.json`.
- Cả hai bộ đều xác nhận: đổi sang màn khác rồi quay lại, đồng bộ SelectedLevel, reset Ready, Client không có quyền đổi màn, tất cả vào đúng scene, 4 Player điều khiển đúng Owner, di chuyển/nhảy/sprint, HP/damage/downed/reset, combat, friendly-fire OFF, Room Full, Room Closed và Host disconnect.
- NavMesh Yard: spawn Enemy hợp lệ, đường đi từ spawn tới Enemy hoàn chỉnh và không đặt NavMesh trong vật chắn. Preview đã vào Yard bằng nút Deploy và xác nhận NavMeshAgent trên Host hoạt động, rồi trở về menu không nhân đôi manager.
- Installer: giữ nguyên byte của MainMenu, MultiplayerTest và SampleScene; chạy lại không ghi lại prefab/scene/config đã đúng, không đổi thứ tự build list cũ.

## Preview giao diện

`Tools/Run-MenuPreview.ps1` thử các Button.onClick thực, normalize/validate Join Code, Ready, đổi màn, Back, Error và trở về menu. Gọi onClick tự động không thay thế thao tác chuột vật lý.

Preview chụp Home, chọn Training/Yard, lobby, lobby sau đổi màn, Join, Error, Controls, Home 4:3 và Yard gameplay. Render Canvas sao chép từ trạng thái UI thật ra RenderTexture để tránh swapchain đen của cửa sổ ẩn. Không dùng HTML/mockup hoặc vẽ lại ảnh thay cho UI Unity.

Ảnh và báo cáo: `Builds/ValidationResults/MenuPreview/`. Bộ kiểm tra tự động đạt **35/35 assertions**, không báo text overflow hoặc rect ngoài viewport; chế độ FreshCapture đạt **9/9 trang** về tạo ảnh/cấu hình. Đã quan sát Home, Join, Controls, lựa chọn màn và lobby ở các lượt render. Một số lượt offscreen vẫn có batch chữ cố định bị thiếu dù dữ liệu text/bounds đúng; không coi các assertion này là bằng chứng mọi glyph đều hiển thị hoàn hảo trong Editor. Cần xác nhận trực quan ở cửa sổ Unity/Game bình thường. Các lần thử có ảnh đen/thiếu batch được lưu trong History; không dùng làm bằng chứng visual PASS.

## Giới hạn / CHƯA KIỂM THỬ

- Relay / Internet trên 4 PC với bản protocol 7.
- Click chuột, nhập bàn phím/gamepad thật, điều hướng và focus/Alt-Tab trong menu mới.
- Kiểm thử riêng toàn bộ AI chase/melee/revive qua các vật chắn mới trên nhiều máy. Đã kiểm tra khởi tạo Agent, NavMesh và combat/cover nền; chưa coi đó là full AI regression cho bố cục Yard.
- Nhiệm vụ, campaign, lưu tiến độ/mở khóa màn: chưa triển khai, cả hai là sân test chơi được.

Config project vẫn minimumPlayersToStart=2. Chỉ fixture preview đặt 1 người trong bộ nhớ tiến trình kiểm tra rồi khôi phục về 2; không chỉnh asset thành solo.
## Build và snapshot cuối

Windows MenuPlayer build PASS (exit 0), không có C# diagnostics mới trong MenuBuild.log. SHA256 LastProtocol.Runtime.dll: E7F743EAB9A0279ADB8DD4CC0FF41584AE6253E37408132A54FD6C0672B244B2.

Snapshot: Builds/ValidationResults/RegressionRuns/20261015-Menu-final. Các lần build sau test mạng chỉ thay công cụ capture Development và copy footer/menu; logic chọn màn, SceneManager, movement, Health và combat giữ nguyên.
