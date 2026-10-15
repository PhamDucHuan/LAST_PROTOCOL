# Player Health — kết quả kiểm chứng

Ngày: 2026-10-15. Unity 6000.5.4f1, NGO 2.13.3, protocol 3.

## Kết quả thực chạy

| Kiểm tra | Kết quả | Bằng chứng |
|---|---|---|
| Health prefab migration trên bản sao | PASS; không lỗi/warning C# | HealthSetup.log |
| EditMode suite | PASS: 33/33, failed=0, skipped=0, Unity exit=0 | EditMode.xml / Tests.log |
| HP 100 -> 75 -> 0/Downed | PASS unit | PlayerHealthTests |
| Damage/heal <= 0, overkill/int overflow | PASS unit | PlayerHealthTests |
| Heal không hồi sinh nhân vật gục | PASS unit | PlayerHealthTests |
| Không ghi được khi chưa spawn | PASS unit | PlayerHealthTests |
| Installer chạy lại không đổi prefab/scene | PASS unit | PlayerHealthTests |
| Controller/setup/connection regression | PASS trong suite 33 tests | PlayerMovementTests / SetupToolTests / ConnectionPolicyTests |
| Windows Development Health build | PASS, Unity exit=0, không error CS/warning CS | HealthBuild.log |
| 4 tiến trình: HP 100 -> 75 -> 0 -> reset 100 | PASS trên cả bốn tiến trình | HealthSmoke/Summary.json |
| Client gọi API damage/heal/reset | PASS: cả ba Client bị từ chối | clientHealthWritesRejected=true |
| Gửi movement khi gục | PASS: không di chuyển trên mọi tiến trình | movedWhileDowned=false |
| Điều khiển sau debug reset | PASS: mọi tiến trình thấy đủ bốn Player di chuyển lại | recoveredMovingPlayers=4 |
| Network movement/sprint/jump/ownership/camera binding | PASS trên cả bốn tiến trình | HealthSmoke/Summary.json |
| Room Full / Room Closed | PASS | fifth.json / closed.json |
| Host thoát | PASS: ba Client xử lý disconnect về Menu/Error | sawHostDisconnect=true |

Smoke suite exit=0; sáu mục host/client1/client2/client3/fifth/closed đều Passed=true. Mỗi tiến trình gameplay quan sát initializedHealthPlayers=4, damagedHealthPlayers=4, downedHealthPlayers=4, resetHealthPlayers=4, recoveredMovingPlayers=4. Toàn bộ source C# trong project khớp bản sao đã build/kiểm thử (0 file khác biệt). Đây là loopback, không thay thế kiểm thử Relay trên nhiều PC hoặc kiểm tra giao diện bằng mắt.

Prefab nguồn đã được backup trước khi tích hợp: `Builds/HealthBackups/20261015-093214-108-BeforeIntegration/NetworkPlayer.prefab`, giữ nguyên .meta/GUID. Chỉ thêm PlayerHealth và PlayerHealthView vào prefab hiện có; không đổi các scene. Config thêm playerMaxHealth=100; protocol 2 -> 3 do thêm NetworkBehaviour vào prefab.

## Giới hạn và kiểm thử thủ công còn lại

- Health trên nhiều PC qua Relay/Internet: **CHƯA KIỂM THỬ** trong module mới.
- Hình thức/vị trí thanh máu, font, billboard, click ba nút Host, Esc/Alt+Tab sau khi gục: **CHƯA KIỂM THỬ** bằng tương tác trực tiếp.
- Release build loại bỏ panel/reset debug: đã giới hạn bằng điều kiện biên dịch, **CHƯA BUILD RELEASE**.
- Không có combat, nguồn damage tự động, revive đồng đội, respawn hoặc game-over. Debug reset chỉ phục vụ kiểm thử.

Hướng dẫn setup, Inspector, Host/Client và kiểm thử thủ công: PLAYER_HEALTH_SYSTEM.md. Không dùng lại binary ThirdPersonPlayer protocol 2; mở HealthPlayer protocol 3 trên mọi máy và tạo phòng mới.

## File tạo/chỉnh sửa

- Tạo: HealthState.cs, PlayerHealth.cs, PlayerHealthView.cs, PlayerHealthSetupTool.cs, PlayerHealthTests.cs và .meta tương ứng; PLAYER_HEALTH_SYSTEM.md và báo cáo này.
- Chỉnh sửa: MultiplayerConfig.cs và MultiplayerConfig.asset (MaxHealth, protocol), NetworkPlayerController.cs (chặn điều khiển khi gục), NetworkPlayer.prefab (thêm Health/View/font), MultiplayerSetupTool.cs (fresh prefab/menu Health build), MultiplayerSmokeHarness.cs (kịch bản Health opt-in), Tools/Run-MultiplayerValidation.ps1 và Tools/Run-MultiplayerSmoke.ps1.
- Tài liệu cập nhật: MULTIPLAYER_ARCHITECTURE.md, MULTIPLAYER_TEST_CASES.md. Không đổi Scene, ProjectSettings hoặc Packages.
