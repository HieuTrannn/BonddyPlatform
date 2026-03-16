## Bonddy Platform - Trip & Booking Flow

Tài liệu này mô tả **luồng nghiệp vụ từ lúc Traveler tạo trip cho tới khi thanh toán xong và hoàn thành chuyến đi**, bao gồm:
- **Traveler** làm gì
- **Buddy** làm gì
- **API backend** nào được gọi (hiện có trong code)
- **Frontend** cần xử lý gì ở từng bước

---

## 1. Tạo trip (Traveler)

- **Mục tiêu**: Traveler tạo nhu cầu chuyến đi để các Buddy có thể gửi đề nghị (TripRequest).
- **Model chính**: `Trip` (`TripStatus`).
- **Trạng thái**:
  - Ban đầu: `Status = Open`.

### 1.1. API sử dụng

- **Tạo trip**
  - `POST /api/Trips`
  - Auth: `Role.User` (Traveler)
  - Body: `TripCreateRequestDto`
    - `City`
    - `StartDate` (`DateOnly`)
    - `StartTime` (`TimeOnly`)
    - `DurationHours`
    - `Adults`, `Children`
    - `PreferredLanguages` (list string, optional)
    - `Notes` (optional)
  - Xử lý chính: `TripService.CreateAsync(travelerUserId, dto)`
  - Kết quả: `TripResponseDto` với `Status = TripStatus.Open`.

- **Xem danh sách trip của mình**
  - `GET /api/Trips/my`
  - Query: `TripSearchRequestDto` (lọc theo city, status, ngày bắt đầu, phân trang, sort).
  - Xử lý: `TripService.GetMyTripsAsync(...)`.

- **Cập nhật trip**
  - `PUT /api/Trips/{id}`
  - Body: `TripUpdateRequestDto` (các field nullable, chỉ field nào có giá trị mới được cập nhật).
  - Xử lý: `TripService.UpdateAsync(...)`.

- **Xóa (soft delete) trip**
  - `DELETE /api/Trips/{id}`
  - Xử lý: `TripService.DeleteAsync(...)`:
    - Chỉ chủ sở hữu trip được xóa.
    - Không cho phép xóa nếu đang có booking ở các trạng thái:
      - `PendingCustomerConfirm`, `PendingPayment`, `Confirmed`, `InProgress`.
    - Khi xóa: `Status = TripStatus.Deleted`.

### 1.2. FE cần làm (Traveler)

- Form "Create Trip":
  - Validate input (city, ngày giờ, số người, …).
  - Gọi `POST /api/Trips` với JWT của traveler.
  - Sau khi tạo thành công, điều hướng đến:
    - Trang chi tiết trip, hoặc
    - Danh sách trips của tôi.

- Màn "My Trips":
  - Gọi `GET /api/Trips/my`.
  - Cho phép:
    - Xem chi tiết trip.
    - Chỉnh sửa trip (`PUT /api/Trips/{id}`) khi còn hợp lệ.
    - Xóa trip (`DELETE /api/Trips/{id}`) nếu không có booking active (nếu API trả 400/409 thì hiển thị lý do).

---

## 2. Buddy duyệt trip và gửi TripRequest

- **Mục tiêu**: Buddy tìm các trip đang mở (`Open`) và gửi đề nghị (giá, nội dung, ghi chú…).
- **Model chính**: `TripRequest`, `Trip`, `Buddy`.

### 2.1. API sử dụng

- **Buddy xem danh sách trip mở**
  - `GET /api/Trips/open`
  - Auth: `Role.Buddy`
  - Query: `TripSearchRequestDto`.
  - Xử lý: `TripService.GetOpenTripsForBuddyAsync(...)` lọc `Trip.Status == TripStatus.Open`.

- **Buddy gửi TripRequest cho một Trip**
  - (Controller không nằm trong đoạn trích, nhưng service là `TripRequestService.CreateAsync(buddyUserId, TripRequestCreateDto)`.)
  - Nghiệp vụ trong `TripRequestService.CreateAsync`:
    - Kiểm tra Buddy profile tồn tại.
    - Load trip và validate:
      - Trip tồn tại.
      - `Trip.Status == TripStatus.Open`.
    - Chống gửi trùng: 1 Buddy chỉ gửi 1 TripRequest cho 1 Trip.
    - Kiểm tra:
      - Nếu `IsInboxOnly == false` thì `OfferedPrice` phải có.
    - Tính `ExpiresAt` = thời điểm bắt đầu trip (StartDate + StartTime, UTC).
    - Tạo record `TripRequest`.
    - **Tự động tạo phòng chat trực tiếp** giữa Traveler và Buddy: `_chatService.GetOrCreateDirectChatAsync(...)`.
    - Gửi **tin nhắn intro** vào chat tóm tắt trip + offer.

