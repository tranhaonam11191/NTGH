# Cấu hình arc, màn chơi và hội thoại

Mở `Assets/Project/Scenes/Data/ArcsData.asset` trong Inspector để chỉnh thứ tự chiến dịch. Mỗi phần tử **Arcs** có tên và danh sách **Levels** riêng. Mỗi phần tử trong **Levels** tham chiếu một asset `LevelData`. Prefab `SOManager` giữ tham chiếu tới danh mục này.

## Thêm một màn

1. Trong cửa sổ Project, chọn **Create > NTGH > Level Data**. Đặt tên asset theo màn để dễ tìm lại trong danh mục. Điền **Display Name** cho tiêu đề hiển thị dưới số màn trong trận.
2. Gán **Default Player** và **Enemy** từ thư mục `Assets/Project/Scenes/Data/Character`. Mỗi nhân vật cần có `Character.health` lớn hơn 0. Trận đấu lấy avatar, HP tối đa và sức tấn công từ hai asset này.
3. Gán **Item Reward** nếu màn có vật phẩm thưởng. Popup thưởng dùng vật phẩm của màn vừa thắng, kể cả khi tiến trình đã sang arc tiếp theo.
4. Trong **Conversations**, thêm một phần tử cho mỗi nhóm thoại. Chọn **Trigger**, kéo thanh **Health Threshold** cho nhóm HP hoặc nhập **Delay Seconds** cho nhóm thời gian. Thêm câu vào **Lines** theo thứ tự đọc. Mỗi câu có **Content**, **Img Character** và **Is Player** để chọn kiểu hiển thị phía người chơi. Có thể để trống ảnh cho lời dẫn.
5. Trong `ArcsData.asset`, mở arc đích rồi kéo asset vừa tạo vào **Levels** tại vị trí muốn chơi. Để thêm arc, thêm một phần tử **Arcs**, điền **Name** và gán các màn. Muốn tạo danh mục riêng, chọn **Create > NTGH > Arcs Data** rồi gán vào **Arcs Data** trên prefab `SOManager`.

## Thời điểm phát thoại

| Trigger | Điều kiện |
| --- | --- |
| Start | Khi hai nhân vật đã được thiết lập và trước khi người chơi thao tác. |
| Player Health | HP người chơi giảm từ trên ngưỡng xuống bằng hoặc dưới ngưỡng. |
| Enemy Health | Cùng quy tắc, áp dụng cho HP của đối thủ. |
| End | Khi một bên hết HP, trước popup thắng hoặc thua. |
| Time After First Click | Khi đủ số giây chơi đã đặt trong **Delay Seconds**, tính từ lần nhấn chuột trái hoặc chạm đầu tiên trong lúc trận cho phép thao tác. |

**Health Threshold** là tỷ lệ HP còn lại của nhân vật được chọn. Ví dụ, đặt **Enemy Health** ở `0.5` cho địch có 120 HP sẽ phát thoại khi HP giảm từ 65 xuống 60 hoặc thấp hơn. Ngưỡng `1` kích hoạt ở lần mất máu đầu tiên từ đầy HP. Ngưỡng `0` kích hoạt khi HP về 0. Mỗi nhóm chỉ phát một lần trong trận, kể cả khi nhân vật hồi máu rồi vượt ngưỡng lần nữa.

**Delay Seconds** dùng chung một mốc cho mọi nhóm thời gian của màn. Đặt `0` để phát ngay lúc click đầu tiên, hoặc `10` để phát sau 10 giây chơi. Các lần chạm đọc thoại mở đầu được bỏ qua. Khi đang đọc thoại, đồng hồ tạm dừng và tiếp tục khi trở lại trận. Retry đặt lại đồng hồ và trạng thái đã phát của mọi nhóm.

Nếu một đòn vượt nhiều ngưỡng, các nhóm phù hợp phát theo thứ tự trong Inspector. Với đòn kết liễu, nhóm HP phát trước nhóm End. Nhóm có **Lines** rỗng được bỏ qua. Trong lúc đọc, trận đấu tạm dừng. Chạm khi chữ đang chạy để hiện hết câu, chạm tiếp để đọc câu sau. Câu cuối tự đóng sau một giây.

## Tiến trình và dữ liệu hiện có

Home, Next và Retry đều gọi `SOManager.PlayCurrentLevel()`. Manager lấy màn hiện tại rồi truyền chính asset đó vào `GameController.Setup(level)` qua callback tải scene của SS. Game mở Story dạng additive và tiếp tục cùng trận sau khi đóng thoại.

Một chiến thắng ghi ngay vị trí kế tiếp vào khóa PlayerPrefs `Num_Story`, bắt đầu từ 0. Hết màn cuối trong arc, game sang màn đầu của arc kế. Thắng màn cuối danh mục sẽ ghi 0. Thua giữ vị trí để Retry hoặc trở về Home chơi lại. Các nút kết quả và quảng cáo không tăng màn lần thứ hai.

Dữ liệu đã chuyển gồm 5 arc, theo thứ tự Quạ Đen → Diệu Dương → Kim Ma → Hoành Mi → Cầm Long. Bốn arc đầu có một màn mỗi arc. Arc Cầm Long có hai màn là `ArcCamLong` và `Final`. Lời thoại có sẵn nằm trong nhóm Start của từng màn. Có thể tách các câu thành nhiều nhóm trong Inspector.

Vị trí lưu là chỉ số trong danh sách màn ghép theo thứ tự arc. Arc rỗng và ô level chưa gán được bỏ qua. Giá trị lưu cũ vượt số màn được quy về phần dư, giá trị âm bắt đầu ở 0. Khi sắp xếp lại danh mục, cùng một chỉ số lưu có thể trỏ đến màn khác.

## Kiểm tra trong Play Mode

- Mở scene Main, vào Home và nhấn Play. Đối chiếu hai avatar và các câu mở đầu với asset của màn.
- Trên bản sao một màn, tạo hai nhóm Enemy Health ở `0.51` và `0.5` cùng một nhóm End. Dùng một đòn từ trên 51% xuống dưới 50% để kiểm tra thứ tự. Làm tương tự với Player Health, sau đó hồi máu và vượt ngưỡng lần hai.
- Tạo hai nhóm Time After First Click ở 0 và 3 giây. Đợi hơn 3 giây trước khi chạm để kiểm tra mốc bắt đầu. Chạm để mở nhóm đầu, đọc hơn 3 giây rồi đóng. Nhóm thứ hai phải xuất hiện sau 3 giây chơi tiếp. Retry và lặp lại.
- Thua rồi chọn Retry. Kiểm tra cùng nhân vật và nhóm Start phát lại. Thua rồi về Home và vào lại để kiểm tra vị trí lưu.
- Thắng liên tiếp qua ranh giới hai arc, qua hai màn trong Cầm Long và qua Final. Kiểm tra Next sau Final mở Quạ Đen. Thử về Home hoặc khởi động lại sau một chiến thắng để kiểm tra màn đã lưu.
