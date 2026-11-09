# Android

[← RiseOn.Vibration](../../../README.md)

Rung qua `Vibrator` của hệ thống, code ở [AndroidVibration.cs](AndroidVibration.cs).

Code nằm trong assembly `RiseOn.Vibration.Android`, chỉ bật cho Android và
Editor: build Android có nó, build nền tảng khác thì không. Editor vẫn biên dịch
để bắt lỗi, nhưng lúc chạy nó chỉ tự cài vào `Vibration` khi
`Application.platform` là Android.

Gọi Java qua `AndroidJavaObject` vốn biên dịch được ở mọi nền tảng, nên ở đây
tách assembly không bắt buộc như bên [iOS](../iOS/README.md). Tách để hai nền
tảng đối xứng, assembly chính chỉ còn API.

## Không để linker bỏ assembly

Không assembly nào tham chiếu tới `RiseOn.Vibration.Android`, nên
[AssemblyInfo.cs](AssemblyInfo.cs) khai `[assembly: AlwaysLinkAssembly]`. Thiếu
dòng này, UnityLinker bỏ cả assembly khỏi bản build khi pack cài dạng package,
kể cả ở mức stripping Minimal: hàm cài không chạy và `Vibration.Play` không làm
gì.

## Khởi động

Lúc khởi động, code chỉ đăng ký với `Vibration`. Lần rung đầu tiên mới gọi sang
Java để lấy `Vibrator`: từ Android 12 (API 31) qua
`VibratorManager.getDefaultVibrator()`, cũ hơn qua
`getSystemService("vibrator")`. Máy không có bộ rung (`hasVibrator()` trả
false) thì từ đó mọi lần gọi đều bỏ qua.

## Luồng riêng

`Vibrator.vibrate` gọi sang dịch vụ rung của hệ thống và chờ nó trả lời. Đo
trên Samsung A30s (Android 11), mỗi lần gọi mất 4 đến 60 ms, lần đầu kèm phần
khởi động trên 150 ms. Gọi trên main thread thì khung hình phải chờ chừng ấy.

Vì vậy `Play` trên main thread chỉ đổi nhịp sang dạng sóng (xem dưới), ghi yêu
cầu vào một chỗ chờ rồi trả về ngay. Mọi lời gọi sang Java, kể cả phần khởi động,
chạy trên một thread nền riêng tên `RiseOn.Vibration`, tạo lúc rung lần đầu và
gắn vào JVM bằng `AndroidJNI.AttachCurrentThread`. Riêng `currentActivity` được
đọc trên main thread trước khi thread chạy.

Chỗ chờ chỉ giữ một yêu cầu: gọi `Play` dồn dập nhanh hơn hệ thống kịp rung thì
yêu cầu mới thay yêu cầu chưa chạy, vì yêu cầu cũ có rung cũng đã trễ.

## Từ nhịp sang dạng sóng

Mọi kiểu gọi đều thành dạng sóng: mảng `timings` xen kẽ tắt/bật tính bằng mili
giây, bắt đầu bằng tắt, kèm mảng cường độ từ 1 đến 255 (`intensity × 255`).

- Android 8 (API 26) trở lên: `VibrationEffect.createWaveform(timings, amplitudes, -1)`.
- Cũ hơn: `vibrate(timings, -1)`, không có cường độ.
- Nhịp có cường độ 0 hoặc thời lượng 0 được gộp vào khoảng nghỉ.

Máy không chỉnh được cường độ (`hasAmplitudeControl()` trả false) thì đoạn bật
nào cũng rung hết cỡ, chỉ còn thời lượng tạo khác biệt.

## Mức có sẵn

Ba mức là ba nhịp đơn, dựng sẵn một lần:

| Mức | Thời lượng | Cường độ |
|---|---|---|
| `Light` | 40 ms | 0,5 |
| `Medium` | 55 ms | 0,75 |
| `Heavy` | 80 ms | 1 |

Không dùng hiệu ứng có sẵn `TICK`, `CLICK`, `HEAVY_CLICK`, vì thứ tự mạnh nhẹ
của chúng do hãng quyết. Ví dụ trên Oppo CPH2121 (Android 12), `CLICK` chạy
34 ms mà không cảm nhận được, trong khi `TICK` thì có.

Thời lượng chọn bằng tay trên chính máy đó, loại không chỉnh được cường độ:
nhịp 20 ms gần như không cảm thấy, 30 ms mới rung rất nhẹ. Nhịp dưới khoảng
30 ms coi như không rung trên những máy như vậy.

## Quyền

[RiseOnVibration.androidlib](RiseOnVibration.androidlib) chỉ có
`AndroidManifest.xml` khai quyền `android.permission.VIBRATE`. Đây là quyền
thường: được cấp lúc cài, không hỏi người chơi. Gradle gộp manifest này vào app
khi build.
