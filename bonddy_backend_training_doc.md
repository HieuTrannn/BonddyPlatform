# Bonddy — The Local Experience Platform
> **Backend + Database Training Document (for AI assistance)**  
> Version: **0.1**  
> Scope: **MVP Phase 1 (CS-driven) + Roadmap Phase 2 (Direct booking + Matching)**  
> Target stack: **Web-first**, multi-language, PostgreSQL

---

## 0) Mục tiêu tài liệu
Tài liệu này dùng để “train/ground” AI hoặc thành viên team mới hiểu nhanh dự án Bonddy và hỗ trợ bạn trong:
- Thiết kế & xây dựng backend (API, auth, chat realtime, booking, payment, review)
- Thiết kế database PostgreSQL (schema, quan hệ, trạng thái, index)
- Định nghĩa business rules, workflow vận hành (Phase 1 → Phase 2)

Tài liệu ưu tiên **tính thực thi**: mô tả *domain*, *workflow runtime*, *data model* và *API contract* mức nền tảng.

---

## 1) Tổng quan sản phẩm
**Bonddy / The Local Experience Platform** kết nối **Traveler** với **Local Buddy/Guide** để cung cấp trải nghiệm địa phương (1–6 giờ).

### 1.1 Phân kỳ sản phẩm
#### Phase 1 (MVP — CS-driven fulfillment)
- **Authentication** (đăng ký/đăng nhập)
- **Create Trip** (tạo yêu cầu chuyến đi cá nhân hóa theo sở thích/nhu cầu)
- **Chat** (Traveler ↔ CS) theo từng Trip Room
- **CS có thể tìm guide ngoài hệ thống** (external guide) và tạo booking thủ công
- **Payment**: online hoặc cash  
  - Nếu online: **Escrow** giữ tiền đến khi trip hoàn thành
- **Review** sau trip

#### Phase 2 (khi đủ supply buddy trong hệ thống)
- Traveler xem **Buddy Profile** và **Direct Booking**
- **Two-way matching**: buddy accept/decline booking request từ trip đã tạo
- **AI Recommendations/Matching**: gợi ý top buddy phù hợp + “book now”

### 1.2 Actors & Roles
- **Traveler**: tạo trip, chat, thanh toán, review
- **CS (Customer Service / Ops)**: tiếp nhận trip, tư vấn, gán guide/buddy, tạo booking thủ công, xử lý sự cố, hỗ trợ escrow/release
- **Admin**: quản trị hệ thống, RBAC, cấu hình, audit
- **Buddy** (Phase 2): nhận request, accept/decline, dẫn trip
- **Partner** (Phase 2+): referral/QR, voucher/offers, reporting (growth & supply)

---

## 2) Domain Model (Khái niệm lõi)
### 2.1 Trip
Trip là yêu cầu trải nghiệm do Traveler tạo, gồm:
- City (HCMC là market đầu tiên; mở rộng city lớn VN)
- Time window / start time
- Duration (phút) — thường 60..360
- Group size
- Preferred language
- Interests / vibe / must-have / must-avoid (tags)
- Budget range (optional)
- Notes & meeting point text

Trip có 1 **Trip Room** để chat.

### 2.2 Booking
Booking là phiên đặt lịch được tạo cho Trip.
- Phase 1: CS tạo booking thủ công và gán guide (internal buddy hoặc external guide)
- Phase 2: traveler gửi request đến buddy → buddy accept/decline → booking confirmed

Booking lưu **pricing breakdown**:
- base_rate_per_hour, duration_minutes
- subtotal_amount
- surcharges_amount
- platform_fee_amount
- tip_amount
- total_amount

### 2.3 Payment (Escrow / Cash)
- **ONLINE**: traveler trả → status `HELD` (escrow) → trip completed → release → `RELEASED`
- **CASH**: traveler trả trực tiếp → status `CASH_EXPECTED` → sau trip mark `CASH_RECEIVED` (CS confirm optional)

