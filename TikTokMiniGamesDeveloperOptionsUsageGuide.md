# Hướng dẫn sử dụng Tùy chọn Nhà phát triển (Developer Options) cho TikTok Mini Games

Sử dụng Tùy chọn Nhà phát triển (Developer Options) khi bạn cần kiểm tra môi trường chạy (runtime environment) bên trong TikTok Mini Game, xem thông tin gỡ lỗi (debugging) cơ bản, mở vConsole, hoặc cung cấp bằng chứng cho một vấn đề liên quan đến xem trước (preview), đánh giá (review), hoặc môi trường sản xuất (production).

Nó hữu ích cho việc tự kiểm tra của nhà phát triển, khắc phục sự cố đánh giá, tái tạo lỗi của đội ngũ Oncall và thu thập bằng chứng trên môi trường sản xuất. Nó không thay thế cho việc gỡ lỗi từ xa bằng DevTool, và cũng không thay thế các hướng dẫn tích hợp chuyên dụng cho tính năng đăng nhập, ủy quyền, IAP (thanh toán trong ứng dụng), IAA (quảng cáo trong ứng dụng), chia sẻ, hoặc các khả năng nền tảng khác.

Lưu ý: Tùy chọn Nhà phát triển hiển thị trạng thái lúc chạy của mini game hiện tại trong Ứng dụng TikTok, thiết bị, và môi trường mạng hiện tại. Khi báo cáo một sự cố, hãy bao gồm Client Key, Phiên bản SDK (SDK Version), Thông tin hệ thống (System Info), Lỗi (Error), ảnh chụp màn hình chính và nhật ký (log) của vConsole khi cần thiết.

## I. Quy trình sử dụng
Quy trình cốt lõi là: mở phiên bản mini game mục tiêu, truy cập Tùy chọn Nhà phát triển từ menu xem thêm (more menu), bật Bảng DevInfo (DevInfo Panel) hoặc vConsole khi cần, và sử dụng các trường thông tin, nhật ký và các yêu cầu mạng để xác định vị trí sự cố.

![Quy trình sử dụng](./Image/TikTokMiniGamesDeveloperOptionsUsageGuide/image1.png)

| Mô-đun (Module) | Mục đích sử dụng (What it is for) | Ranh giới (Boundary) |
|---|---|---|
| DevInfo Panel (Bảng DevInfo) | Hiển thị Client Key, FPS, Phiên bản SDK, Thông tin hệ thống, Lỗi, và trạng thái các công tắc gỡ lỗi. | Hữu ích cho việc kiểm tra nhanh môi trường và tóm tắt lỗi, nhưng không dùng để xem nhật ký (log) hoàn chỉnh. |
| vConsole | Hiển thị nhật ký lúc chạy (Log, Info, Warn, Error) và kết quả của Mạng (Network). | Tốt cho việc kiểm tra nhanh. Sử dụng DevTool để gỡ lỗi từ xa đối với các vấn đề sâu hơn. |
| Disable Domain Verification (Tắt Xác minh Tên miền) | Giúp xác định xem việc gửi yêu cầu thất bại có liên quan đến xác minh tên miền của nền tảng hay không. | Đây chỉ là một công tắc gỡ lỗi. Nó không có nghĩa là tên miền dịch vụ đã được cấu hình đúng. |
| IAA / IAP Mock (Mô phỏng IAA / IAP) | Cho biết quảng cáo hoặc thanh toán có đang sử dụng luồng mô phỏng (mock flows) hay không. | Kết quả mô phỏng không đại diện cho quảng cáo thực hoặc hành vi thanh toán thực. |

## II. Các bước chính
### 1. Mở phiên bản mini game mục tiêu
Đầu tiên, hãy xác nhận xem bạn đang khắc phục sự cố trên phiên bản nào: gỡ lỗi cục bộ (local debugging), bản dựng xem trước (preview build), bản dựng nhiệm vụ đánh giá (review task build), hay môi trường sản xuất (production). Tùy chọn Nhà phát triển có thể hiển thị các khả năng khác nhau trong các môi trường khác nhau, vì vậy hãy bao gồm nguồn truy cập và loại phiên bản khi báo cáo sự cố.

### 2. Mở menu xem thêm và Tùy chọn Nhà phát triển
Sau khi vào mini game, hãy nhấn vào nút `...` ở menu dạng viên nang (capsule menu) góc trên cùng bên phải và mở Tùy chọn Nhà phát triển (Developer Options) từ menu xem thêm. Nếu không thấy mục này, hãy kiểm tra tài khoản, loại bản dựng, phiên bản Ứng dụng TikTok và quyền kiểm thử trước.


### 3. Bật Bảng DevInfo và vConsole
Trong Tùy chọn Nhà phát triển, hãy bật công tắc phù hợp với mục tiêu khắc phục sự cố của bạn:
- Bật Bảng DevInfo (DevInfo Panel) khi bạn cần các trường thông tin môi trường và tóm tắt lỗi nhanh.
- Bật vConsole khi bạn cần nhật ký lúc chạy (runtime logs), ngăn xếp lỗi (error stacks), hoặc các yêu cầu mạng.

