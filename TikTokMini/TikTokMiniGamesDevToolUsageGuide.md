# Hướng dẫn sử dụng DevTool cho TikTok Mini Games

DevTool kết nối dự án mini game cục bộ với ứng dụng TikTok. Sử dụng công cụ này để khởi chạy cục bộ, xem trước bằng mã QR, gỡ lỗi từ xa, tải cấu hình nền tảng, hướng dẫn tích hợp các khả năng, kiểm tra trước mã nguồn và xác thực trước khi tải lên.

Trong phiên gỡ lỗi đầu tiên, hãy đảm bảo Node và `ttmg` CLI đã được cài đặt, chạy lệnh `ttmg login` bằng tài khoản nhà phát triển có quyền truy cập vào mini game mục tiêu, sau đó chạy lệnh `ttmg dev` trong thư mục mini game đã được xuất ra. Sau khi DevTool mở, hãy quét mã QR bằng Ứng dụng TikTok để xem trước trên thiết bị thật. Chuyển sang gỡ lỗi từ xa (remote debugging) khi bạn cần xem nhật ký (logs), điểm ngắt (breakpoints) và các yêu cầu mạng (requests).

## I. Quy trình sử dụng (Usage Flow)

**Khởi chạy phiên gỡ lỗi đầu tiên**
1. **Cài đặt và đăng nhập:** Cài đặt `@ttmg/cli`, sau đó chạy `ttmg login` bằng tài khoản có quyền truy cập vào trò chơi mục tiêu.
2. **Khởi động DevTool:** Chạy lệnh `ttmg dev` trong thư mục mini game đã được xuất ra.
3. **Quét mã để bắt đầu phiên gỡ lỗi:** Sử dụng Ứng dụng TikTok trên điện thoại của bạn và trước tiên hãy xác nhận tính năng xem trước trên thiết bị thật đang hoạt động.
4. **Chọn chế độ gỡ lỗi:** Sử dụng gỡ lỗi từ xa (remote debugging) cho logs, breakpoints và requests; sử dụng chế độ xem trước trên thiết bị thật cho hành vi bên phía điện thoại.
5. **Xác thực và khắc phục sự cố:** Khi xuất hiện các vấn đề về quyền, proxy, tên miền, quét mã, kiểm tra trước (pre-check) hoặc tải lên, hãy tìm hiểu chúng thông qua phần tương ứng trong hướng dẫn này.

![Quy trình sử dụng](./Image/TikTokMiniGamesDevToolUsageGuide/image1.png)

**Mở tính năng phù hợp với sự cố của bạn**
- **Tổng quan Dự án (Project Overview):** Xác nhận Client Key, trạng thái gói, Tên miền Tin cậy (Trust Domain), URL Điều khoản (Terms URL), URL Quyền riêng tư (Privacy URL) và cấu hình nền tảng.

![Tổng quan Dự án](./Image/TikTokMiniGamesDevToolUsageGuide/image2.png)
- **Tích hợp Khả năng (Capability Integration):** Tìm các điểm truy cập khả năng và đề xuất gỡ lỗi cho đăng nhập, ủy quyền, quảng cáo, IAP, chia sẻ, revisit và các khả năng cơ bản.

![Tích hợp Khả năng](./Image/TikTokMiniGamesDevToolUsageGuide/image3.png)
- **Chế độ Khởi chạy (Launch mode):** Định cấu hình gói khởi động (startup package), cảnh truy cập (entry scene) và tham số khởi chạy. Chỉ thực hiện khi bạn cần tái hiện một chiến dịch, sự kiện chia sẻ, revisit, một cảnh hoặc một tham số cụ thể.

![Chế độ Khởi chạy](./Image/TikTokMiniGamesDevToolUsageGuide/image4.png)
- **Tùy chọn Nhà phát triển (Developer Options):** Sử dụng vConsole, DevInfo, công tắc xác minh tên miền và các khả năng mô phỏng (mock) để thu hẹp phạm vi sự cố.

![Tùy chọn Nhà phát triển](./Image/TikTokMiniGamesDevToolUsageGuide/image5.png)
- **Tải lên & Xuất bản (Upload & Publish):** Kiểm tra các rủi ro trước khi tải lên, sau đó dùng tài khoản kiểm thử để quét bản dựng xem trước của nền tảng (platform preview build) vừa được tải lên.