### 2.4 Review
Sau trip, traveler đánh giá guide/buddy:
- rating (1..5)
- comment + tags feedback
- moderation (ẩn/flag) do CS/Admin

### 2.5 Chat
Chat per trip room:
- participants: traveler + CS (+ guide/buddy nếu cần)
- messages: text, image/file (attachment lưu Firebase Storage; DB lưu metadata & path)

### 2.6 External Guide (Phase 1)
Để không phụ thuộc supply buddy, CS có thể tìm guide ngoài hệ thống.  
Khi đó booking vẫn hợp lệ và review vẫn gắn với “guide entity” đó.

---

## 3) Workflows runtime (luồng nghiệp vụ)
### 3.1 Phase 1 — CS-driven fulfillment
1) Traveler đăng nhập  
2) Traveler tạo Trip (personalization) → status Trip: `SUBMITTED`
3) System tạo Trip Room & notify CS
4) CS chat tư vấn, hỏi thêm yêu cầu
5) CS tìm guide (external) hoặc buddy (nếu có), tạo booking thủ công
6) Traveler chọn phương thức thanh toán:
   - Online: pay → `payments.status=HELD`
   - Cash: `payments.status=CASH_EXPECTED` (hoặc tạo record payment sau)
7) Trip diễn ra → CS/Traveler đánh dấu complete:
   - booking: `COMPLETED`
   - payment online: `RELEASED`
8) Traveler để lại review

### 3.2 Phase 2 — Direct booking + accept/decline
1) Traveler tạo Trip
2) System gợi ý top buddies (AI rules/scoring)
3) Traveler gửi booking request → `bookings.status=REQUESTED`
4) Buddy accept/decline trong SLA:
   - accept → `ACCEPTED/CONFIRMED`
   - decline → `DECLINED`
   - quá hạn → `EXPIRED`
5) Payment online/cash như Phase 1
6) Completion & review

---

## 4) Status Models (Trạng thái)
### 4.1 Trip status
- `DRAFT` → `SUBMITTED` → `IN_PROGRESS` → `COMPLETED` → `CLOSED`
- `CANCELLED` (có thể từ submitted/in_progress)

### 4.2 Booking status
- Phase 1: `PENDING` → `CONFIRMED` → `IN_PROGRESS` → `COMPLETED` / `CANCELLED`
- Phase 2: `REQUESTED` → `ACCEPTED`/`DECLINED`/`EXPIRED` → `CONFIRMED` → ...

### 4.3 Payment status
- Online: `UNPAID` → `HELD` → `RELEASED` / `REFUNDED`
- Cash: `CASH_EXPECTED` → `CASH_RECEIVED`

### 4.4 Incident status
- `OPEN` → `IN_PROGRESS` → `RESOLVED` → `CLOSED`

---

## 5) Business Rules (cốt lõi)
- BR-01: Mỗi Trip có đúng **1 Trip Room**
- BR-02: Online payment được **giữ escrow** đến khi trip hoàn thành
- BR-03: Buddy có quyền **accept/decline** request (Phase 2)
- BR-04: Pricing cơ bản theo **giờ** (`base_rate_per_hour`)
- BR-05: Surcharges theo rule (late night, weekend, group, out_of_zone, custom)
- BR-06: Tip optional, sau trip
- BR-07: Phase 1: CS có thể fulfil bằng external guide
- BR-08: HCMC là market đầu tiên; mở rộng city lớn VN
- BR-09: RBAC bắt buộc cho Admin/CS/Partner/Buddy/Traveler

---

## 6) Architecture runtime (high-level)
- Clients: Web Admin (CS/Admin), Mobile Apps (Phase 2+), Traveler web (nếu có)
- API service: Auth, Trip, Chat, Booking, Payment, Review, Partner
- Realtime: WebSocket/SignalR cho chat & status updates
- DB: PostgreSQL
- Storage: Firebase Storage (media/attachments)
- Integrations: payment gateway (TBD), notifications (FCM/email/SMS)

