# TikTok Mini Games JS SDK Reference

> **Lưu ý về API Namespace**:  
> Namespace API tương thích với `tt`, `wx`, `TTMinis.game`, v.v.  
> *Ví dụ:* `TTMinis.game.login` tương đương với `tt.login` và `wx.login`.

---

## 1. Cơ bản (Base)

### `canIUse`

#### Mô tả
Xác định xem API, callback, tham số,... của Mini Game có khả dụng trong phiên bản hiện tại hay không.

#### Tham số
* **`schema`** (`string`): Sử dụng cú pháp `${API}.${method}.${param}.${option}` để gọi.

#### Giá trị trả về
* **`boolean`**: Trả về `true` nếu phiên bản hiện tại hỗ trợ, ngược lại trả về `false`.

#### Giải thích tham số
- **`${API}`**: Đại diện cho tên API.
- **`${method}`**: Đại diện cho phương thức gọi, các giá trị hợp lệ gồm: `return`, `success`, `object`, `callback`.
- **`${param}`**: Đại diện cho tham số hoặc giá trị trả về.
- **`${option}`**: Đại diện cho giá trị tùy chọn của tham số hoặc thuộc tính của giá trị trả về.

#### Mã mẫu (Example Code)
```javascript
// Thuộc tính hoặc phương thức của đối tượng
TTMinis.game.canIUse('env.USER_DATA_PATH');

// Tham số, callback hoặc giá trị trả về của giao diện TTMinis.game
TTMinis.game.canIUse('openBluetoothAdapter');
TTMinis.game.canIUse('getSystemInfoSync.return.safeArea.left');
TTMinis.game.canIUse('getSystemInfo.success.screenWidth');
TTMinis.game.canIUse('showToast.object.image');
TTMinis.game.canIUse('onCompassChange.callback.direction');
TTMinis.game.canIUse('request.object.method.GET');
```

---

### `env`

#### Mô tả
Chứa các biến môi trường của hệ thống.

#### Thuộc tính
* **`USER_DATA_PATH`** (`string`): Đường dẫn thư mục dữ liệu người dùng trong hệ thống tệp tin (đường dẫn cục bộ - local path).

---

## 2. Hệ thống (System)

### `getSystemInfoSync`

#### Mô tả
Lấy thông tin hệ thống của thiết bị theo phương thức đồng bộ (Synchronous).

#### Giá trị trả về
Trả về một `Object` chứa các thông tin hệ thống:

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) | Phiên bản tối thiểu (Min Version) |
| :--- | :--- | :--- | :--- |
| `screenWidth` | `number` | Chiều rộng màn hình (đơn vị: px) | |
| `screenHeight` | `number` | Chiều cao màn hình (đơn vị: px) | |
| `windowWidth` | `number` | Chiều rộng cửa sổ có thể sử dụng (đơn vị: px) | |
| `windowHeight` | `number` | Chiều cao cửa sổ có thể sử dụng (đơn vị: px) | |
| `devicePixelRatio` | `number` | Tỉ lệ pixel của thiết bị (Device Pixel Ratio) | |
| `pixelRatio` | `number` | Tỉ lệ pixel của thiết bị | |
| `deviceOrientation` | `string` | Hướng của thiết bị (Device orientation) | |
| `system` | `string` | Hệ điều hành và phiên bản | |
| `platform` | `'ios'` \| `'android'` | Nền tảng client (`ios` hoặc `android`) | |
| `statusBarHeight` | `number` | Chiều cao thanh trạng thái (Status bar height, đơn vị: px) | `0.2.0` |
| `language` | `string` | Mã ngôn ngữ (tham khảo quy chuẩn mã ngôn ngữ của TikTok Mini Game) | |
| `safeArea` | `object` | Khu vực an toàn (Safe Area) ở hướng màn hình dọc chuẩn | `0.2.0` |
| `SDKVersion` | `string` | Phiên bản của SDK | |
| `version` | `string` | Phiên bản ứng dụng Client TikTok | |

#### Mã mẫu (Example Code)
```javascript
try {
  const res = TTMinis.game.getSystemInfoSync();
  console.log(res.pixelRatio);
  console.log(res.windowWidth);
  console.log(res.windowHeight);
  console.log(res.language);
  console.log(res.version);
  console.log(res.platform);
} catch (e) {
  // Xử lý khi có lỗi xảy ra
  console.error('Lỗi khi lấy thông tin hệ thống đồng bộ:', e);
}
```