Sau khi khắc phục sự cố, hãy tắt các công tắc này và kiểm tra lại từ góc nhìn của người dùng bình thường.

### 4. Đọc các trường trong Bảng DevInfo

| Trường (Field) | Ý nghĩa (Meaning) | Cách sử dụng trong báo cáo (How to use it in reports) |
|---|---|---|
| Client Key | Định danh duy nhất của mini game trên Nền tảng Nhà phát triển TikTok. | Bắt buộc đối với các báo cáo lỗi. Nó giúp xác định mini game và phiên bản. |
| FPS | Tốc độ khung hình theo thời gian thực hiện tại. | Chụp lại khi gặp vấn đề giật lag, rớt khung hình hoặc treo màn hình. |
| SDK Version | Phiên bản SDK được sử dụng bởi môi trường chạy mini game hiện tại. | Bắt buộc đối với các vấn đề về hành vi SDK, lỗi API hoặc sự khác biệt giữa các phiên bản. |
| System Info | Phiên bản TikTok, hệ điều hành, ngôn ngữ, loại mạng, kích thước màn hình, kích thước cửa sổ và các chi tiết môi trường máy khách khác. | Sử dụng để tái hiện môi trường đánh giá hoặc môi trường của người dùng. Hãy chụp ảnh màn hình hoặc sao chép toàn bộ. |
| Error | Tóm tắt lỗi chính trong quá trình chạy. Thông thường sẽ hiển thị N/A khi không có lỗi chính nào bị bắt. | Nếu không phải là N/A, hãy chụp lại và mở vConsole để xem chi tiết lỗi đầy đủ. |

### 5. Kiểm tra các công tắc gỡ lỗi và trạng thái mô phỏng (mock states)

| Công tắc (Switch) | Trạng thái dự kiến (Expected state) | Phải làm gì nếu trạng thái không như dự kiến |
|---|---|---|
| Disable Domain Verification (Tắt Xác minh Tên miền) | Thường TẮT (OFF) đối với xác thực hoặc đánh giá chính thức. | Nếu các yêu cầu mạng chỉ hoạt động sau khi bật công tắc này, hãy quay lại phần cấu hình tên miền dịch vụ trên nền tảng. |
| Enable IAA Mock (Bật Mô phỏng IAA) | TẮT (OFF) khi xác thực hành vi quảng cáo thực tế. | Nếu đang BẬT (ON), kết quả quảng cáo đến từ luồng mô phỏng và không đại diện cho hành vi trên môi trường sản xuất. |
| Enable IAP Mock (Bật Mô phỏng IAP) | TẮT (OFF) khi xác thực hành vi thanh toán thực tế. | Nếu đang BẬT (ON), kết quả thanh toán đến từ luồng mô phỏng và không đại diện cho các khoản thanh toán thực. |
| vConsole | Chỉ bật khi cần thiết, sau đó tắt đi sau khi khắc phục xong sự cố. | Đối với các log phức tạp, vấn đề về Network hoặc hiệu suất, hãy tiếp tục gỡ lỗi từ xa bằng DevTool. |

### 6. Báo cáo sự cố với gói bằng chứng cố định

Khi báo cáo sự cố cho nền tảng, nhà phát triển hoặc hỗ trợ Oncall, hãy bao gồm ít nhất:
1. Tên mini game và Client Key.
2. Loại phiên bản: gỡ lỗi cục bộ (local debugging), bản dựng xem trước (preview build), bản dựng nhiệm vụ đánh giá (review task build), hoặc môi trường sản xuất (production).
3. SDK Version và toàn bộ System Info.
4. Văn bản lỗi (Error text). Nếu không có lỗi, hãy ghi N/A.
5. Các ảnh chụp màn hình quan trọng: Bảng DevInfo, cảnh xảy ra sự cố, vConsole Error / Network.
6. Đường dẫn tái hiện: nguồn truy cập (entry source), thao tác của người dùng, và bước chính xác gây ra lỗi.

## III. Câu hỏi thường gặp (FAQ)

| Triệu chứng (Symptom) | Kiểm tra đầu tiên (Check first) | Bao gồm trong báo cáo (Include in the report) |
|---|---|---|
| Không thể mở, bị kẹt, hoặc tải thất bại | Error, vConsole Error, vConsole Network. | Client Key, SDK Version, System Info, ảnh chụp màn hình lỗi. |
| Giật lag, rớt khung hình, tương tác bị trễ | FPS, System Info, nguồn truy cập. | Ảnh chụp màn hình FPS, chi tiết thiết bị và mạng, đường dẫn tái hiện. |
| Yêu cầu thất bại, không thể tải nội dung | Disable Domain Verification, vConsole Network. | Ảnh chụp màn hình yêu cầu thất bại, trạng thái xác minh tên miền, tên miền yêu cầu. |
| Hành vi quảng cáo bất thường | Enable IAA Mock, vConsole Error. | Đơn vị quảng cáo (Ad unit), trạng thái mô phỏng (mock state), ảnh chụp màn hình sự cố. |
| Hành vi thanh toán bất thường | Enable IAP Mock, vConsole Error. | Tình huống đơn hàng, trạng thái mô phỏng, ảnh chụp màn hình luồng thanh toán. |

