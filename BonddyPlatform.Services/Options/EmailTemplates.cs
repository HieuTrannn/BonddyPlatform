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
        var safePurpose = WebUtility.HtmlEncode(purposeTitle ?? "Verification");
        var safeName = WebUtility.HtmlEncode(recipientName ?? string.Empty);
        var expiresText = expiresInMinutes <= 0
            ? "Please use this code immediately."
            : string.Create(CultureInfo.InvariantCulture, $"This code will expire in {expiresInMinutes} minutes.");

        var greetingLine = string.IsNullOrWhiteSpace(safeName)
            ? "Hello,"
            : $"Hi {safeName},";

        return $"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1.0" />
          <title>{safePurpose}</title>
        </head>
        <body style="margin:0;padding:0;background:#f4f6fb;font-family:'Segoe UI',Arial,sans-serif;">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6fb;padding:40px 0;">
            <tr>
              <td align="center">
                <table width="600" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);max-width:600px;width:100%;">
                  <!-- HEADER -->
                  <tr>
                    <td style="background:linear-gradient(135deg,#1a1a2e 0%,#16213e 50%,#0f3460 100%);padding:32px 40px;text-align:center;">
                      <div style="display:inline-block;background:rgba(255,255,255,0.08);border:1px solid rgba(255,255,255,0.15);border-radius:8px;padding:8px 24px;margin-bottom:16px;">
                        <span style="color:#e2a84b;font-size:18px;font-weight:800;letter-spacing:2px;">BONDDY</span>
                      </div>
                      <h2 style="color:#ffffff;font-size:24px;font-weight:700;margin:0;letter-spacing:-0.5px;">{safePurpose}</h2>
                    </td>
                  </tr>
                  <!-- BODY -->
                  <tr>
                    <td style="padding:40px 40px 32px;">
                      <p style="color:#2d2d2d;font-size:16px;line-height:1.7;margin:0 0 16px;">{greetingLine}</p>
                      <p style="color:#4a4a4a;font-size:15px;line-height:1.6;margin:0 0 32px;">
                        Here is your verification code to complete your request. {WebUtility.HtmlEncode(expiresText)}
                      </p>
                      <!-- OTP BOX -->
                      <table width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:32px;">
                        <tr>
                          <td align="center">
                            <div style="display:inline-block;background:#f8f9ff;border:2px dashed #0f3460;color:#0f3460;border-radius:12px;padding:16px 32px;font-family:'Courier New',Courier,monospace;font-size:32px;letter-spacing:12px;font-weight:800;text-align:center;">
                              {safeOtp}
                            </div>
                          </td>
                        </tr>
                      </table>
                      <p style="color:#7a7a8c;font-size:14px;line-height:1.6;margin:0 0 24px;">
                        If you didn't request this code, you can safely ignore this email. Your account is secure.
                      </p>
                      <hr style="border:none;border-top:1px solid #eaecf3;margin:0;" />
                    </td>
                  </tr>
                  <!-- FOOTER -->
                  <tr>
                    <td style="background:#f8f9ff;border-top:1px solid #eaecf3;padding:24px 40px;text-align:center;">
                      <p style="margin:0 0 4px;color:#0f3460;font-size:14px;font-weight:700;">BONDDY</p>
                      <p style="margin:0;color:#9a9ab0;font-size:12px;">This is an automated email, please do not reply directly.</p>
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
    <html lang="en">
    <head>
      <meta charset="UTF-8" />
      <meta name="viewport" content="width=device-width, initial-scale=1.0" />
      <title>Welcome to Bonddy</title>
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
                  <h1 style="color:#ffffff;font-size:28px;font-weight:700;margin:0 0 8px;letter-spacing:-0.5px;">Congratulations, {fullName}!</h1>
                  <p style="color:rgba(255,255,255,0.7);font-size:15px;margin:0;">Your Buddy application has been approved</p>
                </td>
              </tr>

              <!-- GOLDEN BANNER -->
              <tr>
                <td style="background:linear-gradient(90deg,#e2a84b,#f5c97a,#e2a84b);padding:16px 40px;text-align:center;">
                  <p style="margin:0;color:#1a1a2e;font-size:14px;font-weight:700;letter-spacing:1px;text-transform:uppercase;">You are now an official Buddy</p>
                </td>
              </tr>

              <!-- BODY -->
              <tr>
                <td style="padding:44px 40px 32px;">
                  <p style="color:#2d2d2d;font-size:16px;line-height:1.7;margin:0 0 20px;">Hi <strong>{firstName}</strong>,</p>
                  <p style="color:#4a4a4a;font-size:15px;line-height:1.8;margin:0 0 32px;">After reviewing your profile, the Bonddy team is thrilled to announce that you are officially a <strong style="color:#0f3460;">Buddy</strong> on our platform. Your account is now fully activated with all privileges.</p>

                  <!-- WHAT YOU GET -->
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#f8f9ff;border-radius:12px;padding:4px 0;margin-bottom:36px;">
                    <tr><td style="padding:20px 24px 4px;"><p style="margin:0;color:#0f3460;font-size:13px;font-weight:700;letter-spacing:1px;text-transform:uppercase;">Your Benefits</p></td></tr>
                    <tr>
                      <td style="padding:12px 24px;">
                        <table width="100%" cellpadding="0" cellspacing="0">
                          <tr>
                            <td style="padding:8px 0;border-bottom:1px solid #eaecf3;">
                              <p style="margin:0;color:#2d2d2d;font-size:14px;line-height:1.6;"><strong style="color:#0f3460;">Visible Profile</strong> — Your profile will now be visible to potential travelers across the platform</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:8px 0;border-bottom:1px solid #eaecf3;">
                              <p style="margin:0;color:#2d2d2d;font-size:14px;line-height:1.6;"><strong style="color:#0f3460;">Receive Bookings</strong> — Travelers can now book sessions directly with you</p>
                            </td>
                          </tr>
                          <tr>
                            <td style="padding:8px 0;">
                              <p style="margin:0;color:#2d2d2d;font-size:14px;line-height:1.6;"><strong style="color:#0f3460;">Service Packages</strong> — Upgrade your package to increase visibility and search priority</p>
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
                        <a href="https://www.bonddy.vn/auth/session-sync?reason=buddy-approved&callbackUrl=/buddy"
                           style="display:inline-block;background:linear-gradient(135deg,#0f3460,#16213e);color:#ffffff;text-decoration:none;font-size:15px;font-weight:700;padding:16px 48px;border-radius:50px;letter-spacing:0.5px;box-shadow:0 4px 16px rgba(15,52,96,0.35);"
                           target="_blank">Access Buddy Dashboard</a>
                      </td>
                    </tr>
                  </table>

                  <hr style="border:none;border-top:1px solid #eaecf3;margin:0 0 28px;" />

                  <p style="color:#7a7a8c;font-size:13px;line-height:1.7;margin:0;">If you have any questions, don't hesitate to reach out to our support team. We're always here to help.</p>
                </td>
              </tr>

              <!-- FOOTER -->
              <tr>
                <td style="background:#f8f9ff;border-top:1px solid #eaecf3;padding:24px 40px;text-align:center;">
                  <p style="margin:0 0 4px;color:#0f3460;font-size:14px;font-weight:700;">BONDDY</p>
                  <p style="margin:0;color:#9a9ab0;font-size:12px;">This is an automated email, please do not reply directly.</p>
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