---

### `getSystemInfo`

#### Mô tả
Lấy thông tin hệ thống của thiết bị theo phương thức bất đồng bộ (Asynchronous).

#### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `success` | `function` | | Không | Hàm callback khi gọi API thành công |
| `fail` | `function` | | Không | Hàm callback khi gọi API thất bại |
| `complete` | `function` | | Không | Hàm callback khi kết thúc lời gọi (luôn thực thi dù thành công hay thất bại) |

#### Callback `object.success`
**Tham số:** `Object res`

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) | Phiên bản tối thiểu (Min Version) |
| :--- | :--- | :--- | :--- |
| `screenWidth` | `number` | Chiều rộng màn hình (đơn vị: px) | |
| `screenHeight` | `number` | Chiều cao màn hình (đơn vị: px) | |
| `windowWidth` | `number` | Chiều rộng cửa sổ có thể sử dụng (đơn vị: px) | |
| `windowHeight` | `number` | Chiều cao cửa sổ có thể sử dụng (đơn vị: px) | |
| `devicePixelRatio` | `number` | Tỉ lệ pixel của thiết bị (Device Pixel Ratio) | |
| `pixelRatio` | `number` | Tỉ lệ pixel của thiết bị | |
| `deviceOrientation` | `string` | Hướng của thiết bị | |
| `system` | `string` | Hệ điều hành và phiên bản | |
| `platform` | `'ios'` \| `'android'` | Nền tảng client (`ios` hoặc `android`) | |
| `statusBarHeight` | `number` | Chiều cao thanh trạng thái (đơn vị: px) | `0.2.0` |
| `language` | `string` | Ngôn ngữ | |
| `safeArea` | `object` | Khu vực an toàn ở hướng màn hình dọc chuẩn | `0.2.0` |
| `SDKVersion` | `string` | Phiên bản SDK | |
| `version` | `string` | Phiên bản Client | |

#### Mã mẫu (Example Code)
```javascript
TTMinis.game.getSystemInfo({
  success(res) {
    console.log(res.pixelRatio);
    console.log(res.windowWidth);
    console.log(res.windowHeight);
    console.log(res.language);
    console.log(res.version);
    console.log(res.platform);
  },
  fail(err) {
    console.error('Lấy thông tin hệ thống thất bại:', err);
  }
});
```

---

### `getWindowInfo`
> Hỗ trợ từ Base Library phiên bản **0.2.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

#### Mô tả
Lấy thông tin về cửa sổ và màn hình hiển thị của Mini Game.

#### Giá trị trả về
Trả về một `Object` chứa thông tin cửa sổ:

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `pixelRatio` | `number` | Tỉ lệ pixel của thiết bị (Device Pixel Ratio) |
| `screenWidth` | `number` | Chiều rộng màn hình (đơn vị: px) |
| `screenHeight` | `number` | Chiều cao màn hình (đơn vị: px) |
| `windowWidth` | `number` | Chiều rộng cửa sổ có thể sử dụng (đơn vị: px) |
| `windowHeight` | `number` | Chiều cao cửa sổ có thể sử dụng (đơn vị: px) |
| `statusBarHeight` | `number` | Chiều cao của thanh trạng thái (đơn vị: px) |
| `safeArea` | `Object` | Khu vực an toàn (Safe Area) ở hướng màn hình dọc chuẩn. Một số dòng máy không có khái niệm khu vực an toàn sẽ không trả về trường `safeArea`, nhà phát triển cần tự xử lý tương thích. |
| `screenTop` | `number` | Giá trị tọa độ y của mép trên cửa sổ (đơn vị: px) |

#### Mã mẫu (Example Code)
```javascript
const windowInfo = TTMinis.game.getWindowInfo();

console.log(windowInfo.pixelRatio);
console.log(windowInfo.screenWidth);
console.log(windowInfo.screenHeight);
console.log(windowInfo.windowWidth);
console.log(windowInfo.windowHeight);
console.log(windowInfo.statusBarHeight);
console.log(windowInfo.safeArea);
console.log(windowInfo.screenTop);
```

---

## 3. Vòng đời (Lifecycle)

### `onShow`