- **Traveler xem danh sách TripRequest cho trip của mình**
  - `TripRequestService.GetByTripIdAsync(tripId, requesterId)`:
    - Chỉ Traveler của trip mới xem được.
    - Trả về `TripRequestResponseDto` + `ExistingChatRoomId` nếu đã có phòng chat.

### 2.2. FE cần làm

**Phía Buddy**
- Màn "Browse Trips":
  - Gọi `GET /api/Trips/open`.
  - Hiển thị filter (city, ngày, …).
  - Bấm vào 1 trip → màn chi tiết trip + nút "Gửi đề nghị".

- Form "Gửi TripRequest":
  - Nhập:
    - Giá offer (`OfferedPrice`) nếu không phải inbox-only.
    - Check box "Inbox only" nếu muốn thương lượng giá qua chat.
    - Ghi chú (`Note`).
  - Gọi API tạo TripRequest (controller tương ứng, dùng `TripRequestCreateDto`).
  - Sau khi tạo:
    - FE nên điều hướng sang màn **chat** sử dụng `ChatRoomId` trả về (`StartChat` hoặc `CreateAsync` đã tạo room).

**Phía Traveler**
- Trong chi tiết trip:
  - Hiển thị danh sách TripRequests (gọi API `GetByTripIdAsync`).
  - Với mỗi request:
    - Thông tin Buddy (tên, avatar).
    - Giá đề xuất, ghi chú, trạng thái hết hạn hay chưa.
    - Nút "Chat" (dùng `ExistingChatRoomId` hoặc gọi `StartChatAsync`) để mở phòng chat.
    - Nút "Tạo Booking" (khi quyết định chốt với Buddy đó).

---

## 3. Tạo Booking từ TripRequest (Buddy)

- **Mục tiêu**: Khi thương lượng xong, Buddy tạo Booking chính thức chứa:
  - Thời gian cụ thể (ngày, giờ bắt đầu, số giờ).
  - Số khách (adults, children).
  - Giá tour, currency, phần bao gồm/không bao gồm, ghi chú cho khách.

- **Model**: `Booking`, `BookingStatus`.
  - Trạng thái khởi tạo: `PendingCustomerConfirm`.

### 3.1. API sử dụng

- **Buddy tạo booking**
  - Service: `BookingService.CreateFromTripRequestAsync(buddyUserId, tripRequestId, BookingCreateFromTripRequestDto)`.
  - Nghiệp vụ:
    - Load TripRequest + Trip + Buddy, validate:
      - TripRequest tồn tại, đúng Buddy.
      - Chưa có booking khác cho TripRequest này (one booking per TripRequest).
    - Lấy `ChatRoomId` gắn với TripRequest (nếu có).
    - Tính toán:
      - `PlatformFeeAmount = Price * 10%` (làm tròn 2 chữ số).
      - `TotalAmount = Price + PlatformFeeAmount`.
    - khởi tạo `Booking`:
      - Liên kết: `TripId`, `TripRequestId`, `BuddyId`, `TravelerUserId`, `ChatRoomId`.
      - `BookedDate`, `BookedStartTime`, `BookedDurationHours`, `BookedAdults`, `BookedChildren`.
      - `Price`, `Currency`, `PlatformFeeAmount`, `TotalAmount`.
      - `Includes`, `Excludes`, `NoteForCustomer`.
      - `Status = PendingCustomerConfirm`.
    - Lưu booking.
    - Gửi **BookingCard message** vào phòng chat: `_chatService.SendBookingCardAsync(chatRoomId, buddyUserId, booking.Id)`.

### 3.2. FE cần làm

**Phía Buddy**
- Trong màn chat hoặc màn chi tiết TripRequest:
  - Nút "Tạo Booking":
    - Mở form cho Buddy nhập:
      - `BookedDate`, `BookedStartTime`, `BookedDurationHours`.
      - `BookedAdults`, `BookedChildren`.
      - `Price`, `Currency`.
      - `Includes`, `Excludes`, `NoteForCustomer`.
    - Gọi API `CreateFromTripRequest` (controller gọi `BookingService.CreateFromTripRequestAsync`).
    - Sau khi tạo:
      - Hiển thị BookingCard trong UI chat (tin nhắn kiểu "booking").

