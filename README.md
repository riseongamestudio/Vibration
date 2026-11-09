# RiseOn.Vibration

Rung ngắn cho Android và iOS qua một lớp tĩnh: gọi nhanh bằng mức có sẵn, hoặc
tự định nghĩa từng nhịp.

Package `com.riseon.vibration`, namespace `RiseOn.Vibration`. Không phụ thuộc
package nào khác, chỉ dùng module có sẵn của Unity.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Cài đặt](#cài-đặt)
- [Tổng quan](#tổng-quan)
- [Hướng dẫn nhanh](#hướng-dẫn-nhanh)
- [Thành phần](#thành-phần)
- [Lịch sử thay đổi](#lịch-sử-thay-đổi)
- [Giấy phép](#giấy-phép)

## Yêu cầu

| Phụ thuộc | Cách có | Dùng cho |
|---|---|---|
| Unity 6000.3 | | Bản đang dùng để phát triển |
| `com.unity.modules.androidjni` | Tự cài theo `package.json`, có sẵn trong Unity | Gọi `Vibrator` của Android |

Quyền `VIBRATE` của Android và framework Core Haptics của iOS do package tự
khai, không phải thêm gì vào project.

## Cài đặt

**OpenUPM** (khuyên dùng): thêm registry OpenUPM với scope `com.riseon` vào
`Packages/manifest.json`, rồi thêm package:

```json
{
  "scopedRegistries": [
    {
      "name": "package.openupm.com",
      "url": "https://package.openupm.com",
      "scopes": ["com.riseon"]
    }
  ],
  "dependencies": {
    "com.riseon.vibration": "1.0.1"
  }
}
```

**Git URL**:

```
https://github.com/riseongamestudio/Vibration.git
```

**Thư mục local**: `"com.riseon.vibration": "file:D:/path/to/Vibration"`.

## Tổng quan

Có hai cách gọi, đều chạy trên cả Android và iOS:

- **Mức có sẵn** `VibrationLevel`: `Light`, `Medium`, `Heavy`. Ba mức ứng với
  ba mức chạm của hệ thống iOS; trên Android là ba nhịp dựng sẵn của pack.
- **Tự định nghĩa**: một nhịp (thời lượng, cường độ), hoặc chuỗi nhịp
  `VibrationPulse` gói trong `VibrationPattern`. Mỗi nhịp có khoảng nghỉ trước
  nó, thời lượng tính bằng mili giây và cường độ từ 0 đến 1.

| | Mức có sẵn | Nhịp, chuỗi nhịp |
|---|---|---|
| Android | Dạng sóng riêng của pack (thứ tự mạnh nhẹ giữ đúng trên mọi máy) | Dạng sóng theo đúng các nhịp |
| iOS | Cú chạm của hệ thống (`UIImpactFeedbackGenerator`) | Core Haptics, từ iPhone 8. iPhone 7 thay mỗi nhịp bằng một cú chạm |
| Editor, nền tảng khác | Không làm gì | Không làm gì |

Máy không có bộ rung (phần lớn máy tính bảng, iPad) cũng không làm gì. Nhiều
máy Android không chỉnh được cường độ: mọi nhịp rung hết cỡ, chỉ khác nhau ở
thời lượng.

Mỗi nền tảng nằm trong một assembly riêng và tự cài vào `Vibration` lúc khởi
động; assembly chính chỉ có API.

## Hướng dẫn nhanh

```csharp
using RiseOn.Vibration;

Vibration.Play(VibrationLevel.Light);   // mức có sẵn
Vibration.Play(50);                     // một nhịp 50 ms, hết cỡ
Vibration.Play(50, 0.6f);               // một nhịp 50 ms, cường độ 0,6

// Chuỗi nhịp: 40 ms, nghỉ 100 ms, rồi 60 ms.
Vibration.Play(new VibrationPulse(40), new VibrationPulse(60, 1f, delayMs: 100));
```

Rung dùng nhiều lần thì dựng `VibrationPattern` một lần rồi giữ lại. Nó cũng là
field serializable, chỉnh từng nhịp được ngay trong Inspector:

```csharp
[SerializeField] VibrationPattern hit;

Vibration.Play(hit);
```

`Vibration.Enabled` bật/tắt mọi lần rung, mặc định bật. Package không lưu giá
trị này: game đọc cài đặt của mình lúc khởi động rồi gán vào. Mọi hàm gọi từ
main thread.

## Thành phần

| Thành phần | Assembly | Việc | Chi tiết |
|---|---|---|---|
| `Vibration`, `VibrationLevel`, `VibrationPulse`, `VibrationPattern` | `RiseOn.Vibration` | API chung | README này |
| Android | `RiseOn.Vibration.Android` | Rung qua `Vibrator`, đổi mọi kiểu gọi thành dạng sóng | [Runtime/Platforms/Android](Runtime/Platforms/Android/README.md) |
| iOS | `RiseOn.Vibration.iOS` | Rung qua UIKit và Core Haptics | [Runtime/Platforms/iOS](Runtime/Platforms/iOS/README.md) |

## Lịch sử thay đổi

Xem [CHANGELOG.md](CHANGELOG.md).

## Giấy phép

MIT, xem [LICENSE.md](LICENSE.md).