---

## 7) Database Design (PostgreSQL) — chi tiết
> Design tối ưu cho MVP, vẫn mở đường Phase 2.

### 7.1 Bảng & mục đích
#### A) Users & RBAC
- `users`: thông tin user cơ bản
- `user_roles`: gán nhiều role cho 1 user (ví dụ user có thể vừa CS vừa Partner nếu cần)
- `traveler_profiles`: mở rộng traveler
- `buddy_profiles`: mở rộng buddy (rating, active_mode, base_rate)

#### B) Preferences/Tags
- `tags`: danh mục sở thích/vibe/avoid...
- `user_tags`: sở thích user (có weight)
- `trips`: trip request
- `trip_tags`: tags theo trip (interest/vibe/avoid/must_have)

#### C) Chat
- `chat_rooms`: 1 room/1 trip
- `chat_room_participants`: participants
- `messages`: message record
- `message_attachments`: metadata file (Firebase path)

#### D) Guide / Booking / Payment / Review
- `guides`: internal buddy hoặc external guide
- `bookings`: booking record + pricing breakdown
- `booking_surcharges`: phụ phí chi tiết
- `payments`: online/cash + escrow timestamps
- `reviews`: review theo booking
- `review_tags`: tags feedback

#### E) Safety / Incident
- `incidents`: ticket xử lý sự cố

#### F) Partner (Phase 2+)
- `partners`: đối tác
- `partner_referrals`: referral codes/QR
- `referral_attributions`: gắn attribution trip/booking
- `vouchers`: voucher/offers

### 7.2 Quan hệ chính (ERD in words)
- `users (1) --- (n) user_roles`
- `users (1) --- (1) traveler_profiles` (optional)
- `users (1) --- (1) buddy_profiles` (optional)
- `users (1) --- (n) user_tags --- (n) tags`
- `users (traveler) (1) --- (n) trips`
- `trips (1) --- (1) chat_rooms`
- `chat_rooms (1) --- (n) messages`
- `chat_rooms (1) --- (n) chat_room_participants --- (n) users`
- `trips (1) --- (n) bookings`
- `bookings (1) --- (1) payments`
- `bookings (1) --- (0..1) reviews`
- `guides (1) --- (n) bookings`
- `partners (1) --- (n) partner_referrals`
- `partner_referrals (1) --- (n) referral_attributions`

### 7.3 Index & performance (khuyến nghị)
- `trips(city, start_time)` cho listing theo city/time
- `messages(room_id, created_at)` cho load chat theo room
- `bookings(status, scheduled_start_time)` cho queue CS/buddy
- `payments(status)` cho ops reconciliation
- `reviews(guide_id, created_at)` cho profile stats
- `buddy_availability(buddy_user_id, start_time)` cho match availability

### 7.4 Chuẩn hóa nâng cao (phase 2+ / v2 schema)
Để AI matching tốt hơn, nên chuẩn hóa:
- `buddy_languages(buddy_user_id, language_code)` thay vì comma list
- `buddy_tags(buddy_user_id, tag_id, weight)`
- `buddy_service_areas(buddy_user_id, city, radius_km)`
- `buddy_last_active_at`, `active_mode` telemetry

---

## 8) API Design (gợi ý contract backend)
> Contract mức khung để implement backend; có thể dùng REST.

### 8.1 Auth
- `POST /auth/register`
- `POST /auth/login`
- `POST /auth/logout`
- `POST /auth/forgot-password`
- `POST /auth/reset-password`
- `GET /me`

### 8.2 Trip
- `POST /trips` (create trip + tags + create chat_room)
- `GET /trips` (list by traveler or CS filters)
- `GET /trips/{id}`
- `PATCH /trips/{id}` (edit before in_progress)

