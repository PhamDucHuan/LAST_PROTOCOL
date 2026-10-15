# Third-Person Controller — giai đoạn sau Multiplayer MVP

Ngày triển khai: 2026-10-15. Dùng Capsule, Input System và Cinemachine hiện có; không cài package hoặc thay scene/prefab. Chưa thêm model/animation, combat, AI hay stamina.

## Điều khiển
- WASD: đi theo hướng camera.
- Giữ Left Shift: chạy nhanh. Space: nhảy một lần mỗi lần nhấn.
- Chuột: xoay camera cục bộ. Esc: mở/khóa con trỏ.
- Khi Alt+Tab hoặc đổi cửa sổ: mở con trỏ, không đọc input gameplay ở cửa sổ mất focus. Khi quay lại, tự khóa chuột và khôi phục điều khiển nếu trước đó đang điều khiển nhân vật. Nếu đã chủ động nhấn Esc để mở chuột, trạng thái này được giữ; nhấn Esc hoặc click vùng gameplay ngoài UI để tiếp tục. Bỏ delta chuột ở frame khóa lại để tránh camera giật.
- Gamepad có action Sprint/Jump sẵn trong asset input, nhưng camera/gamepad và trải nghiệm gamepad chưa được kiểm thử/hoàn thiện; ưu tiên Windows keyboard/mouse.

## Authority và mạng
Owner gửi hướng di chuyển, trạng thái chạy, bộ đếm nhấn nhảy và số thứ tự gói ở 30 Hz. Jump edge được đọc **mỗi frame**, không bỏ lỡ thao tác giữa hai lần gửi. Bộ đếm lặp trong các gói unreliable; Host chỉ nhận sự kiện mới, tránh một lần nhấn kích hoạt lại khi lặp gói. Nếu nhiều lần nhấn xảy ra trước một snapshot, chúng được gộp thành tối đa một yêu cầu.

Host tiếp tục kiểm tra sender/sequence/rate/finite input, clamp vector, quyết định tốc độ từ config và mô phỏng trong FixedUpdate. Client không gửi tốc độ hoặc vị trí cuối cùng. Host chỉ nhận nhảy khi grounded và hết cooldown; yêu cầu nhảy trên không bị tiêu thụ ngay, không xếp hàng để tự nhảy sau khi đáp đất. Input quá 0.25 giây không cập nhật thì ngừng đi/chạy/nhảy, nhưng trọng lực vẫn được tính trên Host.

CharacterController.Move không tự áp dụng gravity; controller tự tích phân gravity, clamp tốc độ rơi và dừng vận tốc đi lên khi va trần. Grounded lấy từ kết quả Move trước đó. Tham khảo [Unity CharacterController.Move](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController.Move.html) và [isGrounded](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/CharacterController-isGrounded.html).

## Config Inspector
`Assets/_Project/Settings/MultiplayerConfig.asset`:
| Trường | Mặc định |
|---|---:|
| movementSpeed | 5 m/s |
| sprintSpeed | 8 m/s |
| jumpHeight | 1.4 m |
| gravityMultiplier | 2 |
| terminalFallSpeed | 30 m/s |
| jumpCooldownSeconds | 0.15 s |

Sprint speed phải >= walk speed; các giá trị số phải hữu hạn. `PlayerMovement` cung cấp IsGrounded/PlanarSpeed/VerticalSpeed cho bước animation phía server sau này; chưa đồng bộ Animator/state animation.

**Protocol nâng từ 1 lên 2 vì RPC input thay đổi. Phải build lại và phát cùng bản cho tất cả máy.** Build cũ bị từ chối do protocol không tương thích. Không thay đổi MaxPlayers/Relay/Cloud settings.

## Kiểm thử
- EditMode physics: đi ở nhiều timestep, sprint/input oversized, grounded jump/no air jump, đáp đất, va trần, config không hợp lệ.
- Smoke 4 tiến trình: mỗi owner đi/chạy, nhảy rồi gửi thêm một yêu cầu giữa không trung; mọi máy phải quan sát đủ bốn người di chuyển/quay/chạy/nhảy, không vượt độ cao jump hợp lệ.
- `Tools/Run-MultiplayerValidation.ps1 -Stage Tests`, tiếp theo `-Stage ControllerBuild`, tiếp theo `Tools/Run-MultiplayerSmoke.ps1` từ root project. Không chạy hai Unity batch trên cùng validation project đồng thời.
- Build trong Editor: `LastProtocol > Player > Build Windows Third-Person Demo` ra `Builds/ThirdPersonPlayer`; bản build kiểm chứng tự động ở `Builds/ValidationProject/Builds/ThirdPersonPlayer`. Giữ bản MVP cũ trong SmokePlayer để đối chiếu; hai bản protocol khác nhau không join chung.
- Harness phát input tự động khi headless để kiểm tra network; không chứng minh phím Shift/Space, focus/chuột hay cảm giác điều khiển thực tế.

Kết quả chạy thực tế được ghi trong `THIRD_PERSON_TEST_RESULTS.md`; không suy luận PASS từ code hoặc từ lần kiểm thử multiplayer cũ. Người dùng báo kết nối mạng hiện đã ổn; chưa suy rộng thành xác nhận đầy đủ bốn PC khác mạng.