#### Mô tả
Lắng nghe sự kiện Mini Game quay trở lại tiền cảnh (foreground / hiển thị trở lại).

#### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện khi Mini Game quay trở lại tiền cảnh.

#### Tham số của `listener`
**`Object res`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `scene` | `string` | Giá trị ngữ cảnh (Scene value) khởi động game |
| `query` | `Record<string, string>` | Các tham số truy vấn (Query parameters) |

#### Mã mẫu (Example Code)
```javascript
TTMinis.game.onShow((result) => {
  let query = result.query;
  // Thực hiện xử lý khi Mini Game hiển thị trở lại
});
```

---

### `offShow`

#### Mô tả
Hủy bỏ hàm lắng nghe sự kiện Mini Game quay trở lại tiền cảnh (foreground).

#### Tham số
* **`listener`** (`function`): Hàm listener đã truyền vào trong `onShow`.

#### Mã mẫu (Example Code)
```javascript
const listener = function (res) {
  console.log(res);
};

TTMinis.game.onShow(listener);
TTMinis.game.offShow(listener); // Cần truyền vào cùng một đối tượng hàm với lúc lắng nghe
```

---

### `onHide`

#### Mô tả
Lắng nghe sự kiện Mini Game bị ẩn xuống chế độ chạy ngầm (background).

#### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện Mini Game ẩn xuống chế độ chạy ngầm.

#### Mã mẫu (Example Code)
```javascript
TTMinis.game.onHide(() => {
  // Thực hiện xử lý khi Mini Game bị ẩn (dừng nhạc, lưu trạng thái,...)
});
```

---

### `offHide`

#### Mô tả
Hủy bỏ hàm lắng nghe sự kiện Mini Game ẩn xuống chế độ chạy ngầm.

#### Tham số
* **`listener`** (`function`): Hàm listener đã truyền vào trong `onHide`.

#### Mã mẫu (Example Code)
```javascript
const listener = function (res) {
  console.log(res);
};

TTMinis.game.onHide(listener);
TTMinis.game.offHide(listener); // Cần truyền vào cùng một đối tượng hàm với lúc lắng nghe
```

---

### `getLaunchOptionsSync`

#### Mô tả
Lấy các tham số khi Mini Game khởi động nguội (Cold Start).

#### Giá trị trả về
Trả về một `Object` chứa các tham số khởi động:

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `scene` | `string` | Giá trị ngữ cảnh (Scene value) khởi động Mini Game |
| `query` | `Record<string, string>` | Các tham số query khi khởi động Mini Game |

#### Bảng giá trị ngữ cảnh (`scene`)
| Ngữ cảnh (Scene) | Mô tả (Description) |
| :--- | :--- |
| `scan_qrcode` | Quét mã QR để vào game |
| `search` | Tìm kiếm |
| `anchor` | Gắn liên kết từ Video / Livestream (Anchor) |
| `center` | Lối vào từ thanh bên / Trung tâm Mini Game (Sidebar / Center) |
| `ads` | Quảng cáo |
| `desktop_shortcut` | Phím tắt ngoài màn hình chính (Desktop shortcut) |
| `dm_sharing` | Lối vào từ tin nhắn trực tiếp (Direct Message) |
| `minis_link` | Truy cập thông qua Minis link |

#### Mã mẫu (Example Code)
```javascript
const launchOptions = TTMinis.game.getLaunchOptionsSync();
console.log('Khởi động với scene:', launchOptions.scene);
console.log('Query parameters:', launchOptions.query);
```

---

### `getEnterOptionsSync`

#### Mô tả
Lấy các tham số khi mở Mini Game (bao gồm cả khởi động nguội - Cold Start và khởi động nóng - Hot Start).

#### Giá trị trả về
Trả về một `Object` chứa các tham số khởi động:

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `scene` | `string` | Giá trị ngữ cảnh (Scene value) khởi động Mini Game |
| `query` | `Record<string, string>` | Các tham số query khi khởi động Mini Game |

#### Mã mẫu (Example Code)
```javascript
const enterOptions = TTMinis.game.getEnterOptionsSync();
console.log('Mở game với scene:', enterOptions.scene);
console.log('Query parameters:', enterOptions.query);
```

---

## 4. Tải gói phân đoạn (Subpackage Loading)

### `preDownloadSubpackage`