![Tải lên & Xuất bản](./Image/TikTokMiniGamesDevToolUsageGuide/image6.png)
- **Khắc phục sự cố trên môi trường sản xuất (Production troubleshooting):** Sử dụng kết hợp DevInfo, nguồn truy cập (entry source) và cấu hình nền tảng cho các sự cố của phiên bản đã xuất bản.

![Khắc phục sự cố trên môi trường sản xuất](./Image/TikTokMiniGamesDevToolUsageGuide/image7.png)

## II. Các bước chính (Key Steps)

### 1. Cài đặt môi trường gỡ lỗi
Đối với phiên bản beta Subpackage Collection này, hãy cài đặt bản dựng được ghim `@ttmg/cli@0.4.4-beta.1`. Hãy sử dụng registry npm chính thức nếu registry nội bộ vẫn chưa đồng bộ.
```bash
npm install -g @ttmg/cli@0.4.4-beta.1 --registry=https://registry.npmjs.org/
ttmg -v
```

Các lệnh thường dùng:

| Lệnh (Command) | Khi nào sử dụng (When to use it) | Ghi chú (Notes) |
|---|---|---|
| `ttmg setup` | Lần đầu thiết lập môi trường cục bộ | Khởi tạo các phần phụ thuộc cục bộ và cấu hình cơ bản |
| `ttmg init` | Tạo hoặc khởi tạo một dự án | Tạo cấu hình dự án TikTok Mini Games |
| `ttmg login` | Trước khi dùng các tính năng phụ thuộc vào nền tảng | Đăng nhập vào tài khoản Nền tảng Nhà phát triển (Developer Platform) |
| `ttmg logout` | Chuyển đổi hoặc xóa trạng thái tài khoản | Sử dụng khi đang đăng nhập sai tài khoản |
| `ttmg config` | Xem hoặc cập nhật cấu hình cục bộ | Thường được sử dụng để cấu hình ngôn ngữ và proxy |
| `ttmg dev` | Khởi động DevTool | Điểm truy cập cho việc gỡ lỗi cục bộ và xem trước |
| `ttmg dev --verbose` | Khắc phục sự cố khởi động hoặc quét mã QR | In chi tiết nhật ký kết nối và dịch vụ cục bộ |
| `ttmg build` | Xây dựng (build) cục bộ | Tạo tệp đầu ra của bản dựng trước khi tải lên |
| `ttmg upload` | Tải lên cho kiểm thử trên nền tảng | Tạo bản dựng xem trước của nền tảng |
| `ttmg reset` | Trạng thái CLI cục bộ bị lỗi | Đặt lại trạng thái CLI cục bộ; sử dụng cẩn thận |
| `ttmg -v` | Xác nhận phiên bản | Hiển thị phiên bản CLI hiện tại |
| `ttmg --help` | Khi bạn không chắc chắn | Hiển thị thông tin hỗ trợ cho các lệnh |

### 2. Đăng nhập và xác nhận quyền truy cập
Lệnh `ttmg login` sử dụng tài khoản Nền tảng Nhà phát triển (Developer Platform account). Trong khi việc quét mã QR sử dụng tài khoản TikTok trên điện thoại. Chúng là các trạng thái đăng nhập hoàn toàn khác nhau.
- Nếu Tổng quan Dự án (Project Overview), Tải lên & Xuất bản (Upload & Publish) hoặc cấu hình nền tảng không thể tải được, trước tiên hãy xác nhận xem tài khoản đăng nhập qua `ttmg login` có quyền truy cập vào Client Key hiện tại hay không.
- Nếu tài khoản có quyền truy cập nhưng Tên miền Tin cậy (Trust Domain), URL Điều khoản (Terms URL), URL Quyền riêng tư (Privacy URL) hoặc dữ liệu nền tảng khác bị trống, trước tiên hãy kiểm tra proxy của thiết bị đầu cuối hoặc mạng văn phòng của bạn.
- Nếu việc xem trước bằng mã QR bị lỗi, hãy xác nhận xem tài khoản TikTok trên điện thoại đã được thêm làm người dùng kiểm thử (test user) cho mini game hiện tại hay chưa.