**Phía Traveler**
- Trong chat:
  - Khi nhận được BookingCard:
    - Hiển thị đầy đủ thông tin booking.
    - Có nút "Confirm Booking" để chuyển sang bước thanh toán.

---

## 4. Traveler xác nhận Booking (trước khi thanh toán)

- **Mục tiêu**: Traveler đồng ý với nội dung booking → hệ thống chuyển sang trạng thái **chờ thanh toán**.
- **Model**: `BookingStatus`.
  - Từ `PendingCustomerConfirm` → `PendingPayment`.

### 4.1. API sử dụng

- **Traveler confirm booking**
  - Service: `BookingService.ConfirmByTravelerAsync(travelerUserId, bookingId)`.
  - Nghiệp vụ:
    - Chỉ Traveler của trip mới được confirm.
    - Chỉ cho confirm khi `booking.Status == PendingCustomerConfirm`.
    - Khi confirm:
      - `Status = PendingPayment`.
      - `PaymentDeadline = Now + 24h` (hằng số `PaymentDeadlineHours = 24`).

*(Controller tương ứng sẽ gọi service này, ví dụ `PUT /api/Bookings/{id}/confirm` – tùy cách bạn đã tạo controller.)*

### 4.2. FE cần làm

- **Trong UI BookingCard (phía Traveler)**:
  - Khi status là `PendingCustomerConfirm`:
    - Hiển thị nút "Confirm booking".
    - Gọi API confirm (ví dụ `PUT /api/Bookings/{id}/confirm`).
    - Sau khi thành công:
      - Cập nhật trạng thái booking sang `PendingPayment`.
      - Hiển thị countdown tới `PaymentDeadline` (nếu muốn).
      - Hiển thị nút "Proceed to Payment".

---

## 5. Thanh toán (Payment) và xác nhận Booking

- **Mục tiêu**: Traveler thanh toán tiền cho booking trong thời gian cho phép.
- **Model**: `Booking`, `Payment` (reference từ `Booking.Payment`), `BookingStatus`.

### 5.1. Trạng thái booking liên quan payment

- `PendingPayment`:
  - Traveler đã confirm nội dung, nhưng **chưa trả tiền**.
  - Có `PaymentDeadline`.
- `Confirmed`:
  - Thanh toán thành công.
  - Booking được giữ chỗ chính thức.
  - Logic trong `BookingService.UpdateStatusAsync`:
    - Nếu chuyển sang `Confirmed`:
      - Set `Trip.Status = Closed` để không nhận thêm request/booking nữa.
- `CancelledByTimeout`:
  - Hết deadline mà chưa thanh toán → hệ thống/cron có thể chuyển sang trạng thái này.
  - Đồng thời nếu không còn booking active khác, có thể mở lại trip (`Trip.Status = Open`).

### 5.2. API / Service liên quan

- **Cập nhật trạng thái booking (CS/Admin hoặc luồng system)**
  - `BookingService.UpdateStatusAsync(Guid id, BookingStatus newStatus)`:
    - Kiểm tra transition hợp lệ bằng `AllowedTransitions`:
      - `PendingPayment` → `Confirmed`, `CancelledByTimeout`, `Cancelled`.
      - `Confirmed` → `InProgress`.
      - `InProgress` → `Completed`.
      - `Completed`, `Cancelled`, `CancelledByTimeout` → trạng thái cuối, không chuyển tiếp.
    - Khi `newStatus == Confirmed`: đóng trip (`Trip.Status = Closed`).
    - Khi `newStatus == Completed`: `Trip.Status = Expired`.
    - Khi hủy (`Cancelled`, `CancelledByTimeout`):
      - Nếu đây là booking active duy nhất của trip → mở lại trip (`Trip.Status = Open`).

- **Luồng payment cụ thể (tích hợp cổng thanh toán)**
  - Chưa được implement đầy đủ trong code, nhưng logic thường là:
    1. Traveler ở màn `PendingPayment` bấm "Pay now".
    2. Backend tạo session/tham số thanh toán với cổng (Stripe, PayPal, v.v.).
    3. Sau khi cổng báo **success** qua webhook / callback:
       - Backend gọi `BookingService.UpdateStatusAsync(bookingId, BookingStatus.Confirmed)`.
       - Tạo record `Payment` gắn với Booking.