### 8.3 Chat
- `GET /trips/{id}/room`
- `GET /rooms/{roomId}/messages?cursor=...`
- `POST /rooms/{roomId}/messages` (text)
- `POST /rooms/{roomId}/attachments` (upload metadata; file upload via Firebase client or backend proxy)
- Realtime channel: `ws://.../rooms/{roomId}`

### 8.4 Booking (Phase 1 manual / Phase 2 direct)
- Phase 1:
  - `POST /trips/{id}/bookings` (CS create manual booking, can create external guide)
  - `PATCH /bookings/{id}` (status updates)
- Phase 2:
  - `POST /trips/{id}/booking-requests` (traveler to buddy)
  - `POST /booking-requests/{id}/accept`
  - `POST /booking-requests/{id}/decline`

### 8.5 Payment
- `POST /bookings/{id}/payments` (init)
- `POST /payments/{id}/confirm` (gateway callback/webhook)
- `POST /bookings/{id}/complete` (mark completed; triggers escrow release)
- `POST /payments/{id}/refund` (policy TBD)

### 8.6 Review
- `POST /bookings/{id}/reviews`
- `GET /guides/{id}/reviews`
- `PATCH /reviews/{id}/moderate` (CS/Admin)

### 8.7 Partner (Phase 2+)
- `POST /partners` (admin)
- `POST /partners/{id}/referrals`
- `GET /partners/{id}/stats`
- `POST /partners/{id}/vouchers`

---

## 9) Security & Access Control (RBAC)
### 9.1 Permissions (high-level)
- Traveler: CRUD own trip, read own booking/payment/review, chat in own rooms
- CS: read all trips, join rooms, create bookings, update statuses, manage incidents, moderate reviews
- Admin: all permissions + manage roles/config
- Buddy (Phase 2): read assigned requests/bookings, accept/decline, chat in related rooms
- Partner: manage referrals/vouchers, view own stats (limited scope)

### 9.2 Audit logging (khuyến nghị)
Log các hành động quan trọng:
- CS tạo booking, sửa giá, mark completed, release escrow
- Admin thay đổi role/config
- Review moderation

---

## 10) Pricing Engine (rule-based)
### 10.1 Base
`subtotal = base_rate_per_hour * (duration_minutes/60)`

### 10.2 Surcharges (configurable)
- `late_night`: 22:00–06:00
- `weekend/holiday`
- `group_size`: từ người thứ 3 trở lên
- `out_of_zone`: phí di chuyển theo khu vực
- `custom`: CS nhập tay

### 10.3 Platform fee
`platform_fee = subtotal * commission_rate` (ví dụ 15–20%)

### 10.4 Tip
Tip optional sau completion (không tính commission hoặc tính rate thấp hơn — policy)

---

## 11) Firebase Storage usage
- Lưu media: profile images, chat attachments
- DB lưu:
  - `message_attachments.storage_path`
  - `content_type`, `size_bytes`
- Quy tắc bảo mật:
  - upload token/signing hoặc direct upload theo user auth (tùy implementation)
  - chỉ participants của room được access link (policy)

---

## 12) TBD / Open questions (để hoàn thiện backend)
- Payment gateway chọn cái nào (VNPay/MoMo/Stripe?)
- Refund/cancel policy chi tiết
- KYC/Verification level cho buddy (Phase 2+)
- Safety nâng cao (SOS, location share)
- Notification stack (FCM/email/SMS)
- Multi-tenant city config & currency conversion

---

## 13) “How to use this doc” cho AI hỗ trợ backend
Khi bạn hỏi AI về backend, nên cung cấp context theo format:
- **Goal**: bạn muốn làm endpoint/module nào
- **Phase**: Phase 1 hay Phase 2
- **Data**: bảng liên quan + trạng thái
- **Constraints**: escrow, RBAC, multi-language
- **Output**: mong muốn (ERD, migration, API spec, code skeleton, tests)

Ví dụ:
> “Thiết kế API + schema cho Trip creation + chat room creation (Phase 1), đảm bảo RBAC traveler/CS, PostgreSQL migrations, và flow realtime chat.”
