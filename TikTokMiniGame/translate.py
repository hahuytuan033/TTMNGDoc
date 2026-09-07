import re

file_path = r'C:\Developer\TTMNGDoc\TikTokMiniGame\TikTokMiniGame\Assets\AmobearTTMNG\Demo\Scripts\DemoMainMenu.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

replacements = [
(r'/// <summary>\s*/// Demo entry MonoBehaviour[\s\S]*?<see cref="EditorTools.DemoSceneBuilder"/> 生成。\s*/// </summary>', '''/// <summary>
    /// MonoBehaviour đầu vào của Demo. Xây dựng một danh sách dạng accordion cho các nút API TT.*
    /// khi chạy, liên kết từng nút với một trình xử lý và ghi kết quả vào bảng log.
    /// Bản thân UI được tạo bởi <see cref="EditorTools.DemoSceneBuilder"/>.
    /// </summary>'''),

(r'// EN: Inspector references — wired by DemoSceneBuilder when it rebuilds the scene.\s*// ZH: Inspector 引用——DemoSceneBuilder 重建场景时自动接线。', '''// Các tham chiếu Inspector — được kết nối tự động bởi DemoSceneBuilder khi nó xây dựng lại scene.'''),

(r'Container that holds the ButtonTemplate \+ all spawned category/sub-buttons\. / 承载 ButtonTemplate 与所有运行时生成按钮的容器。', 'Container chứa ButtonTemplate + tất cả các nút danh mục/nút con được tạo ra.'),
(r'Top-most title text\. / 顶部标题。', 'Văn bản tiêu đề ở trên cùng.'),
(r'Log panel text content \(rolling buffer\)\. / 日志面板文本（滚动缓冲）。', 'Nội dung văn bản của bảng log (bộ đệm cuộn).'),
(r'ScrollRect wrapping <see cref="LogText"/>; we keep it pinned to the bottom on append\. / 包裹 LogText 的 ScrollRect；每次追加日志后会自动滚到底部。', 'ScrollRect bọc <see cref="LogText"/>; chúng ta ghim nó ở dưới cùng mỗi khi thêm log mới.'),
(r'Clears the log buffer\. / 清空日志缓冲。', 'Xóa bộ đệm log.'),
(r'The big "Init SDK" button at the top — must be pressed first\. / 顶部的"Init SDK"按钮，必须首先按一次。', 'Nút "Init SDK" lớn ở trên cùng — phải được nhấn đầu tiên.'),
(r'Tiny status text under the title showing inited / not inited\. / 标题下方显示 SDK 已初始化 / 未初始化的小字。', 'Văn bản trạng thái nhỏ dưới tiêu đề hiển thị trạng thái đã khởi tạo / chưa khởi tạo.'),

(r'/// <summary>\s*/// SDK package version baked in by <see cref="EditorTools.DemoSceneBuilder"/> at[\s\S]*?<c>Assets/Plugins/com.tiktok.minigame/package.json</c> 写入的 SDK 包版本号。\s*/// </summary>', '''/// <summary>
        /// Phiên bản package SDK được ghi nhận bởi <see cref="EditorTools.DemoSceneBuilder"/> khi
        /// xây dựng lại scene bằng cách đọc <c>Assets/Plugins/com.tiktok.minigame/package.json</c>.
        /// </summary>'''),

(r'// EN: Runtime state\.\s*// ZH: 运行时状态。', '// Trạng thái runtime.'),

(r'// EN: Listener handles so we can Off them later[\s\S]*?// ZH: 保存监听句柄方便后续 Off 掉。不存的话 Off\* 找不到对应回调（TT 内部按委托相等判断）。', '''// Lưu trữ các listener handles để chúng ta có thể Off (hủy đăng ký) sau này. Nếu không có những biến này,
        // các API Off* không thể tìm thấy callback tương ứng để xóa bỏ (TT đăng ký qua so sánh delegate).'''),

(r'Lifecycle / 生命周期', 'Vòng đời / Lifecycle'),

(r'// EN: Title shows the SDK \*package\* version[\s\S]*?//     而 CP 一般习惯按发版包号（如 1\.1\.1）来识别。', '''// Tiêu đề hiển thị phiên bản *package* SDK (file unitypackage mà bạn import),
            // được ghi sẵn bởi DemoSceneBuilder. Chúng ta ưu tiên dùng nó thay vì TT.TTSDKVersion vì
            // TT.TTSDKVersion theo dõi bản revision nội bộ của mã nguồn SDK (ví dụ 6.3.9),
            // trong khi các nhà phát triển (CP) thường ghi nhớ theo phiên bản package đã phát hành (ví dụ 1.1.1).'''),

(r'UI build / UI 构建', 'Xây dựng UI'),

(r'/// <summary>\s*/// Define the full category/API tree\. Adding a new TT\.\* API = one line\.[\s\S]*?/// 在这里集中声明分类与接口树，新增 TT\.\* 接口只需加一行。\s*/// </summary>', '''/// <summary>
        /// Định nghĩa toàn bộ cây danh mục/API. Để thêm một API TT.* mới, chỉ cần thêm một dòng.
        /// </summary>'''),

(r'/// <summary>\s*/// Add one category header \+ a list of collapsed sub-buttons\. Header click toggles\.[\s\S]*?/// 添加一个分类头 \+ 一组初始折叠的子按钮，点击头部展开/收起。\s*/// </summary>', '''/// <summary>
        /// Thêm một header danh mục + một danh sách các nút con được thu gọn. Bấm vào header để mở rộng/thu gọn.
        /// </summary>'''),

(r'// EN: Header button uses a distinct slate color to contrast with the blue sub-buttons\.\s*// ZH: 分类头使用 slate 灰色，与蓝色子按钮形成对比。', '// Nút Header sử dụng màu xám (slate) đặc trưng để tạo độ tương phản với các nút con màu xanh.'),

(r'// EN: ContentSizeFitter \+ VerticalLayoutGroup recompute their layout on the next[\s\S]*?//     ForceUpdateCanvases 立刻刷新，让滚动条位置不抖。', '''// ContentSizeFitter + VerticalLayoutGroup sẽ tính toán lại layout ở frame tiếp theo.
                // ForceUpdateCanvases buộc cập nhật lại chiều cao của cuộn ngay lập tức để giữ thanh cuộn ở đúng vị trí mà không bị giật.'''),

(r'// EN: indent for visual hierarchy / ZH: 缩进表达层级', '// thụt lề để thể hiện phân cấp giao diện'),
(r'// EN: start collapsed / ZH: 默认折叠', '// mặc định là thu gọn'),

(r'/// <summary>\s*/// Clone the ButtonTemplate \(always child 0 of <see cref="ButtonContainer"/>\) and[\s\S]*?/// 克隆 ButtonTemplate（永远是 <see cref="ButtonContainer"/> 的第 0 个子节点）并返回新 Button。\s*/// </summary>', '''/// <summary>
        /// Tạo một bản sao của ButtonTemplate (luôn là child 0 của <see cref="ButtonContainer"/>) và
        /// trả về instance Button mới được tạo.
        /// </summary>'''),

(r'/// <summary>\s*/// Run an API handler wrapped in try/catch so a misbehaving SDK call doesn\'t kill[\s\S]*?/// SynchronizationContext 异步抛错，这里的 try/catch 抓不到——Init 路径单独处理。\s*/// </summary>', '''/// <summary>
        /// Chạy một trình xử lý API được bọc trong try/catch để đảm bảo nếu một lời gọi SDK gặp lỗi
        /// cũng không làm hỏng menu. Lưu ý: TT.InitSDK ném lỗi bất đồng bộ qua SynchronizationContext,
        /// vì vậy try/catch ở đây KHÔNG bắt được ngoại lệ của InitSDK — việc đó được xử lý riêng.
        /// </summary>'''),

(r'SDK initialization / SDK 初始化', 'Khởi tạo SDK'),

(r'/// <summary>\s*/// Call <c>TT\.InitSDK</c>\. Must succeed before any container-dependent API[\s\S]*?/// 走 Mock 实现，正式 WebGL 包则与真实容器通信。\s*/// </summary>', '''/// <summary>
        /// Gọi <c>TT.InitSDK</c>. Phải thành công trước khi có thể gọi bất kỳ API nào phụ thuộc vào
        /// container (Login / Pay / Ads / Share). Trong Unity Editor (với SDK >= 1.1.1, DLL dành cho editor
        /// sẽ tự động tải và gọi thành công qua bản giả lập Mock); trong bản build WebGL thì nó sẽ giao tiếp với container thật.
        /// </summary>'''),

(r'// EN: code == 0 → success; non-zero values are SDK-specific errors\.\s*// ZH: code == 0 表示成功，其他值参考 SDK 文档错误码。', '// code == 0 là thành công; các giá trị khác 0 là lỗi do SDK trả về.'),

(r'// EN: With SDK < 1\.1\.1 InitSDK throws synchronously in Editor; we still[\s\S]*?// ZH: SDK < 1\.1\.1 在 Editor 会同步抛错，这里捕获并提示用户。', '''// Với SDK < 1.1.1 InitSDK sẽ ném lỗi đồng bộ trong Editor; chúng ta vẫn sẽ
                // bắt lỗi đó và hiển thị cho người dùng.'''),

(r'// API handlers — each region maps 1:1 to a category in BuildFeatureButtons\.\s*// API 处理器——每个 region 与 BuildFeatureButtons 中的一个分类一一对应。', '// Trình xử lý API — mỗi phân vùng tương ứng 1:1 với một danh mục trong BuildFeatureButtons.'),

(r'/// <summary>\s*/// <c>TT\.GetSystemInfo</c> — host / device / locale info\. Available before InitSDK in v6\.5\+\.[\s\S]*?/// <c>TT\.GetSystemInfo</c>——宿主 / 设备 / 语言等信息。v6\.5\+ 不依赖 InitSDK 即可调用。\s*/// </summary>', '''/// <summary>
        /// <c>TT.GetSystemInfo</c> — thông tin thiết bị / host / ngôn ngữ. Hỗ trợ gọi trước InitSDK ở phiên bản v6.5+.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.GetContainerVersion</c> \+ version constants\. Useful for compat checks\.[\s\S]*?/// <c>TT\.GetContainerVersion</c> \+ 版本常量。常用于做兼容性判断。\s*/// </summary>', '''/// <summary>
        /// <c>TT.GetContainerVersion</c> + hằng số phiên bản. Thường dùng để kiểm tra tính tương thích.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.GetLaunchOptionsSync</c> — params the host passed to the game on launch\.[\s\S]*?/// <c>TT\.GetLaunchOptionsSync</c>——宿主传入的启动参数（scene、path、query）。\s*/// </summary>', '''/// <summary>
        /// <c>TT.GetLaunchOptionsSync</c> — các tham số (scene, path, query) được truyền cho game khi khởi chạy.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.CleanAllFileCache</c> — wipes the cache directory the SDK manages\.[\s\S]*?/// <c>TT\.CleanAllFileCache</c>——清空 SDK 维护的缓存目录。\s*/// </summary>', '''/// <summary>
        /// <c>TT.CleanAllFileCache</c> — dọn dẹp thư mục cache do SDK quản lý.
        /// </summary>'''),

(r'/// <summary>\s*/// Subscribe to <c>TT\.GetAppLifeCycle\(\)\.OnShow / OnHide</c> for foreground/background events\.[\s\S]*?/// 订阅 <c>TT\.GetAppLifeCycle\(\)\.OnShow / OnHide</c>，监听前/后台切换。\s*/// </summary>', '''/// <summary>
        /// Đăng ký theo dõi <c>TT.GetAppLifeCycle().OnShow / OnHide</c> để nhận sự kiện chạy nền/hiển thị lại.
        /// </summary>'''),

(r'/// <summary>\s*/// Intercept the user\'s exit gesture\. Return <c>true</c> to suppress default exit[\s\S]*?/// 返回 <c>false</c> 表示走容器默认退出。\s*/// </summary>', '''/// <summary>
        /// Chặn thao tác thoát của người dùng. Trả về <c>true</c> để ngăn chặn hành động thoát mặc định
        /// (bạn sẽ phải tự gọi logic thoát của riêng mình), hoặc <c>false</c> để cho phép container đóng game.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.Login</c> — exchange container session for a short-lived <c>code</c> your[\s\S]*?/// <c>TT\.Login</c>——把容器会话换成临时 <c>code</c>，由后端拿去抖音开放接口换 openid。\s*/// </summary>', '''/// <summary>
        /// <c>TT.Login</c> — đổi phiên container lấy <c>code</c> có thời hạn ngắn, sau đó backend
        /// của bạn dùng mã này lấy openid thông qua openapi của Douyin.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.Authorize</c> — request a scope\. "scope\.userInfo" is the most common one\.[\s\S]*?/// <c>TT\.Authorize</c>——请求授权 scope。最常用的是 "scope\.userInfo"。\s*/// </summary>', '''/// <summary>
        /// <c>TT.Authorize</c> — yêu cầu cấp quyền truy cập. "scope.userInfo" là quyền phổ biến nhất.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.ShareAppMessage</c> — open the container\'s share sheet\. <c>TemplateType=1</c>[\s\S]*?/// <c>Path</c>/<c>Query</c> 用来回跳到游戏内指定页面。\s*/// </summary>', '''/// <summary>
        /// <c>TT.ShareAppMessage</c> — mở bảng chia sẻ của container. <c>TemplateType=1</c>
        /// là dạng template "thẻ" tiêu chuẩn; <c>Path</c>/<c>Query</c> là link deep-link trỏ ngược về game.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.Pay</c> — create a payment for a trade order id returned by your backend\.[\s\S]*?/// <c>TT\.Pay</c>——用后端返回的 trade_order_id 发起支付。\s*/// </summary>', '''/// <summary>
        /// <c>TT.Pay</c> — thực hiện thanh toán với mã đơn hàng (trade order id) do backend của bạn trả về.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.CheckBalance</c> — query whether the user has enough virtual currency\.[\s\S]*?/// <c>TT\.CheckBalance</c>——查询用户虚拟币余额是否足够。\s*/// </summary>', '''/// <summary>
        /// <c>TT.CheckBalance</c> — truy vấn xem người dùng có đủ tiền ảo (virtual currency) hay không.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.Recharge</c> — open the recharge flow for a configured tier id\.[\s\S]*?/// <c>TT\.Recharge</c>——拉起预设档位的充值流程。\s*/// </summary>', '''/// <summary>
        /// <c>TT.Recharge</c> — mở giao diện nạp tiền với một mức giá (tier id) được cấu hình trước.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.NavigateToBalance</c> — open the user\'s balance / wallet view\.[\s\S]*?/// <c>TT\.NavigateToBalance</c>——跳转到余额 / 钱包页面。\s*/// </summary>', '''/// <summary>
        /// <c>TT.NavigateToBalance</c> — mở giao diện xem số dư / ví tiền của người dùng.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.StartEntranceMission</c> — kick off a "first-time entry" mission flow[\s\S]*?/// <c>TT\.StartEntranceMission</c>——发起入口任务流程（如从特定渠道首次进入给奖励）。\s*/// </summary>', '''/// <summary>
        /// <c>TT.StartEntranceMission</c> — khởi chạy quy trình nhiệm vụ "lần đầu tiên truy cập"
        /// (ví dụ: tặng thưởng 1 lần cho người chơi truy cập từ một kênh được chỉ định).
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.GetEntranceMissionReward</c> — check / claim the reward for the active mission\.[\s\S]*?/// <c>TT\.GetEntranceMissionReward</c>——查询或领取入口任务奖励。\s*/// </summary>', '''/// <summary>
        /// <c>TT.GetEntranceMissionReward</c> — kiểm tra / nhận phần thưởng cho nhiệm vụ đang kích hoạt.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.CreateRewardedVideoAd</c> — rewarded video\. <b>No</b> <c>Load\(\)</c>: the[\s\S]*?/// 告诉你用户是否看完（看完才发奖）。\s*/// </summary>', '''/// <summary>
        /// <c>TT.CreateRewardedVideoAd</c> — video nhận thưởng. <b>Không có</b> <c>Load()</c>:
        /// đối tượng trả về chỉ hiển thị <c>Show()</c> / <c>Destroy()</c>; SDK sẽ tự động load trước
        /// nội dung bên trong. <c>OnClose(isEnded)</c> thông báo cho bạn biết người dùng đã xem xong video hay chưa (để bạn trao thưởng).
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.CreateInterstitialAd</c> — interstitial\. Same shape as rewarded but with no[\s\S]*?/// <c>TT\.CreateInterstitialAd</c>——插屏广告。接口形态与激励视频相同，无奖励回调。\s*/// </summary>', '''/// <summary>
        /// <c>TT.CreateInterstitialAd</c> — quảng cáo xen kẽ. Có dạng tương tự video quảng cáo nhưng không có callback nhận thưởng.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.GetNetWorkType</c> — one-shot query of the current connectivity\.[\s\S]*?/// <c>TT\.GetNetWorkType</c>——一次性查询当前网络类型。\s*/// </summary>', '''/// <summary>
        /// <c>TT.GetNetWorkType</c> — kiểm tra tình trạng kết nối mạng hiện tại một lần.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.OnNetworkStatusChange</c> — subscribe to connection on/off events\.[\s\S]*?/// <c>TT\.OnNetworkStatusChange</c>——监听网络连接变化。\s*/// </summary>', '''/// <summary>
        /// <c>TT.OnNetworkStatusChange</c> — đăng ký nhận các sự kiện bật/tắt kết nối mạng.
        /// </summary>'''),

(r'/// <summary>Mirror of On / 对应 On 的取消订阅。</summary>', '/// <summary>Hủy đăng ký tương ứng với hàm On.</summary>'),

(r'/// <summary>\s*/// <c>TT\.OnNetworkWeakChange</c> — subscribe to "weak network" toggles \(lag detection\)\.[\s\S]*?/// <c>TT\.OnNetworkWeakChange</c>——监听弱网状态切换（用于卡顿检测）。\s*/// </summary>', '''/// <summary>
        /// <c>TT.OnNetworkWeakChange</c> — đăng ký nhận sự kiện "mạng yếu" (phát hiện giật lag do mạng).
        /// </summary>'''),

(r'/// <summary><c>TT\.VibrateShort</c> — ~15ms haptic\. / <c>TT\.VibrateShort</c>——约 15ms 触感。</summary>', '/// <summary><c>TT.VibrateShort</c> — rung ngắn ~15ms.</summary>'),
(r'/// <summary><c>TT\.VibrateLong</c> — ~400ms haptic\. / <c>TT\.VibrateLong</c>——约 400ms 触感。</summary>', '/// <summary><c>TT.VibrateLong</c> — rung dài ~400ms.</summary>'),

(r'/// <summary>\s*/// <c>TT\.SetPreferredFramesPerSecond</c> — hint the engine \+ container for a target FPS\.[\s\S]*?/// <c>TT\.SetPreferredFramesPerSecond</c>——给引擎和容器一个目标 FPS 提示。\s*/// </summary>', '''/// <summary>
        /// <c>TT.SetPreferredFramesPerSecond</c> — gửi gợi ý FPS mục tiêu cho engine và container.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.ShowKeyboard</c> — bring up the soft keyboard \(WebGL only\)\. Hook the[\s\S]*?/// 输入 / 确认 / 收起回调挂在 <c>TT\.GetTTKeyboard\(\)</c> 上。\s*/// </summary>', '''/// <summary>
        /// <c>TT.ShowKeyboard</c> — hiển thị bàn phím ảo (chỉ hỗ trợ WebGL). Gắn các
        /// callback khi gõ phím / xác nhận / ẩn bàn phím qua <c>TT.GetTTKeyboard()</c> nếu bạn cần.
        /// </summary>'''),

(r'/// <summary><c>TT\.HideKeyboard</c> — dismiss the soft keyboard\. / <c>TT\.HideKeyboard</c>——收起软键盘。</summary>', '/// <summary><c>TT.HideKeyboard</c> — ẩn bàn phím ảo.</summary>'),

(r'/// <summary>\s*/// <c>TT\.AddShortcut</c> — prompt the user to put the game on the home screen\.[\s\S]*?/// <c>TT\.AddShortcut</c>——请求用户把游戏添加到桌面快捷方式。\s*/// </summary>', '''/// <summary>
        /// <c>TT.AddShortcut</c> — yêu cầu người dùng thêm shortcut game vào màn hình chính.
        /// </summary>'''),

(r'/// <summary>\s*/// Demo POCO used to show <c>TT\.Save&lt;T&gt;</c>\. Any \[Serializable\] type works\.[\s\S]*?/// 演示用 POCO，配合 <c>TT\.Save&lt;T&gt;</c>。任何 \[Serializable\] 类型都可以。\s*/// </summary>', '''/// <summary>
        /// Class POCO demo dùng để minh họa <c>TT.Save&lt;T&gt;</c>. Mọi class có tag [Serializable] đều dùng được.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.PlayerPrefs</c> — cross-platform key/value store\. Same API surface as[\s\S]*?/// 一致；在 WebGL 下会落到容器的持久化存储。\s*/// </summary>', '''/// <summary>
        /// <c>TT.PlayerPrefs</c> — hệ thống lưu trữ key/value đa nền tảng. Có API giống hệt
        /// <see cref="UnityEngine.PlayerPrefs"/>; trên WebGL thì SDK sẽ định tuyến nó lưu
        /// vào bộ nhớ lưu trữ bền vững của container.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.Save&lt;T&gt;</c> — serialize an object to the SDK\'s saving slot\.[\s\S]*?/// <c>saveName</c> 指定多个槽。\s*/// </summary>', '''/// <summary>
        /// <c>TT.Save&lt;T&gt;</c> — serialize một object và lưu vào slot của SDK.
        /// Mỗi loại dữ liệu chiếm một slot trừ khi bạn chỉ định rõ <c>saveName</c>.
        /// </summary>'''),

(r'/// <summary><c>TT\.LoadSaving&lt;T&gt;</c> — read back what Save wrote\. / 读回 Save 写入的数据。</summary>', '/// <summary><c>TT.LoadSaving&lt;T&gt;</c> — đọc lại dữ liệu đã lưu từ Save.</summary>'),
(r'/// <summary><c>TT\.DeleteSaving&lt;T&gt;</c> — clear one slot\. / 清空一个存档槽。</summary>', '/// <summary><c>TT.DeleteSaving&lt;T&gt;</c> — xóa dữ liệu trong một slot.</summary>'),
(r'/// <summary><c>TT\.ClearAllSavings</c> — wipe every slot\. / 清空所有存档槽。</summary>', '/// <summary><c>TT.ClearAllSavings</c> — xóa sạch tất cả các slot lưu trữ.</summary>'),
(r'/// <summary><c>TT\.GetSavingDiskSize</c> — total bytes on disk used by savings\. / 返回所有存档槽占用磁盘字节数。</summary>', '/// <summary><c>TT.GetSavingDiskSize</c> — tổng số byte trên ổ đĩa được sử dụng bởi các dữ liệu lưu trữ.</summary>'),

(r'/// <summary>\s*/// <c>TT\.OnFeedStatusChange</c> — subscribe to feed-entry / feed-exit events \(即看即玩\)\.[\s\S]*?/// <c>TT\.OnFeedStatusChange</c>——监听即看即玩进入 / 离开。\s*/// </summary>', '''/// <summary>
        /// <c>TT.OnFeedStatusChange</c> — đăng ký nhận các sự kiện vào/thoát trạng thái Feed (chơi trực tiếp).
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.ReportEvent</c> — custom event with arbitrary JSON params\.[\s\S]*?/// <c>TT\.ReportEvent</c>——自定义埋点事件，支持任意 JSON 参数。\s*/// </summary>', '''/// <summary>
        /// <c>TT.ReportEvent</c> — sự kiện tùy chỉnh kèm tham số JSON bất kỳ.
        /// </summary>'''),

(r'/// <summary>\s*/// <c>TT\.ReportScene</c> — report a scene/page change for funnel analytics\.[\s\S]*?/// <c>TT\.ReportScene</c>——上报场景 / 页面切换，用于漏斗分析。\s*/// </summary>', '''/// <summary>
        /// <c>TT.ReportScene</c> — ghi nhận chuyển cảnh/trang để phân tích phễu (funnel analytics).
        /// </summary>'''),

(r'Log panel / 日志面板', 'Bảng log'),

(r'/// <summary>\s*/// Append a line to the rolling log \+ Unity console \+ snap the scroll view to the bottom\.[\s\S]*?/// 追加一行到滚动日志 \+ Unity 控制台 \+ 把滚动条置底。\s*/// </summary>', '''/// <summary>
        /// Thêm một dòng vào cuộn log + Console Unity + kéo thanh cuộn xuống tận cùng.
        /// </summary>'''),

(r'// EN: Drop oldest entries to keep memory bounded\.\s*// ZH: 丢弃最旧条目，防止日志无限增长。', '// Bỏ bớt các mục cũ nhất để giới hạn bộ nhớ log.'),

(r'// EN: Force layout so the ScrollRect knows the new content height before we set[\s\S]*?//     否则置底会延迟一帧。', '''// Bắt buộc render lại layout để ScrollRect tính được chiều cao nội dung mới trước khi gán
            // verticalNormalizedPosition — nếu không việc ghim xuống đáy sẽ bị trễ một frame.''')
]

for pat, rep in replacements:
    content = re.sub(pat, rep, content)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)
print("Done")