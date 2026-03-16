## Realtime Chat (SignalR) - Hướng dẫn cho FE

Tài liệu này hướng dẫn FE kết nối tới `ChatHub` để làm chat realtime cho BonddyPlatform.

---

## 1. Endpoint & Auth

- **Hub URL**: `/hubs/chat`
  - Được map trong `Program.cs`:
    - `app.MapHub<ChatHub>("/hubs/chat");`
- **Auth**:
  - Hub được đánh `[Authorize]`, nên **bắt buộc phải gửi access token (JWT)** giống như khi gọi API.
  - Server sẽ đọc `NameIdentifier` trong JWT để lấy `userId`.

### 1.1. Cách truyền token (JS/TS)

Ví dụ với `@microsoft/signalr` trên FE:

```ts
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl('https://<api-base-url>/hubs/chat', {
    accessTokenFactory: () => localStorage.getItem('access_token') ?? ''
  })
  .withAutomaticReconnect()
  .build();
```

- `access_token` là JWT bạn lấy khi login qua API `Auth`.

---

## 2. Các method phía server mà FE có thể gọi

`ChatHub` định nghĩa 3 method public để FE gọi qua SignalR:

1. **JoinRoom**
   - Server: `Task JoinRoom(Guid roomId)`
   - FE gọi:
     ```ts
     await connection.invoke('JoinRoom', roomId);
     ```
   - Logic:
     - Lấy `userId` từ JWT.
     - Kiểm tra user có phải participant của room (`IChatService.IsUserInRoomAsync`).
     - Nếu hợp lệ, add connection vào group SignalR có tên `room-{roomId}`.

2. **LeaveRoom**
   - Server: `Task LeaveRoom(Guid roomId)`
   - FE gọi:
     ```ts
     await connection.invoke('LeaveRoom', roomId);
     ```
   - Logic:
     - Remove connection ra khỏi group `room-{roomId}`.

3. **SendMessage**
   - Server: `Task SendMessage(Guid roomId, string content)`
   - FE gọi:
     ```ts
     await connection.invoke('SendMessage', roomId, contentText);
     ```
   - Logic:
     - Lấy `userId` từ JWT, verify user thuộc room.
     - Gọi `_chatService.SendMessageAsync(roomId, userId, new SendMessageRequestDto { ContentText = content })`.
     - Sau khi lưu DB, broadcast xuống group `room-{roomId}` event:
       - Tên event: `"MessageCreated"`
       - Payload: `MessageDto` (giống API REST).

---

## 3. Event FE cần subscribe

Trong `ChatHub`, khi có tin nhắn mới:

- Server gọi:
  - `await Clients.Group(GetGroupName(roomId)).SendAsync("MessageCreated", message);`

Trên FE, sau khi tạo `connection`, cần subscribe:

```ts
connection.on('MessageCreated', (message) => {
  // message là MessageDto từ backend
  // Cập nhật UI: thêm message vào list của room tương ứng
});
```

- **MessageDto** (theo `ChatDtos.cs`):
  - `id`
  - `roomId`
  - `senderUserId`
  - `contentType` (Text / BookingCard / Image / File / System)
  - `contentText`
  - `bookingId`
  - `createdAt`

---

## 4. Quy trình chuẩn FE khi mở một room chat

1. **Lấy danh sách room của user**
   - Gọi REST API:
     - `GET /api/Chat/rooms`
   - Chọn room cần mở (ví dụ `roomId`).

2. **Lấy lịch sử tin nhắn ban đầu (paging)**
   - Gọi REST API:
     - `GET /api/Chat/rooms/{roomId}/messages?page=1&pageSize=20`
   - Render list messages hiện tại.

3. **Kết nối SignalR & join room**
   - Khởi tạo `HubConnection` với `accessTokenFactory`.
   - `await connection.start();`
   - `await connection.invoke('JoinRoom', roomId);`
   - Đăng ký handler:
     ```ts
     connection.on('MessageCreated', (message) => {
       if (message.roomId === roomId) {
         addMessageToUI(message);
       }
     });
     ```

4. **Gửi tin nhắn mới (realtime)**
   - Khi user nhấn gửi:
     ```ts
     await connection.invoke('SendMessage', roomId, inputText);
     ```
   - Nếu thành công:
     - Server lưu DB → broadcast `"MessageCreated"` → FE tự nhận event và cập nhật UI.
   - Lưu ý: không cần tự thêm tin nhắn local nếu bạn tin tưởng server, nhưng có thể:
     - Optimistic UI (thêm tạm lên list với trạng thái “sending”) rồi khi nhận event thì update.

5. **Rời room khi đóng màn hình chat**
   - Trước khi unmount component:
     ```ts
     await connection.invoke('LeaveRoom', roomId);
     await connection.stop();
     ```

---

## 5. Luồng realtime đã hoạt động chưa?

Dựa trên code hiện tại:

- `Program.cs` đã map hub: `app.MapHub<ChatHub>("/hubs/chat");`
- `ChatHub`:
  - Kiểm tra quyền bằng JWT (`[Authorize]`, `GetCurrentUserId`).
  - Join/Leave sử dụng **SignalR groups** theo `room-{roomId}`.
  - Gửi message:
    - Lưu DB qua `IChatService.SendMessageAsync` (re-use logic REST).
    - Broadcast `MessageCreated` tới group.
- `ChatService.SendMessageAsync`:
  - Kiểm tra user có thuộc room (thông qua `_uow.ChatParticipants.IsUserInRoomAsync`).
  - Lưu `Message` entity và trả `MessageDto`.

Kết luận:

- **Backend đã sẵn sàng cho chat realtime**:
  - Hub route có trong `Program.cs`.
  - Hub sử dụng groups + event `"MessageCreated"`.
- Điều kiện để realtime hoạt động:
  1. FE kết nối đúng tới `/hubs/chat` với **JWT hợp lệ**.
  2. FE **gọi `JoinRoom(roomId)`** trước khi gửi/nhận.
  3. FE **subscribe event `"MessageCreated"`** và update UI.

Nếu ba điều kiện trên được FE triển khai đúng, luồng chat sẽ realtime (không cần reload trang).