#### Mô tả
Kích hoạt tải trước gói phân đoạn (Pre-download subpackage).

#### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `name` | `string` | | **Có** | Tên của gói phân đoạn (Subpackage name) |
| `success` | `function` | | **Có** | Hàm callback khi tải gói phân đoạn thành công |
| `fail` | `function` | | **Có** | Hàm callback khi tải gói phân đoạn thất bại |
| `complete` | `function` | | **Có** | Hàm callback khi kết thúc quá trình tải (luôn thực thi dù thành công hay thất bại) |

#### Giá trị trả về
* **`PreDownloadSubpackageTask`**: Đối tượng tác vụ tải trước gói phân đoạn, dùng để theo dõi trạng thái và tiến độ tải.

#### Mã mẫu (Example Code)
```javascript
var task = TTMinis.game.preDownloadSubpackage({
  name: "ModuleA",
  success(res) {
    console.log("Tải trước phân gói thành công", res);
    // Thực thi nạp gói phân đoạn
    TTMinis.game.loadSubpackage({
      name: "ModuleA",
      success(res) {
        console.log("Nạp phân gói thành công:", res);
      },
    });
  },
  fail(res) {
    console.log("Tải trước phân gói thất bại:", res);
  }
});

task.onProgressUpdate((res) => {
  console.log('Tiến độ tải:', res.progress); // Lắng nghe tiến độ tải qua onProgressUpdate
  console.log('Đã tải (bytes):', res.totalBytesWritten);
  console.log('Tổng dung lượng dự kiến (bytes):', res.totalBytesExpectedToWrite);
});
```

---

### `loadSubpackage`

#### Mô tả
Kích hoạt nạp/tải gói phân đoạn (Load subpackage).

#### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `name` | `string` | | **Có** | Tên của gói phân đoạn |
| `success` | `function` | | **Có** | Hàm callback khi tải gói phân đoạn thành công |
| `fail` | `function` | | **Có** | Hàm callback khi tải gói phân đoạn thất bại |
| `complete` | `function` | | **Có** | Hàm callback khi kết thúc quá trình tải (luôn thực thi dù thành công hay thất bại) |

#### Giá trị trả về
* **`LoadSubpackageTask`**: Đối tượng tác vụ nạp gói phân đoạn, dùng để lấy trạng thái nạp gói phân đoạn.

> **Lưu ý quan trọng**:  
> Sự khác biệt giữa `TTMinis.game.preDownloadSubpackage` và `TTMinis.game.loadSubpackage`:  
> - `TTMinis.game.preDownloadSubpackage`: Chỉ tải về file gói mã nguồn mà **không tự động thực thi** mã.  
> - `TTMinis.game.loadSubpackage`: Sau khi tải xong gói mã nguồn sẽ **tự động thực thi mã**.

---

### `LoadSubpackageTask`

Đối tượng tác vụ nạp gói phân đoạn.

#### `.onProgressUpdate(listener)`
Lắng nghe sự kiện thay đổi tiến độ nạp/tải gói phân đoạn.

* **Tham số:** `listener` (`function`) - Hàm lắng nghe sự kiện thay đổi tiến độ.
* **Tham số của callback `listener` (`Object res`):**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `progress` | `number` | Phần trăm tiến độ tải gói phân đoạn (0 - 100) |
| `totalBytesWritten` | `number` | Dung lượng dữ liệu đã tải xuống (đơn vị: Bytes) |
| `totalBytesExpectedToWrite` | `number` | Tổng dung lượng dự kiến cần tải (đơn vị: Bytes) |

---

### `PreDownloadSubpackageTask`

Đối tượng tác vụ tải trước gói phân đoạn.

#### `.onProgressUpdate(listener)`
Lắng nghe sự kiện thay đổi tiến độ tải trước gói phân đoạn.

* **Tham số:** `listener` (`function`) - Hàm lắng nghe sự kiện thay đổi tiến độ.
* **Tham số của callback `listener` (`Object res`):**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `progress` | `number` | Phần trăm tiến độ tải gói phân đoạn (0 - 100) |
| `totalBytesWritten` | `number` | Dung lượng dữ liệu đã tải xuống (đơn vị: Bytes) |
| `totalBytesExpectedToWrite` | `number` | Tổng dung lượng dự kiến cần tải (đơn vị: Bytes) |

---