### 5.3. FE cần làm

- Khi booking đang ở `PendingPayment`:
  - Hiển thị:
    - Số tiền (`TotalAmount`, `Currency`).
    - `PaymentDeadline` (đồng hồ đếm ngược nếu muốn).
  - Nút "**Pay now**":
    - Gọi API tạo payment session (ví dụ `POST /api/Payments/checkout` với `bookingId`).
    - Handle redirect tới cổng thanh toán hoặc mở popup (tùy provider).

- Sau khi thanh toán:
  - Nhận redirect back URL (kèm `bookingId` hoặc code).
  - FE gọi API hỏi lại trạng thái booking (`GET /api/Bookings/{id}`):
    - Nếu `Status == Confirmed` → hiển thị "Booking confirmed".
    - Nếu payment fail / timeout → hiển thị lỗi, cho phép thử lại hoặc hủy.

---

## 6. Thực hiện tour và hoàn thành

- **Mục tiêu**: Sau khi đã thanh toán, tour diễn ra và được đánh dấu hoàn thành.

### 6.1. Trạng thái liên quan

- `Confirmed`: booking đã thanh toán, tour sắp diễn ra.
- `InProgress`: tour đang diễn ra.
- `Completed`: tour hoàn thành.

Các transition do `BookingService.UpdateStatusAsync` điều khiển:
- `Confirmed` → `InProgress` (khi tour bắt đầu).
- `InProgress` → `Completed` (khi tour kết thúc).
- Khi trở thành `Completed`:
  - `Trip.Status = Expired` (không còn hiệu lực để nhận thêm yêu cầu).

### 6.2. FE cần làm

- **Dashboard Traveler/Buddy**:
  - Gọi API (ví dụ `GET /api/Bookings/my-traveler`, `GET /api/Bookings/my-buddy`) – các service đã có (`GetMyBookingsAsTravelerAsync`, `GetMyBookingsAsBuddyAsync`).
  - Hiển thị trạng thái booking và các action:
    - Đối với Buddy:
      - Có thể chuyển booking sang `InProgress` / `Completed` thông qua API admin/CS hoặc endpoint dành riêng cho Buddy (tùy bạn thiết kế controller).
    - Đối với Traveler:
      - Xem trạng thái, chi tiết booking, mở chat với Buddy.

---

## 7. Tóm tắt luồng chính (Traveler & Buddy)

### 7.1. Phía Traveler

1. **Tạo trip** → `POST /api/Trips` → `Trip.Status = Open`.
2. Nhận TripRequests từ nhiều Buddy → xem trong màn chi tiết trip.
3. Chat với từng Buddy (phòng chat được tạo tự động khi Buddy gửi TripRequest).
4. Khi chốt với 1 Buddy:
   - Nhận BookingCard trong chat.
   - **Bước 1**: Confirm nội dung booking → `ConfirmByTravelerAsync` → `Status = PendingPayment`.
   - **Bước 2**: Thanh toán → cổng thanh toán → backend cập nhật `Status = Confirmed`.
5. Chờ tour → tour diễn ra (`InProgress`) → kết thúc (`Completed`).

### 7.2. Phía Buddy

1. **Duyệt trip mở** → `GET /api/Trips/open`.
2. Gửi TripRequest cho trip phù hợp:
   - Tạo request + auto chat room + intro message.
3. Thương lượng qua chat.
4. Khi thống nhất:
   - Tạo Booking từ TripRequest → `CreateFromTripRequestAsync` → `Status = PendingCustomerConfirm`.
   - Hệ thống gửi BookingCard vào chat.
5. Chờ Traveler:
   - Traveler Confirm → `PendingPayment`.
   - Traveler thanh toán → `Confirmed`.
6. Đến ngày tour:
   - Buddy có thể chuyển booking sang `InProgress` rồi `Completed` (qua API tương ứng mà bạn thiết kế UI gọi).

---

## 8. Gợi ý mở rộng cho tài liệu & code

- Bổ sung thêm:
  - **PaymentController** mô tả luồng cụ thể với cổng thanh toán bạn chọn (Stripe, PayPal, v.v.).
  - **Webhook endpoints** để cập nhật `BookingStatus` tự động.
  - Mô tả chi tiết request/response JSON cho từng API trong tài liệu này để FE dễ implement.

