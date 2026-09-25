# iOS

[← RiseOn.Vibration](../../../README.md)

Rung qua UIKit và Core Haptics. C# ở [IOSVibration.cs](IOSVibration.cs), native
ở [ROVibration.mm](ROVibration.mm).

## Vì sao nằm assembly riêng

`IOSVibration` gọi sang native bằng `[DllImport("__Internal")]`. Trên iOS và
WebGL, IL2CPP nối hàm `__Internal` ngay lúc build, nên nếu dòng `extern` nằm ở
assembly dùng chung thì build nền tảng không có `ROVibration.mm` sẽ báo thiếu
hàm.

Assembly `RiseOn.Vibration.iOS` chỉ bật cho iOS và Editor: build iOS có nó,
build nền tảng khác thì không. Editor vẫn biên dịch để bắt lỗi, nhưng lúc chạy
nó chỉ tự cài vào `Vibration` khi `Application.platform` là iOS.

Không assembly nào tham chiếu tới nó, nên [AssemblyInfo.cs](AssemblyInfo.cs)
khai `[assembly: AlwaysLinkAssembly]`, để UnityLinker không bỏ nó khỏi bản
build.

## Mức có sẵn

`VibrationLevel` 0, 1, 2 trùng giá trị với `UIImpactFeedbackStyleLight`,
`Medium`, `Heavy`, nên số đi thẳng sang `UIImpactFeedbackGenerator`. Đây là cú
chạm hệ thống đã tinh chỉnh, chạy từ iPhone 7. Mỗi bộ tạo được tạo ở lần dùng
đầu rồi giữ lại.

## Nhịp và chuỗi nhịp

`VibrationPulse` sang native nguyên mảng, không chép: struct chỉ có
`int, int, float` theo đúng thứ tự của `ROVibrationPulse`. Đổi field của struct
thì sửa cả hai bên.

- **Có Core Haptics** (iPhone 8 trở lên): mỗi nhịp là một `CHHapticEvent` liên
  tục, cường độ theo `intensity`, độ sắc cố định 0,5. Engine tự tắt khi rảnh và
  được bật lại mỗi lần phát. Framework `CoreHaptics` khai trong `.meta` của
  `ROVibration.mm`, Unity tự thêm vào project Xcode.
- **Không có Core Haptics** (iPhone 7): mỗi nhịp thành một cú chạm
  `impactOccurredWithIntensity:` đúng lúc nhịp bắt đầu. Thời lượng không giữ
  được.

## Khi nào không rung

- iPad và iPhone 6s trở xuống không có phần cứng rung kiểu này.
- Người chơi tắt *Rung hệ thống* (System Haptics) trong Cài đặt → Âm thanh & Cảm ứng.
- App đang thu âm.

Code Obj-C++ chưa được biên dịch trên máy Windows; kiểm trên Mac trước khi phát
hành.
