# Lịch sử thay đổi

Mọi thay đổi đáng kể của `com.riseon.vibration` được ghi ở đây. Định dạng theo
[Keep a Changelog](https://keepachangelog.com/vi/1.1.0/), đánh số theo
[Semantic Versioning](https://semver.org/lang/vi/).

## [1.0.0] - 2026-09-25

Bản phát hành đầu tiên.

### Thêm

- `Vibration.Play` với các overload: mức có sẵn `VibrationLevel` (`Light`,
  `Medium`, `Heavy`), một nhịp (thời lượng, cường độ), chuỗi nhịp
  `VibrationPulse` và `VibrationPattern` serializable. Công tắc
  `Vibration.Enabled`.
- Hiện thực cho Android (`Vibrator`, package tự khai quyền `VIBRATE`) và iOS
  (UIKit cho mức có sẵn, Core Haptics cho nhịp).
- Giấy phép MIT.
