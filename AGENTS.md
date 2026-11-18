# Hướng dẫn đóng góp cho kho mã nguồn

## Cấu trúc dự án và tổ chức mô-đun
NTGH là game nhập vai match-3 xây dựng bằng Unity. Mã nguồn chính nằm trong `Assets/Project/Scenes/Scripts/`, gồm `Home/` và `Story/`; một số thành phần scene nằm ngay trong `Assets/Project/Scenes/`. Các scene gồm `Intro`, `Home`, `Story` và `Game`. Đặt tài nguyên tái sử dụng trong `Assets/Project/Prefabs/`, `Sprites/` và `Textures/`; đặt công cụ Editor trong `Assets/Project/Editor/`. `Assets/MathThreeGame/` chứa tài nguyên match-3 cũ; `Thirdparties/`, `Demigiant/` và `GoogleMobileAds/` chứa các thư viện tích hợp. Các gói phụ thuộc và cấu hình Unity nằm trong `Packages/` và `ProjectSettings/`.

## Lệnh phát triển, kiểm thử và dựng bản phát hành
Dùng Unity **6000.6.1f1**, khớp với `ProjectSettings/ProjectVersion.txt`. Mở dự án bằng Unity Hub, mở `Assets/Project/Scenes/Intro.unity` rồi nhấn Play để chạy luồng khởi động.

Ví dụ PowerShell, với `$Unity` trỏ đến tệp thực thi Unity Editor:

```powershell
& $Unity -projectPath .
& $Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults TestResults.xml -logFile TestRun.log
```

Lệnh đầu mở dự án; lệnh sau chạy kiểm thử Edit Mode. Thay `EditMode` bằng `PlayMode` để kiểm thử khi game chạy. Dựng bản phát hành qua **File > Build Profiles**; kiểm tra nền tảng đích và danh sách scene với `ProjectSettings/EditorBuildSettings.asset`.

## Quy tắc viết mã và đặt tên
Tuân theo định dạng C# xung quanh; dùng bốn dấu cách để thụt lề mã mới. Dùng PascalCase cho lớp và phương thức, camelCase cho biến cục bộ, tiền tố `m_` theo quy ước hiện có cho trường được serialize. Tên tệp MonoBehaviour phải trùng tên lớp. Không đổi tên trường được serialize nếu chưa có cơ chế bảo toàn dữ liệu. Dùng TextMeshPro cho văn bản UI mới, lưu kết quả tìm component để tái sử dụng và dọn các tween DOTween khi đối tượng bị hủy. Dự án chưa có cấu hình định dạng mã thống nhất.

## Hướng dẫn kiểm thử
Dự án đã cài Unity Test Framework nhưng chưa có bộ kiểm thử gameplay hoặc ngưỡng độ bao phủ. `Assets/Test.cs` là MonoBehaviour phục vụ chẩn đoán. Thêm kiểm thử vào assembly riêng cho Edit Mode hoặc Play Mode; dùng tên như `GameRulesTests` và `Match_RemovesMatchedItems`. Chạy bằng Unity Test Runner. Kiểm tra thủ công các scene, cơ chế chiến đấu, điều hướng và bố cục UI bị ảnh hưởng.

## Quy tắc commit và pull request
Các commit gần đây dùng câu mệnh lệnh ngắn, chẳng hạn `Hide gold item text` và `Fix game layout`. Mỗi commit nên tập trung vào một thay đổi. PR cần mô tả hành vi thay đổi, issue liên quan, cách đã kiểm tra và ảnh chụp nếu thay đổi giao diện.

## Bảo toàn tài nguyên và chỉ dẫn cho agent
Khi tạo hoặc chỉnh sửa scene, screen hay popup, đọc và áp dụng skill [ntgh-ss-ui](C:/Users/letua/.codex/skills/ntgh-ss-ui/SKILL.md) để sử dụng framework `Assets/Thirdparties/SS` đúng cách.

Giữ tệp `.meta` đồng bộ với tài nguyên tương ứng. Không đưa bộ nhớ đệm sinh tự động, log hoặc thiết lập cục bộ vào thay đổi. Không đưa mật khẩu ký bản phát hành hoặc thông tin xác thực vào kho mã nguồn.

<!-- codebase-memory-mcp:start -->
Khi khám phá cấu trúc mã nguồn, hãy sử dụng skill `codebase-memory` đã cài đặt.
<!-- codebase-memory-mcp:end -->
