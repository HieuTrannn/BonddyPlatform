using System.Globalization;
using System.Net;

namespace BonddyPlatform.Services.Options;

public static class EmailTemplates
{
    public static string BuildOtpEmailHtml(
        string otpCode,
        int expiresInMinutes,
        string purposeTitle,
        string? recipientName = null)
    {
        var safeOtp = WebUtility.HtmlEncode(otpCode ?? string.Empty);
        var safePurpose = WebUtility.HtmlEncode(purposeTitle ?? "Xác thực");
        var safeName = WebUtility.HtmlEncode(recipientName ?? string.Empty);
        var expiresText = expiresInMinutes <= 0
            ? "Vui lòng sử dụng mã này ngay."
            : string.Create(CultureInfo.InvariantCulture, $"Mã sẽ hết hạn sau {expiresInMinutes} phút.");

        var greetingLine = string.IsNullOrWhiteSpace(safeName)
            ? "Xin chào,"
            : $"Xin chào {safeName},";

        // Gmail-friendly: table layout + inline CSS, no external assets.
        return $@"<!doctype html>
<html lang=""vi"">
<head>
  <meta http-equiv=""Content-Type"" content=""text/html; charset=UTF-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
  <title>{safePurpose}</title>
</head>
<body style=""margin:0;padding:0;background-color:#f4f6fb;"">
  <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""background-color:#f4f6fb;"">
    <tr>
      <td align=""center"" style=""padding:24px 12px;"">
        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""600"" style=""width:600px;max-width:600px;"">
          <tr>
            <td style=""padding:0 0 12px 0;"">
              <div style=""font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;color:#6b7280;text-align:center;"">
                Bonddy Platform
              </div>
            </td>
          </tr>

          <tr>
            <td style=""background:#ffffff;border-radius:14px;padding:24px;border:1px solid #e5e7eb;"">
              <div style=""font-family:Arial,Helvetica,sans-serif;font-size:18px;line-height:26px;color:#111827;font-weight:700;margin:0 0 6px 0;"">
                {safePurpose}
              </div>
              <div style=""font-family:Arial,Helvetica,sans-serif;font-size:14px;line-height:22px;color:#374151;margin:0 0 16px 0;"">
                {greetingLine}<br/>
                Đây là mã OTP của bạn để hoàn tất thao tác. {WebUtility.HtmlEncode(expiresText)}
              </div>

              <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" border=""0"" width=""100%"" style=""margin:0 0 16px 0;"">
                <tr>
                  <td align=""center"" style=""padding:14px 0;"">
                    <div style=""display:inline-block;background:#111827;color:#ffffff;border-radius:12px;padding:14px 18px;font-family:Arial,Helvetica,sans-serif;font-size:26px;letter-spacing:6px;font-weight:800;"">
                      {safeOtp}
                    </div>
                  </td>
                </tr>
              </table>

              <div style=""font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:#6b7280;margin:0;"">
                Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email.
              </div>

              <div style=""height:16px;line-height:16px;font-size:1px;"">&nbsp;</div>
              <div style=""font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;color:#9ca3af;margin:0;"">
                Email này được gửi tự động. Vui lòng không trả lời trực tiếp.
              </div>
            </td>
          </tr>

          <tr>
            <td style=""padding:12px 0 0 0;"">
              <div style=""font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;color:#9ca3af;text-align:center;"">
                © {DateTime.UtcNow.Year} Bonddy Platform. All rights reserved.
              </div>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}

