# Unity Editor Setup

## Bắt đầu
1. Đợi Package Manager resolve và Unity compile xong. Xóa filter Console, sửa lỗi trước khi chạy tool. Nếu đang có scene untitled, hãy tự lưu scene trước; tool không tự lưu hoặc thay scene chưa lưu.
2. Chọn `LastProtocol > Multiplayer > Generate MVP Assets (preserve existing)`.
3. Chọn `Validate MVP Assets`.
4. Chọn `Add MVP Scenes to Build List` nếu chúng chưa được bật trong build list/profile đang dùng.
5. Mở `_Project/Scenes/MainMenu/MainMenu.unity` và Play. **Không bắt đầu từ SampleScene hoặc gameplay scene.**
6. Cấu hình Cloud thủ công theo UNITY_RELAY_SETUP.md.

## Tool tạo gì?
- Cấu trúc `_Project`, MultiplayerConfig, font TMP, material Player/Ground.
- NetworkPlayer.prefab và NetworkRoomState.prefab.
- MainMenu: NetworkManager + UnityTransport + MultiplayerSession, đăng ký hai network prefab, UI/menu camera.
- MultiplayerTest: Ground, bốn spawn point, Directional Light, local Cinemachine camera và HUD.

Tool tạo scene mới theo chế độ Additive rồi đóng; không save/đổi scene hiện tại của người dùng. Không sửa SampleScene. Nếu asset mục tiêu đã tồn tại, giữ nguyên và validate; asset sai kiểu sẽ báo lỗi, không tự thay. Generate không sửa build list. Add MVP Scenes là lệnh riêng, chỉ thêm/enable scene, giữ các scene và thứ tự cũ.

## Build Windows
Menu `Build Windows Development MVP` build **MainMenu trước, MultiplayerTest sau** vào `Builds/SmokePlayer`. Hoặc Build Profiles > Windows: bật hai scene và đặt MainMenu đầu danh sách profile bạn chọn; giữ SampleScene nếu cần ở profile khác. Đừng build với SampleScene làm scene đầu rồi kỳ vọng thấy multiplayer UI.

NetworkManager Inspector phải có UnityTransport, ConnectionApproval, EnableSceneManagement, tick rate 30; PlayerPrefab và Prefab list đúng. Session phải có Config/Room/Player prefab. Không đổi NetworkTransform thành owner authority.

Danh sách prefab phải là asset serialized `_Project/Settings/MvpNetworkPrefabs.asset`, chứa cả Player và Room. `NetworkConfig.Prefabs.Add` là đăng ký runtime, không đủ để lưu scene trong Editor. Nếu validation báo thiếu danh sách, dùng `Repair MVP Prefab Registration`; tool giữ các danh sách hợp lệ đã có, thêm danh sách MVP. Nếu scene đang mở, tool chỉ đánh dấu dirty để bạn tự lưu.

## Kiểm chứng tự động trên bản sao
Từ PowerShell tại root:
```powershell
.\Tools\Run-MultiplayerValidation.ps1 -Stage Setup
.\Tools\Run-MultiplayerValidation.ps1 -Stage Tests
.\Tools\Run-MultiplayerValidation.ps1 -Stage Build
.\Tools\Run-MultiplayerSmoke.ps1
.\Tools\Run-MultiplayerLifecycle.ps1
```
Script copy Assets/Packages/ProjectSettings vào `Builds/ValidationProject` (không xóa dữ liệu), mở Unity batch ẩn, ghi log/XML ở `Builds/ValidationResults`. Không chạy đồng thời các stage trên cùng validation project. Không đóng Editor người dùng. Source cache/build có thể tốn dung lượng trong Builds; đây là dữ liệu kiểm chứng được gitignore, không phải source game.

Kết quả compile/asset/test từ bản sao chỉ áp dụng cho đúng source/package được copy ở lần chạy đó. Internet testing vẫn cần các máy và dịch vụ Cloud.
