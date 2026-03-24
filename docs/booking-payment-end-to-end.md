# Booking -> Payment End-to-End Flow (BonddyPlatform)

Tài liệu này mô tả đầy đủ luồng từ lúc Buddy tạo booking trong chat cho tới khi Traveler thanh toán thành công bằng PayPal, bao gồm:

- API contract cho FE
- Cách backend xử lý từng bước
- Tutorial FE triển khai theo từng màn hình

---

## 1) Mục tiêu nghiệp vụ

Luồng chuẩn:

1. Buddy và Traveler trao đổi trong chat.
2. Buddy tạo booking ngay trong chat (không bắt buộc phải có Trip trước từ FE).
3. Backend gửi BookingCard vào chat.
4. Traveler mở chi tiết booking.
5. Traveler bấm **Xác nhận booking**.
6. Hệ thống chuyển booking sang trạng thái chờ thanh toán và tạo PayPal order ngay.
7. FE redirect user sang PayPal checkout.
8. Traveler thanh toán thành công.
9. FE gọi capture (và webhook PayPal đồng bộ trạng thái).
10. Booking chuyển sang `Confirmed`, payment chuyển sang `Held`.

---

## 2) Các trạng thái chính

### BookingStatus

- `PendingCustomerConfirm`: Buddy đã gửi booking card, chờ Traveler xác nhận.
- `PendingPayment`: Traveler đã xác nhận, chờ thanh toán.
- `Confirmed`: thanh toán thành công.
- `InProgress`, `Completed`, `Cancelled`, `CancelledByTimeout`, `Expired`: các trạng thái sau đó.

### PaymentStatus

- `Pending`: payment record vừa tạo/chưa thu tiền thành công.
- `Held`: đã capture thành công (tiền giữ trong escrow).
- `Released`: giải ngân (ops/admin xử lý).
- `Refunded`: hoàn tiền.

---

## 3) API BE cho luồng này

## 3.1 Buddy tạo booking từ chat

### `POST /api/Bookings`

Role: `Buddy`

`TripRequestId` là optional:

- Có `tripRequestId`: chạy flow cũ từ TripRequest.
- Không có `tripRequestId`: dùng `chatRoomId` để tạo booking trực tiếp từ cuộc chat.

Request mẫu (direct chat):

```json
{
  "tripRequestId": null,
  "chatRoomId": "f329f425-2fd0-4e18-a670-327eb7f79f01",
  "bookedDate": "2026-03-25",
  "bookedStartTime": "08:30:00",
  "bookedDurationHours": 6,
  "bookedAdults": 2,
  "bookedChildren": 1,
  "price": 120,
  "currency": "USD",
  "includes": "Guide, transport in city center",
  "excludes": "Entrance tickets",
  "noteForCustomer": "Please arrive 10 minutes early"
}
```

Response: `BookingResponseDto`.

Khi tạo thành công, backend tự gửi BookingCard message vào chat room.

---

## 3.2 Traveler xem chi tiết booking

### `GET /api/Bookings/{id}`

Role: authenticated.

Dùng để render màn hình chi tiết booking trước khi thanh toán.

---

## 3.3 1-click: xác nhận booking và tạo PayPal order luôn

### `POST /api/Bookings/{id}/confirm-and-create-paypal-order`

Role: `User` (Traveler)

Đây là endpoint chính cho UX mới: bấm 1 nút là đi thẳng payment.

Response: `ConfirmBookingAndPayResponseDto`

```json
{
  "success": true,
  "message": "Booking confirmed and PayPal order created.",
  "data": {
    "booking": {
      "id": "....",
      "status": 1,
      "statusName": "PendingPayment"
    },
    "paymentOrder": {
      "paymentId": "....",
      "bookingId": "....",
      "orderId": "O-....",
      "approveUrl": "https://www.paypal.com/checkoutnow?token=...",
      "currency": "USD",
      "amount": 120.0
    }
  }
}
```

FE lấy `paymentOrder.approveUrl` và redirect ngay.

---

## 3.4 Capture sau khi user approve ở PayPal

### `POST /api/Bookings/payments/{paymentId}/paypal/capture`

Role: authenticated user

Request:

```json
{
  "orderId": "O-...."
}
```

Nếu capture thành công:

- Payment -> `Held`
- Booking (nếu đang `PendingPayment`) -> `Confirmed`

---

## 3.5 Webhook PayPal

### `POST /api/payments/paypal/webhook`

Role: `AllowAnonymous` (PayPal gọi server-to-server)

Backend verify chữ ký webhook và cập nhật trạng thái tương ứng:

- `CHECKOUT.ORDER.COMPLETED` / `PAYMENT.CAPTURE.COMPLETED` -> `Held` / `Confirmed`
- `PAYMENT.CAPTURE.REFUNDED`, `PAYMENT.CAPTURE.DENIED` -> `Refunded`

---

## 4) Backend xử lý như thế nào (chi tiết)

## 4.1 Tạo booking direct-chat (không Trip từ FE)

Trong service:

