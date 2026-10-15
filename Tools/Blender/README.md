# Kiểm tra Blender → Unity

Đã chạy thực tế ngày 2026-10-15 trên LAST_PROTOCOL:

- Blender **5.2.2 LTS**, `D:\Blender\blender.exe`, chạy nền bằng Python thành công.
- Unity **6000.5.4f1**, URP **17.5.0**, nhập FBX thành công.
- Blender và Unity đều thoát với mã **0**; Unity probe đạt **16/16 kiểm tra**.
- Không cài thêm package, add-on, connector hoặc thay đổi cấu hình hệ thống.

Đây là quy trình tự động chạy script → xuất FBX → nhập Unity. Chưa thiết lập đồng bộ trực tiếp khi chỉnh sửa trong Blender, và chưa kiểm tra nhập thẳng file `.blend` vào Unity.

## File và phạm vi

- `ExportConnectionProbe.py`: tạo mới một scene Blender trong tiến trình chạy nền riêng; tạo sàn và ba khối thử có UV/material slot; lưu `.blend` và xuất FBX.
- `UnityConnectionProbe.cs`: mẫu kiểm tra Editor, chỉ được chép vào bản sao `Builds/ValidationProject/Assets/Editor`; không thêm code vào Assets của project chính.
- `Test-BlenderUnity.ps1`: điều phối hai chương trình, lưu kết quả vào thư mục có timestamp, từ chối chạy khi bản sao Unity đang bị khóa.
- Không mở/sửa các file Blender sẵn có. Không sửa scene, prefab, Assets hoặc ProjectSettings của project chính. Các asset nhập thử chỉ nằm trong bản sao validation.

## Chạy lại

Tại thư mục gốc project, chạy PowerShell:

```powershell
.\Tools\Blender\Test-BlenderUnity.ps1
```

Cần có bản sao `Builds/ValidationProject` đã được tạo bởi công cụ validation hiện có. Các đường dẫn Blender/Unity có thể truyền bằng tham số `-Blender` và `-UnityEditor`. Khi chạy trong Codex, môi trường giới hạn có thể cần quyền thực thi ngoài sandbox; lần kiểm tra này đã chạy thành công theo cách đó.

## Thiết lập xuất đã kiểm chứng

Blender: Metric, Unit Scale 1; apply object scale; FBX meshes only; Scale 1; Apply Unit; Apply Scalings = FBX Units Scale; Forward -Z; Up Y; không animation, không bake space transform.

Unity dùng ModelImporter mặc định về tỷ lệ/trục. Probe bật Read/Write trong bản sao chỉ để đọc UV kiểm tra. Không cần bật Read/Write cho mọi asset sản xuất.

Kết quả quan sát: cube 1 m → 1 Unity unit; khối kích thước Blender (1,2,3) → Unity (1,3,2); sàn (8,6,0.2) → (8,0.2,6), mặt sàn ở Y=0. Marker Blender (0,-2,0.2) → Unity (0,0.2,2). Tọa độ Blender X=2 → Unity X=-2 trong cấu hình đã thử; cần tuân thủ phép đổi hệ trục khi bố trí marker gameplay từ dữ liệu Blender.

## Bằng chứng

Kết quả lần kiểm tra đầu tiên:

`Builds/ValidationResults/BlenderConnection/20261015-145452-008/`

- `ConnectionProbe.blend`: nguồn Blender có thể chỉnh sửa.
- `ConnectionProbe.fbx`: FBX thực tế được Unity nhập.
- `blender-result.json`, `unity-result.json`: kết quả và kích thước đo.
- `blender.log`, `blender-errors.log`, `unity.log`: log chạy.

16 kiểm tra gồm nhận diện FBX, tạo model, đủ bốn mesh, vertex/UV và material slot cho từng mesh, tỷ lệ mét, trục đứng, kích thước sàn, cao độ đáy và raycast MeshCollider chạm sàn.

Chưa kiểm tra hình ảnh vật liệu URP/texture trong Game View, animation/rig, lightmap UV, NavMesh, multiplayer trên model này hoặc map hoàn chỉnh. Material slot được giữ không đồng nghĩa mọi shader Blender tự chuyển thành URP. Khi dựng map thật, vật liệu URP, collider và NavMesh sẽ được cấu hình/kiểm tra riêng trong Unity.

Unity log có thông báo dịch vụ Cloud/chứng chỉ và licensing ban đầu; phiên Editor sau đó nhập model, chạy probe và thoát 0. Chúng không ngăn quy trình FBX trong lần kiểm tra này.
