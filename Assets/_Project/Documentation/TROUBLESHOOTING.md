# Troubleshooting

| Triệu chứng | Kiểm tra |
|---|---|
| Không có menu LastProtocol | Chờ import/compile, kiểm tra Console và asmdef; đúng Editor 6000.5.4f1 |
| Missing package/type | Package Manager đã resolve manifest/lock chưa; không cài thêm standalone Relay |
| UGS request failed | Cloud link, quyền organization, environment, Authentication, Relay và Internet |
| Relay InactiveProject/Forbidden | Kiểm tra dịch vụ/quyền project trên Dashboard bằng tài khoản của bạn |
| Sai/expired code | Copy code phiên đang hoạt động; code phải 6 ký tự; Host cũ đã Leave thì tạo phiên mới |
| Room Full | Đã đủ 4 người hoặc Relay không còn capacity; đợi Client rời Lobby rồi thử lại |
| Room Closed | Host đã Start/đóng phòng; MVP không cho join giữa trận |
| Connecting timeout | Firewall/outbound DTLS, mạng, Relay, Host; StartClient=true chưa chứng minh Connected |
| Client về Menu | Host thoát, transport lỗi hoặc scene load timeout; đọc thông báo và log |
| Empty SampleScene khi build | MainMenu chưa là scene đầu của build; dùng menu Build Windows Development MVP |
| Gameplay scene load thất bại | Hai scene phải ở build list/profile, tên không đổi; EnableSceneManagement bật |
| Không spawn Player | Chỉ spawn sau LoadEventCompleted; kiểm tra spawn points và prefab registration |
| Client không di chuyển | Owner đúng, map Player/Move, cursor Locked, phase Playing; CharacterController client disabled là đúng |
| Camera sai người | Local camera chỉ Bind trong owner spawn; không thêm camera/network manager vào prefab |
| Leave khó bấm trong game | Nhấn Esc mở cursor rồi bấm Leave; nhấn Esc lần nữa để khóa |
| Multiple AudioListeners | Mỗi scene chỉ một camera vật lý; không gắn listener lên Player/CinemachineCamera |
| Setup không sửa prefab bị sai | Tool cố ý giữ asset sẵn có; sửa Inspector thủ công hoặc tạo bản sao trước khi thay |
| Create mới bị khóa | Chờ NGO shutdown; đừng gọi StartHost/StartClient từ script khác |

Logs kiểm chứng ở `Builds/ValidationResults`. Kiểm tra lỗi thuộc `_Project` riêng với lỗi AI Assistant/analytics/licensing của Editor. Không tắt hoặc thay đổi dịch vụ người dùng chỉ để che log.

Các giới hạn đã biết: không host migration, reconnect trong trận, prediction, account backend hoặc persistence. Không thể khẳng định Relay hoạt động chỉ từ build thành công. Xem TEST_RESULTS.md để biết case nào đã chạy.