## 5. Quản lý phiên bản (Version Management)

### `getUpdateManager`

#### Mô tả
Lấy đối tượng quản lý phiên bản toàn cục duy nhất (`UpdateManager`). Ứng dụng Client sẽ tự động kiểm tra xem có phiên bản mới hay không khi khởi động Mini Game, nhà phát triển không cần phải tự phát động kiểm tra phiên bản thủ công.

#### Tham số
Không có (`None`).

#### Giá trị trả về
* **`UpdateManager`**: Đối tượng quản lý phiên bản (Singleton). Các lần gọi lặp lại trong cùng môi trường runtime JavaScript sẽ trả về cùng một đối tượng.

#### Mã mẫu (Example Code)
```javascript
const updateManager = TTMinis.game.getUpdateManager();
```

---

### `UpdateManager`

Đối tượng dùng để lắng nghe kết quả kiểm tra phiên bản và trạng thái sẵn sàng của gói cập nhật, đồng thời áp dụng bản cập nhật sau khi gói mới đã chuẩn bị xong.

#### `.onCheckForUpdate(listener)`

##### Mô tả
Lắng nghe sự kiện hoàn thành kiểm tra phiên bản. Việc đăng ký lắng nghe này sẽ không chủ động kích hoạt kiểm tra phiên bản mới.

##### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện kiểm tra phiên bản hoàn tất.

##### Tham số callback của `listener` (`Object res`)

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `hasUpdate` | `boolean` | Cho biết có phiên bản mới hay không (`true`: có phiên bản mới, `false`: hiện tại đã là phiên bản mới nhất). |

##### Giá trị trả về
Không có (`void`).

##### Mã mẫu (Example Code)
```javascript
const updateManager = TTMinis.game.getUpdateManager();

updateManager.onCheckForUpdate((res) => {
  console.log('Có phiên bản mới hay không:', res.hasUpdate);
});
```

---

#### `.onUpdateReady(listener)`

##### Mô tả
Lắng nghe sự kiện gói cập nhật phiên bản mới đã được chuẩn bị xong (tải về hoàn tất). Sau khi nhận được sự kiện này, bạn có thể gọi `applyUpdate()` để áp dụng phiên bản mới.

##### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện phiên bản mới đã sẵn sàng (callback không có tham số).

##### Giá trị trả về
Không có (`void`).

##### Mã mẫu (Example Code)
```javascript
const updateManager = TTMinis.game.getUpdateManager();

updateManager.onUpdateReady(() => {
  console.log('Phiên bản mới đã sẵn sàng');
});
```

---

#### `.onUpdateFailed(listener)`

##### Mô tả
Lắng nghe sự kiện chuẩn bị phiên bản mới thất bại (tải gói cập nhật thất bại). Sau khi nhận được sự kiện này, Mini Game có thể tiếp tục sử dụng phiên bản hiện tại.

##### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện chuẩn bị phiên bản mới thất bại (callback không có tham số).

##### Giá trị trả về
Không có (`void`).

##### Mã mẫu (Example Code)
```javascript
const updateManager = TTMinis.game.getUpdateManager();

updateManager.onUpdateFailed(() => {
  console.warn('Chuẩn bị phiên bản mới thất bại, tiếp tục sử dụng phiên bản hiện tại');
});
```

---

#### `.applyUpdate()`

##### Mô tả
Áp dụng phiên bản mới đã chuẩn bị sẵn và khởi động lại Mini Game. **Vui lòng chỉ gọi phương thức này sau khi đã nhận được sự kiện `onUpdateReady`**, đồng thời cần lưu tiến trình game hoặc các trạng thái nghiệp vụ cần thiết trước khi gọi.

##### Tham số
Không có (`None`).

##### Giá trị trả về
Không có (`void`).

##### Mã mẫu (Example Code)
```javascript
const updateManager = TTMinis.game.getUpdateManager();

updateManager.onUpdateReady(() => {
  // Lưu tiến trình game và nhận xác nhận của người chơi trước khi áp dụng cập nhật
  updateManager.applyUpdate();
});
```

---

