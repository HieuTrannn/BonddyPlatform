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

    public static string BuildNewTripRequestEmailHtml(
        string travelerName,
        string buddyName,
        string city,
        DateOnly startDate,
        TimeOnly startTime,
        int adults,
        int children,
        int durationHours,
        bool isInboxOnly,
        decimal? offeredPrice,
        string? buddyNote,
        Guid tripId,
        DateTime createdAtUtc,
        string baseWebUrl = "https://bonddy.vercel.app")
    {
        var safeTravelerName = WebUtility.HtmlEncode(travelerName ?? string.Empty);
        var safeBuddyName = WebUtility.HtmlEncode(buddyName ?? "A Bonddy buddy");
        var safeCity = WebUtility.HtmlEncode(city ?? string.Empty);
        var safeNote = string.IsNullOrWhiteSpace(buddyNote) ? null : WebUtility.HtmlEncode(buddyNote.Trim());

        var offerLine = isInboxOnly
            ? "This buddy prefers to discuss the price directly in chat."
            : $"Proposed price: <strong>{(offeredPrice ?? 0):0.##}</strong>";

        var requestUrl = $"{baseWebUrl.TrimEnd('/')}/trips/{tripId}/requests";

        var noteSection = safeNote == null
            ? string.Empty
            : $"""
                <tr>
                  <td style="padding:16px 24px 0;font-size:14px;color:#333;">
                    <strong>Message from buddy:</strong><br/>
                    <span style="white-space:pre-line;">{safeNote}</span>
                  </td>
                </tr>
              """;

        return $"""
<!DOCTYPE html>
<html lang="en">
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>New trip request</title>
  </head>
  <body style="margin:0;padding:0;background-color:#f5f5f7;font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif;">
    <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#f5f5f7;padding:24px 0;">
      <tr>
        <td align="center">
          <table width="600" cellpadding="0" cellspacing="0" style="background-color:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 8px 24px rgba(15,23,42,0.12);">
            <tr>
              <td style="background:linear-gradient(135deg,#ec4899,#8b5cf6);padding:20px 24px;color:#ffffff;">
                <table width="100%" cellpadding="0" cellspacing="0">
                  <tr>
                    <td style="font-size:20px;font-weight:600;">
                      Bonddy • New Trip Request
                    </td>
                    <td align="right" style="font-size:12px;opacity:0.85;">
                      {createdAtUtc:yyyy-MM-dd HH:mm} UTC
                    </td>
                  </tr>
                </table>
              </td>
            </tr>

            <tr>
              <td style="padding:20px 24px 4px;font-size:16px;color:#111827;">
                Hi {safeTravelerName},
              </td>
            </tr>
            <tr>
              <td style="padding:0 24px 12px;font-size:14px;color:#4b5563;line-height:1.6;">
                <strong>{safeBuddyName}</strong> has just sent a new request for your trip.
                You can review the details below and continue the conversation in chat.
              </td>
            </tr>

            <tr>
              <td style="padding:0 24px 16px;">
                <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;">
                  <tr>
                    <td colspan="2" style="background-color:#f9fafb;padding:12px 16px;font-size:14px;font-weight:600;color:#111827;">
                      Your trip
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:10px 16px;font-size:13px;color:#6b7280;width:30%;">Destination</td>
                    <td style="padding:10px 16px;font-size:13px;color:#111827;font-weight:500;">
                      {safeCity}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 16px 10px;font-size:13px;color:#6b7280;">Date &amp; time</td>
                    <td style="padding:0 16px 10px;font-size:13px;color:#111827;">
                      {startDate:yyyy-MM-dd} at {startTime}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 16px 10px;font-size:13px;color:#6b7280;">Group</td>
                    <td style="padding:0 16px 10px;font-size:13px;color:#111827;">
                      {adults} adults, {children} children • {durationHours} hours
                    </td>
                  </tr>
                </table>
              </td>
            </tr>

            <tr>
              <td style="padding:0 24px 16px;">
                <table width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;">
                  <tr>
                    <td colspan="2" style="background-color:#f9fafb;padding:12px 16px;font-size:14px;font-weight:600;color:#111827;">
                      Buddy's offer
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:10px 16px;font-size:13px;color:#6b7280;width:30%;">Buddy</td>
                    <td style="padding:10px 16px;font-size:13px;color:#111827;font-weight:500;">
                      {safeBuddyName}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 16px 12px;font-size:13px;color:#6b7280;">Offer</td>
                    <td style="padding:0 16px 12px;font-size:13px;color:#111827;">
                      {offerLine}
                    </td>
                  </tr>
                </table>
              </td>
            </tr>

            {noteSection}

            <tr>
              <td style="padding:20px 24px 16px;" align="center">
                <a href="{requestUrl}"
                   style="display:inline-block;background:linear-gradient(135deg,#ec4899,#8b5cf6);color:#ffffff;text-decoration:none;
                          padding:10px 24px;border-radius:999px;font-size:14px;font-weight:600;box-shadow:0 4px 12px rgba(236,72,153,0.45);">
                  View request &amp; chat with buddy
                </a>
              </td>
            </tr>

            <tr>
              <td style="padding:0 24px 20px;font-size:12px;color:#9ca3af;line-height:1.6;">
                If the button above does not work, copy and paste this link into your browser:<br/>
                <span style="color:#6b7280;">{requestUrl}</span>
              </td>
            </tr>

            <tr>
              <td style="background-color:#f9fafb;padding:12px 24px;font-size:11px;color:#9ca3af;text-align:center;">
                You are receiving this email because you created a trip on Bonddy and enabled notifications.
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>
""";
    }

    public static string BuildBuddyApprovedEmailHtml(string fullName, string firstName) => $"""
    <!DOCTYPE html>
    <html lang="vi">
    <head>
      <meta charset="UTF-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1.0" />
      <title>Chào mừng bạn đến với Bonddy</title>
    </head>
    <body style="margin:0;padding:0;background:#f4f6fb;font-family:'Segoe UI',Arial,sans-serif;">
      <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6fb;padding:40px 0;">
        <tr>
          <td align="center">
            <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);max-width:600px;width:100%;">

              <!-- HEADER -->
              <tr>
                <td style="background:linear-gradient(135deg,#1a1a2e 0%,#16213e 50%,#0f3460 100%);padding:48px 40px 40px;text-align:center;">
                  <div style="display:inline-block;background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.15);border-radius:12px;padding:12px 28px;margin-bottom:24px;">
                    <span style="color:#e2a84b;font-size:22px;font-weight:800;letter-spacing:2px;">BONDDY</span>
                  </div>
                  <h1 style="color:#ffffff;font-size:28px;font-weight:700;margin:0 0 8px;letter-spacing:-0.5px;">Chúc mừng, {fullName}!</h1>
                  <p style="color:rgba(255,255,255,0.7);font-size:15px;margin:0;">Yêu cầu đăng ký Buddy của bạn đã được chấp thuận</p>
                </td>
              </tr>

              <!-- GOLDEN BANNER -->
              <tr>
                <td style="background:linear-gradient(90deg,#e2a84b,#f5c97a,#e2a84b);padding:16px 40px;text-align:center;">
                  <p style="margin:0;color:#1a1a2e;font-size:14px;font-weight:700;letter-spacing:1px;text-transform:uppercase;">Bạn giờ đây là một Buddy chính thức</p>
                </td>
              </tr>

              <!-- BODY -->
              <tr>
                <td style="padding:44px 40px 32px;">
                  <p style="color:#2d2d2d;font-size:16px;line-height:1.7;margin:0 0 20px;">Xin chào <strong>{firstName}</strong>,</p>
                  <p style="color:#4a4a4a;font-size:15px;line-height:1.8;margin:0 0 32px;">Sau khi xem xét hồ sơ của bạn, đội ngũ Bonddy rất vui mừng thông báo rằng bạn đã chính thức trở thành một <strong style="color:#0f3460;">Buddy</strong> trên nền tảng của chúng tôi. Tài khoản của bạn hiện đã được kích hoạt đầy đủ quyền hạn.</p>

                  <!-- WHAT YOU GET -->
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#f8f9ff;border-radius:12px;padding:4px 0;margin-bottom:36px;">
                    <tr><td style="padding:20px 24px 4px;"><p style="margin:0;color:#0f3460;font-size:13px;font-weight:700;letter-spacing:1px;text-transform:uppercase;">Quyền lợi của bạn</p></td></tr>
                    <tr>
                      <td style="padding:12px 24px;">
                        <table width="100%" cellpadding="0" cellspacing="0">
                          <tr>
                            <td style="padding:8px 0;border-bottom:1px solid #eaecf3;">
                              <p style="margin:0;color:#2d2d2d;font-size:14px;line-height:1.6;"><strong style="color:#0f3460;">Profile được hiển thị</strong> — Hồ sơ của bạn sẽ xuất hiện cho khách hàng tiềm năng trên toàn nền tảng</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:8px 0;border-bottom:1px solid #eaecf3;">
                              <p style="margin:0;color:#2d2d2d;font-size:14px;line-height:1.6;"><strong style="color:#0f3460;">Nhận booking</strong> — Khách du lịch có thể đặt lịch trực tiếp với bạn</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:8px 0;">
                              <p style="margin:0;color:#2d2d2d;font-size:14px;line-height:1.6;"><strong style="color:#0f3460;">Gói dịch vụ</strong> — Nâng cấp gói để tăng mức độ hiển thị và ưu tiên trong kết quả tìm kiếm</p>
                            </td>
                          </tr>
                        </table>
                      </td>
                    </tr>
                  </table>

                  <!-- CTA BUTTON -->
                  <table width="100%" cellpadding="0" cellspacing="0">
                    <tr>
                      <td align="center" style="padding-bottom:36px;">
                        <a href="https://www.bonddy.vn/login?refreshSession=1&callbackUrl=/buddy"
                           style="display:inline-block;background:linear-gradient(135deg,#0f3460,#16213e);color:#ffffff;text-decoration:none;font-size:15px;font-weight:700;padding:16px 48px;border-radius:50px;letter-spacing:0.5px;box-shadow:0 4px 16px rgba(15,52,96,0.35);"
                           target="_blank">Truy cập trang quản lí Buddy</a>
                      </td>
                    </tr>
                  </table>

                  <hr style="border:none;border-top:1px solid #eaecf3;margin:0 0 28px;" />

                  <p style="color:#7a7a8c;font-size:13px;line-height:1.7;margin:0;">Nếu bạn có bất kỳ câu hỏi nào, đừng ngần ngại liên hệ với đội ngũ hỗ trợ của chúng tôi. Chúng tôi luôn sẵn sàng giúp đỡ bạn.</p>
                </td>
              </tr>

              <!-- FOOTER -->
              <tr>
                <td style="background:#f8f9ff;border-top:1px solid #eaecf3;padding:24px 40px;text-align:center;">
                  <p style="margin:0 0 4px;color:#0f3460;font-size:14px;font-weight:700;">BONDDY</p>
                  <p style="margin:0;color:#9a9ab0;font-size:12px;">Email này được gửi tự động, vui lòng không reply trực tiếp.</p>
                  <p style="margin:8px 0 0;color:#c0c0d0;font-size:11px;">&#169; {DateTime.UtcNow.Year} Bonddy. All rights reserved.</p>
                </td>
              </tr>

            </table>
          </td>
        </tr>
      </table>
    </body>
    </html>
    """;
}