- Validate Buddy đúng participant trong chat room.
- Từ chat participants, xác định Traveler (participant còn lại).
- Tạo các dữ liệu nội bộ cần thiết để tương thích schema hiện tại.
- Tạo booking với `PendingCustomerConfirm`.
- Gửi BookingCard message vào chat (`SendBookingCardAsync`).

Lưu ý: Dù FE không gửi Trip, backend vẫn đảm bảo ràng buộc dữ liệu nội bộ để không phá schema.

## 4.2 Confirm-and-create-payment

`confirm-and-create-paypal-order` thực hiện 2 việc:

1. Ensure booking ở trạng thái `PendingPayment`:
   - nếu đang `PendingCustomerConfirm` thì confirm.
   - nếu đã `PendingPayment` thì idempotent (không lỗi).
2. Tạo PayPal order và trả `approveUrl`.

## 4.3 Capture & Webhook (double safety)

- FE gọi capture khi user quay về success page.
- Webhook vẫn chạy độc lập để đồng bộ trạng thái trong các tình huống mạng chập chờn/FE rớt.

Nguyên tắc:

- Không cho thanh toán booking sai trạng thái.
- Không cho user thanh toán booking không thuộc mình.
- Idempotent ở bước trạng thái `PendingPayment`.

---

## 5) Tutorial cho FE (khuyến nghị triển khai)

## 5.1 Màn chat của Buddy

UI:

- Nút `Create Booking` trong chat toolbar.

Action:

1. Buddy nhập form booking.
2. FE gọi `POST /api/Bookings`.
3. Sau success, booking card sẽ xuất hiện trong chat (qua message list/realtime event).

## 5.2 Màn chat của Traveler

UI:

- BookingCard có nút `View booking`.

Action:

1. Mở detail page `/bookings/{bookingId}`.
2. Gọi `GET /api/Bookings/{id}` để render thông tin.

## 5.3 Màn booking detail

UI:

- Nút chính: `Confirm & Pay`.

Action:

1. Gọi `POST /api/Bookings/{id}/confirm-and-create-paypal-order`.
2. Nếu success và có `approveUrl`, redirect:
   - `window.location.href = approveUrl`.

## 5.4 Màn callback sau PayPal

Return URL ví dụ: `https://www.bonddy.vn/payment/success`.

On load:

1. Đọc query params (`token` thường là `orderId`).
2. Lấy `paymentId` đã lưu tạm trước redirect (state/store/local/session).
3. Gọi `POST /api/Bookings/payments/{paymentId}/paypal/capture` với `orderId`.
4. Nếu success -> hiển thị "Payment success".
5. Chuyển user về booking detail trạng thái `Confirmed`.

## 5.5 Màn cancel

Cancel URL ví dụ: `https://www.bonddy.vn/payment/cancel`.

- Hiển thị thông báo thanh toán bị hủy.
- Cho user quay lại booking detail để thử lại.

---

## 6) Pseudo-code FE (React/Next)

```ts
async function onConfirmAndPay(bookingId: string) {
  const res = await api.post(`/api/Bookings/${bookingId}/confirm-and-create-paypal-order`);
  const approveUrl = res.data?.data?.paymentOrder?.approveUrl;
  const paymentId = res.data?.data?.paymentOrder?.paymentId;
  const orderId = res.data?.data?.paymentOrder?.orderId;

  sessionStorage.setItem("pendingPaymentId", paymentId);
  sessionStorage.setItem("pendingOrderId", orderId);

  window.location.href = approveUrl;
}

async function onPayPalReturn(orderIdFromQuery: string) {
  const paymentId = sessionStorage.getItem("pendingPaymentId");
  await api.post(`/api/Bookings/payments/${paymentId}/paypal/capture`, {
    orderId: orderIdFromQuery
  });
  // show success UI
}
```

---

## 7) Config cần có ở backend (Live)

Env vars:

- `PAYPAL_BASE_URL=https://api-m.paypal.com`
- `PAYPAL_CLIENT_ID=...`
- `PAYPAL_SECRET=...`
- `PAYPAL_WEBHOOK_ID=...`
- `PAYPAL_RETURN_URL=https://www.bonddy.vn/payment/success`
- `PAYPAL_CANCEL_URL=https://www.bonddy.vn/payment/cancel`

Webhook URL trên PayPal App:

- `https://api.bonddy.vn/api/payments/paypal/webhook`

---

## 8) Checklist test end-to-end

- Buddy tạo booking từ chat direct thành công.
- Traveler thấy booking card và mở detail được.
- Bấm `Confirm & Pay` redirect đúng sang PayPal.
- PayPal approve xong quay lại FE success page.
- FE capture thành công.
- Booking chuyển `Confirmed`.
- Payment chuyển `Held`.
- Webhook PayPal nhận và verify thành công.
- Trường hợp cancel vẫn quay lại flow retry được.

---

## 9) Lưu ý triển khai thực tế

- FE nên disable nút `Confirm & Pay` khi request đang pending để tránh double-click.
- FE cần lưu `paymentId` tạm trước khi redirect.
- Nếu capture fail ở callback, hiển thị thông báo + nút retry.
- Nên có trang lịch sử payment/booking cho user đối soát.