#### Lưu ý quan trọng khi Quản lý phiên bản:
- **Thời điểm đăng ký**: Khuyến nghị đăng ký các sự kiện lắng nghe quản lý phiên bản ngay tại cổng vào của game (Game entry point), trước khi nạp scene đầu tiên.
- **Tránh trùng lặp**: Tránh việc đăng ký nhiều lần cùng một loại listener trong nhiều scene hoặc trong các đoạn mã khởi tạo chạy lặp lại.
- **Trải nghiệm người chơi**: Không nên khởi động lại game ngay lập tức khi người chơi đang trong trận đấu thời gian thực (PvP/PvE), đang thanh toán hoặc trong tiến trình nhận thưởng; hãy chọn thời điểm nghiệp vụ thích hợp để thông báo và yêu cầu người dùng cập nhật.

---

## 6. Chia sẻ (Share)

### `shareToStory`

#### Mô tả
Chia sẻ nội dung lên Story (TikTok Story).

#### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `title` | `string` | | Không | Tiêu đề chia sẻ |
| `desc` | `string` | | Không | Mô tả chia sẻ |
| `imageUrl` | `string` | | Không | Đường dẫn hình ảnh chia sẻ |
| `query` | `string` | | Không | Chuỗi tham số tùy chỉnh (query) chia sẻ |
| `success` | `function` | | Không | Hàm callback khi gọi API thành công |
| `fail` | `function` | | Không | Hàm callback khi gọi API thất bại |
| `complete` | `function` | | Không | Hàm callback khi kết thúc lời gọi (luôn thực thi dù thành công hay thất bại) |

---

### `shareAppMessage`

#### Mô tả
Chủ động mở giao diện chuyển tiếp/chia sẻ (Forward), chuyển vào màn hình lựa chọn danh bạ / bạn bè.

#### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `title` | `string` | | Không | Tiêu đề chuyển tiếp (Tạm thời chưa hỗ trợ API tùy chỉnh, lấy giá trị cấu hình trên nền tảng) |
| `imageUrl` | `string` | | Không | Đường dẫn hình ảnh hiển thị khi chuyển tiếp (Tạm thời chưa hỗ trợ API tùy chỉnh, lấy giá trị cấu hình trên nền tảng) |
| `query` | `string` | | Không | Chuỗi truy vấn (query). Khi người chơi vào game qua tin nhắn chuyển tiếp này, có thể lấy tham số query thông qua `TTMinis.game.getLaunchOptionsSync()` hoặc `TTMinis.game.onShow()`. |
| `subTitle` | `string` | | Không | Phụ đề chuyển tiếp (Tạm thời chưa hỗ trợ API tùy chỉnh, lấy giá trị cấu hình trên nền tảng) |
| `success` | `function` | | Không | Hàm callback khi gọi API thành công |
| `fail` | `function` | | Không | Hàm callback khi gọi API thất bại |
| `complete` | `function` | | Không | Hàm callback khi kết thúc lời gọi (luôn thực thi dù thành công hay thất bại) |

---

### `onCopyUrl`

#### Mô tả
Lắng nghe sự kiện khi người dùng bấm vào nút **"Sao chép liên kết" (Copy Link)** trên thanh menu ở góc trên bên phải màn hình.

#### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện khi người dùng bấm nút sao chép liên kết.

#### Callback / Giá trị trả về của `listener`
**`Object res`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- |
| `title` | `string` | Không | Tiêu đề chia sẻ |
| `desc` | `string` | Không | Mô tả chia sẻ |
| `imageUrl` | `string` | Không | Hình ảnh chia sẻ |
| `query` | `string` | Không | Chuỗi tham số tùy chỉnh (query) chia sẻ |
| `success` | `function` | Không | Hàm callback khi gọi thành công |
| `fail` | `function` | Không | Hàm callback khi gọi thất bại |
| `complete` | `function` | Không | Hàm callback khi kết thúc lời gọi |

#### Mã mẫu (Example Code)
```javascript
// Gắn tham số chia sẻ khi người dùng sao chép URL
TTMinis.game.onCopyUrl(() => {
  return { query: 'a=1&b=2' };
});

// Hủy gắn tham số chia sẻ
TTMinis.game.offCopyUrl();
```

---

### `offCopyUrl`

#### Mô tả
Hủy bỏ tất cả các hàm lắng nghe sự kiện người dùng bấm nút "Sao chép liên kết" trên menu góc trên bên phải.