**1. Tại sao tôi không thể thấy mục Tùy chọn Nhà phát triển (Developer Options)?**
Kiểm tra tài khoản, phiên bản mini game, phiên bản Ứng dụng TikTok và nguồn truy cập trước tiên. Một số phiên bản sản xuất hoặc chế độ xem của người dùng thông thường có thể không hiển thị đầy đủ các Tùy chọn Nhà phát triển. Các bản dựng xem trước, người dùng kiểm thử hoặc các mục dành cho nhiệm vụ đánh giá thường tốt hơn để khắc phục sự cố.
Nếu bạn cần log đầy đủ, chi tiết Mạng (Network) hoặc kiểm tra yêu cầu, hãy sử dụng tính năng gỡ lỗi từ xa của DevTool trước.

**2. Tại sao các yêu cầu hoạt động sau khi tắt xác minh tên miền, nhưng lại thất bại với những người khác?**
Tắt xác minh tên miền chỉ là một công tắc gỡ lỗi. Nó giúp xác định xem việc gửi yêu cầu thất bại có thể liên quan đến tính năng xác minh tên miền của nền tảng hay không.
Nó không cho phép những người dùng kiểm thử khác, môi trường đánh giá, hoặc người dùng trên môi trường sản xuất bỏ qua việc xác minh tên miền. Tiếp theo, hãy kiểm tra phần cấu hình tên miền dịch vụ của nền tảng, HTTPS, cổng (ports), đường dẫn, tên miền WebSocket và quá trình truyền tải cấu hình.

**3. Tại sao quảng cáo hoặc thanh toán trông có vẻ thành công, nhưng kết quả trên môi trường sản xuất lại khác?**
Hãy kiểm tra xem Enable IAA Mock hoặc Enable IAP Mock có đang được bật hay không.
Nếu Mock đang BẬT (ON), kết quả quảng cáo hoặc thanh toán đến từ một luồng giả lập. Nó có thể xác thực logic nghiệp vụ cục bộ, nhưng không đại diện cho SDK quảng cáo thực hoặc luồng thanh toán thực. Để xác thực thực tế, hãy tắt Mock và khắc phục sự cố trong bản dựng xem trước hoặc bản dựng sản xuất theo hướng dẫn khả năng chuyên dụng.

**4. Nếu vConsole hiển thị lỗi, tôi có vẫn cần DevTool không?**
Điều này phụ thuộc vào độ sâu của vấn đề. vConsole rất hữu ích để kiểm tra nhanh các phần Error, Warn, và Network. Nếu bạn cần toàn bộ chi tiết mạng, ngữ cảnh log, lặp lại việc tái hiện hoặc khắc phục sự cố khả năng của máy khách, hãy tiếp tục sử dụng gỡ lỗi từ xa với DevTool.

**5. Lỗi (Error) hiển thị N/A. Điều đó có nghĩa là không có vấn đề gì đúng không?**
Không. N/A chỉ có nghĩa là Bảng DevInfo không nắm bắt được tóm tắt lỗi chính. Các trạng thái bị kẹt, tải tài nguyên chậm, thất bại yêu cầu mạng, các vấn đề về thích ứng giao diện (UI), quảng cáo hoặc thanh toán vẫn có thể tồn tại. Hãy sử dụng vConsole, Network, System Info và đường dẫn tái hiện kết hợp với nhau.

**6. Phiên bản sản xuất có thể mở toàn bộ Tùy chọn Nhà phát triển không?**
Tùy chọn Nhà phát triển trong môi trường sản xuất có thể bị giới hạn bởi phiên bản Ứng dụng TikTok, quyền của tài khoản và môi trường. Đối với các vấn đề trên môi trường sản xuất, hãy ghi lại ít nhất Client Key, SDK Version, System Info, nguồn truy cập và thao tác của người dùng. Nếu bạn cần các khả năng gỡ lỗi đầy đủ, hãy tái hiện sự cố với một bản dựng xem trước hoặc bằng cách gỡ lỗi từ xa với DevTool.

## IV. Tham khảo thêm
- [Hướng dẫn sử dụng DevTool cho TikTok Mini Games](./TikTokMiniGamesDevToolUsageGuide.md)
- [Tham khảo JSAPI / Khả năng Runtime cho TikTok Mini Games](./TikTokMiniGamesJSAPIRuntimeCapabilityReference.md)