#### Mã mẫu (Example Code)
```javascript
// Gắn tham số chia sẻ
TTMinis.game.onCopyUrl(() => {
  return { query: 'a=1&b=2' };
});

// Hủy bỏ tất cả lắng nghe sao chép liên kết
TTMinis.game.offCopyUrl();
```

---

## 7. Giao diện (Interface / UI)

### Menu

#### `getMenuButtonBoundingClientRect`
> Hỗ trợ từ Base Library phiên bản **0.3.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Lấy thông tin vị trí bố cục của nút Menu (nút Capsule ở góc trên bên phải màn hình). Hệ tọa độ lấy gốc `(0, 0)` tại góc trên cùng bên trái của màn hình.

##### Giá trị trả về
Trả về một `Object` chứa thông tin vị trí bố cục của nút Menu:

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `width` | `number` | Chiều rộng nút (đơn vị: px) |
| `height` | `number` | Chiều cao nút (đơn vị: px) |
| `top` | `number` | Tọa độ mép trên (Top, đơn vị: px) |
| `right` | `number` | Tọa độ mép phải (Right, đơn vị: px) |
| `bottom` | `number` | Tọa độ mép dưới (Bottom, đơn vị: px) |
| `left` | `number` | Tọa độ mép trái (Left, đơn vị: px) |

##### Mã mẫu (Example Code)
```javascript
const res = TTMinis.game.getMenuButtonBoundingClientRect();

console.log('Chiều rộng:', res.width);
console.log('Chiều cao:', res.height);
console.log('Tọa độ trên (top):', res.top);
console.log('Tọa độ phải (right):', res.right);
console.log('Tọa độ dưới (bottom):', res.bottom);
console.log('Tọa độ trái (left):', res.left);
```

---

## 8. Thiết bị (Device)

### Bàn phím (Keyboard)
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

#### `showKeyboard`

##### Mô tả
Hiển thị bàn phím ảo trên màn hình thiết bị.

##### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `defaultValue` | `string` | | **Có** | Giá trị mặc định hiển thị trong ô nhập liệu của bàn phím |
| `maxLength` | `number` | | **Có** | Độ dài tối đa của văn bản nhập |
| `multiple` | `boolean` | | **Có** | Có cho phép nhập nhiều dòng (Multiline) hay không |
| `confirmHold` | `boolean` | | **Có** | Có giữ bàn phím tiếp tục hiển thị khi bấm nút Xác nhận (Confirm) hay không |
| `confirmType` | `string` | | **Có** | Loại nút confirm ở góc dưới bên phải bàn phím (chỉ ảnh hưởng đến chữ hiển thị trên nút) |
| `keyboardType` | `string` | | **Có** | Loại bàn phím. Mặc định là kiểu văn bản (`text`), Client phiên bản 8.0.57 trở lên hỗ trợ bàn phím số (`number`) |
| `success` | `function` | | Không | Hàm callback khi gọi API thành công |
| `fail` | `function` | | Không | Hàm callback khi gọi API thất bại |
| `complete` | `function` | | Không | Hàm callback khi kết thúc lời gọi (luôn thực thi dù thành công hay thất bại) |

---

#### `updateKeyboard`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Cập nhật nội dung trong ô nhập liệu của bàn phím. **Chỉ có hiệu lực khi bàn phím đang ở trạng thái hiển thị (mở lên)**.

##### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `value` | `string` | | **Có** | Giá trị hiện tại cần cập nhật vào ô nhập liệu của bàn phím |
| `success` | `function` | | Không | Hàm callback khi gọi API thành công |
| `fail` | `function` | | Không | Hàm callback khi gọi API thất bại |
| `complete` | `function` | | Không | Hàm callback khi kết thúc lời gọi (luôn thực thi dù thành công hay thất bại) |

---

#### `hideKeyboard`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Ẩn / đóng bàn phím ảo.

##### Tham số
**`Object object`**

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mặc định (Default) | Bắt buộc (Required) | Mô tả (Description) |
| :--- | :--- | :--- | :--- | :--- |
| `success` | `function` | | Không | Hàm callback khi gọi API thành công |
| `fail` | `function` | | Không | Hàm callback khi gọi API thất bại |
| `complete` | `function` | | Không | Hàm callback khi kết thúc lời gọi (luôn thực thi dù thành công hay thất bại) |

---

#### `onKeyboardInput`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Lắng nghe sự kiện người dùng nhập văn bản trên bàn phím.

##### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện nhập bàn phím.

##### Tham số callback của `listener` (`Object res`)

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `value` | `string` | Giá trị văn bản hiện tại sau khi nhập |

---

#### `offKeyboardInput`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Hủy bỏ hàm lắng nghe sự kiện nhập văn bản trên bàn phím.

##### Tham số
* **`listener`** (`function`): Hàm listener đã truyền vào trong `onKeyboardInput`. Nếu không truyền tham số này sẽ hủy bỏ tất cả các hàm lắng nghe.

##### Mã mẫu (Example Code)
```javascript
const listener = function (res) {
  console.log(res);
};

TTMinis.game.onKeyboardInput(listener);
TTMinis.game.offKeyboardInput(listener); // Cần truyền vào cùng một đối tượng hàm với lúc lắng nghe
```

---

#### `onKeyboardHeightChange`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Lắng nghe sự kiện thay đổi chiều cao của bàn phím (khi bàn phím trượt lên/xuống).

##### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện thay đổi chiều cao bàn phím.

##### Tham số callback của `listener` (`Object res`)

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `height` | `number` | Chiều cao của bàn phím (đơn vị: px) |

---

#### `offKeyboardHeightChange`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Hủy bỏ hàm lắng nghe sự kiện thay đổi chiều cao của bàn phím.

##### Tham số
* **`listener`** (`function`): Hàm listener đã truyền vào trong `onKeyboardHeightChange`. Nếu không truyền tham số này sẽ hủy bỏ tất cả các hàm lắng nghe.

##### Mã mẫu (Example Code)
```javascript
const listener = function (res) {
  console.log(res);
};

TTMinis.game.onKeyboardHeightChange(listener);
TTMinis.game.offKeyboardHeightChange(listener); // Cần truyền vào cùng một đối tượng hàm với lúc lắng nghe
```

---

#### `onKeyboardConfirm`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Lắng nghe sự kiện người dùng bấm vào nút Confirm (Xác nhận / Hoàn tất) trên bàn phím.

##### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện người dùng bấm nút Confirm trên bàn phím.

##### Tham số callback của `listener` (`Object res`)

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `value` | `string` | Giá trị văn bản hiện tại trong ô nhập liệu |

---

#### `offKeyboardConfirm`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Hủy bỏ hàm lắng nghe sự kiện khi người dùng bấm vào nút Confirm trên bàn phím.

##### Tham số
* **`listener`** (`function`): Hàm listener đã truyền vào trong `onKeyboardConfirm`. Nếu không truyền tham số này sẽ hủy bỏ tất cả các hàm lắng nghe.

##### Mã mẫu (Example Code)
```javascript
const listener = function (res) {
  console.log(res);
};

TTMinis.game.onKeyboardConfirm(listener);
TTMinis.game.offKeyboardConfirm(listener); // Cần truyền vào cùng một đối tượng hàm với lúc lắng nghe
```

---

#### `onKeyboardComplete`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Lắng nghe sự kiện bàn phím bị thu gọn / đóng lại.

##### Tham số
* **`listener`** (`function`): Hàm lắng nghe sự kiện bàn phím thu gọn.

##### Tham số callback của `listener` (`Object res`)

| Thuộc tính (Property) | Kiểu dữ liệu (Type) | Mô tả (Description) |
| :--- | :--- | :--- |
| `value` | `string` | Giá trị văn bản hiện tại trong ô nhập liệu khi bàn phím đóng |

---

#### `offKeyboardComplete`
> Hỗ trợ từ Base Library phiên bản **0.6.0** trở lên. Cần xử lý tương thích đối với các phiên bản thấp hơn.

##### Mô tả
Hủy bỏ hàm lắng nghe sự kiện khi bàn phím thu gọn / đóng lại.

##### Tham số
* **`listener`** (`function`): Hàm listener đã truyền vào trong `onKeyboardComplete`. Nếu không truyền tham số này sẽ hủy bỏ tất cả các hàm lắng nghe.

##### Mã mẫu (Example Code)
```javascript
const listener = function (res) {
  console.log(res);
};

TTMinis.game.onKeyboardComplete(listener);
TTMinis.game.offKeyboardComplete(listener); // Cần truyền vào cùng một đối tượng hàm với lúc lắng nghe
```
