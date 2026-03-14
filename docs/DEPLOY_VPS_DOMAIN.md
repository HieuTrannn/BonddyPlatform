# Hướng dẫn cấu hình VPS và tên miền để deploy Bonddy

Sau khi mua **VPS** (Ubuntu 24.04 LTS) và **tên miền** (bonddy.vn), làm lần lượt theo các bước dưới.

---

## Phần 1: Thông tin cần có sau khi mua

- **VPS**: Địa chỉ IP, tài khoản root (hoặc user + mật khẩu), port SSH (thường 22).
- **Tên miền**: Đã trỏ về nhà quản lý DNS (mặc định Vietnix: ns1.vietnix.net, ns2.vietnix.net).

---

## Phần 2: Cấu hình tên miền (DNS)

### 2.1 Giữ Nameserver mặc định của Vietnix

- Không cần đổi Nameserver 1/2 (ns1.vietnix.net, ns2.vietnix.net).
- Vào **Quản lý tên miền** → chọn **bonddy.vn** → **Quản lý DNS** / **Thiết lập DNS**.

### 2.2 Giữ nguyên bản ghi NS

- **Không xóa** các bản ghi **NS** (ns1.vietnix.net, ns2.vietnix.net, nsbak.vietnix.net). Đó là cấu hình nameserver của domain.

### 2.3 Thêm bản ghi A trỏ về VPS

Bấm nút **Thêm bản ghi** (hoặc Add / +), thêm **3 bản ghi** sau. Thay **YOUR_VPS_IP** bằng địa chỉ IP thật của VPS (ví dụ `103.xxx.xxx.xxx`).

| NAME (Tên / Host) | TYPE | CONTENT (Giá trị) | PRIO | TTL |
|-------------------|------|-------------------|------|-----|
| `@`               | **A** | `YOUR_VPS_IP`     | để trống hoặc 0 | 1 hour (hoặc 3600) |
| `www`             | **A** | `YOUR_VPS_IP`     | để trống hoặc 0 | 1 hour (hoặc 3600) |
| `api`             | **A** | `YOUR_VPS_IP`     | để trống hoặc 0 | 1 hour (hoặc 3600) |

**Cách điền trên giao diện Vietnix:**
- **NAME**: Gõ `@` cho bản ghi thứ nhất (tức bonddy.vn), `www` cho bản ghi thứ hai, `api` cho bản ghi thứ ba. Một số hệ thống dùng "Host" và cho phép để trống thay cho `@`.
- **TYPE**: Chọn **A**.
- **CONTENT** (hoặc Value/Point to): Gõ **IP VPS** (chỉ số, ví dụ `103.45.67.89`), không có `http://` hay dấu `/`.
- **PRIO**: Để trống hoặc 0 (bản ghi A thường không dùng priority).
- **TTL**: Để mặc định (1 hour / 3600) hoặc 300.

Lưu từng bản ghi sau khi thêm. Đợi **5–30 phút** (có thể đến vài giờ) để DNS cập nhật toàn cầu.

### 2.4 Kiểm tra DNS (ping / nslookup)

Trên máy Windows (PowerShell hoặc CMD) hoặc máy Mac/Linux:

```bash
# Kiểm tra bonddy.vn
ping bonddy.vn

# Kiểm tra www.bonddy.vn
ping www.bonddy.vn

# Kiểm tra api.bonddy.vn
ping api.bonddy.vn
```

**Kết quả mong đợi:** Dòng "Reply from **YOUR_VPS_IP**" — địa chỉ IP trùng với IP VPS bạn đã điền trong bản ghi A.

**Nếu ping bị chặn trên VPS** (nhiều nhà cung cấp tắt ICMP): dùng `nslookup` thay vì ping để chỉ kiểm tra DNS đã trỏ đúng IP:

```bash
nslookup bonddy.vn
nslookup www.bonddy.vn
nslookup api.bonddy.vn
```

Trong kết quả, mục **Address** (hoặc Addresses) phải là **IP VPS** của bạn.

**Ví dụ (Windows PowerShell):**
```powershell
nslookup api.bonddy.vn
# Kết quả mong đợi có dòng:
# Address:  103.xxx.xxx.xxx   (đúng IP VPS)
```

---

## Phần 3: Kết nối và bảo mật cơ bản VPS

Thay **IP_VPS** bằng IP thật (ví dụ `103.200.20.106`). Port SSH mặc định: **22**.

### 3.1 SSH vào VPS

**Trên Windows (PowerShell hoặc CMD):**
```bash
ssh root@IP_VPS
```
Ví dụ: `ssh root@103.200.20.106`

- Lần đầu có thể hỏi "Are you sure you want to continue connecting?" → gõ `yes` rồi Enter.
- Khi hỏi password: gõ **mật khẩu root** (mật khẩu không hiện khi gõ), xong Enter.

**Nếu dùng PuTTY:** Mở PuTTY → Host Name: `IP_VPS`, Port: `22`, Connection type: SSH → Open → login: `root`, password: nhập khi được hỏi.

Khi thấy dòng dạng `root@bonddy:~#` hoặc `root@ubuntu:~$` là đã vào VPS.

---

### 3.2 Cập nhật hệ thống

Chạy lần lượt (copy từng khối vào terminal trong VPS):

```bash
sudo apt update
```
Đợi xong, rồi:

```bash
sudo apt upgrade -y
```
Đợi cập nhật xong (có thể vài phút).

---

### 3.3 Đổi hostname (tùy chọn)

Để tên máy hiển thị là `bonddy`:

```bash
sudo hostnamectl set-hostname bonddy
```

Kiểm tra: `hostname` → in ra `bonddy`.

---

### 3.4 Tạo user không dùng root (khuyến nghị)

Tạo user `bonddy` (hoặc tên khác) để không phải dùng root hàng ngày:

```bash
adduser bonddy
```

- Nhập **mật khẩu** cho user (gõ 2 lần khi được hỏi).
- Full Name, Room number... có thể bỏ trống (Enter).

Cho user quyền sudo:

```bash
usermod -aG sudo bonddy
```

Sau này có thể đăng nhập bằng: `ssh bonddy@IP_VPS` và dùng `sudo` khi cần lệnh quyền admin.

---

### 3.5 Cấu hình firewall (UFW)

Mở port SSH, HTTP, HTTPS rồi bật firewall:

```bash
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

Khi hỏi "Command may disrupt existing ssh connections. Proceed?" → gõ `y` rồi Enter.

Kiểm tra:

```bash
sudo ufw status
```

Nên thấy: 22, 80, 443 ALLOW. Status: active.

---

### 3.6 Kiểm tra nhanh

- `hostname` → bonddy (nếu đã đổi).
- `sudo ufw status` → active, 22/80/443 allow.
- Vẫn SSH được (nếu bị ngắt, đăng nhập lại bằng root hoặc bonddy).

Xong phần 3. Tiếp theo: **Phần 4** – cài Docker hoặc .NET + Nginx + PostgreSQL.

---

## Phần 4: Cài đặt môi trường trên VPS

Chọn **một trong hai** cách: **Docker** (gọn, dễ rollback) hoặc **cài trực tiếp** (.NET + Nginx + PostgreSQL).

### Cách A: Dùng Docker (khuyến nghị)

**Bước 1 – Cài Docker trên VPS**

```bash
curl -fsSL https://get.docker.com | sh
sudo usermod -aG docker $USER
```

Đăng xuất SSH rồi đăng nhập lại (hoặc chạy `newgrp docker`) để dùng lệnh `docker` không cần `sudo`.

**Bước 2 – Đưa code lên VPS**

- Clone repo (hoặc dùng SCP/rsync) vào thư mục ví dụ `~/bonddy`. Trong thư mục phải có `docker-compose.yml`, `BonddyPlatform.API/`, `BonddyPlatform.Services/`, `BonddyPlatform.Repositories/`.

**Bước 3 – Cấu hình biến môi trường**

Trên VPS, vào thư mục chứa `docker-compose.yml`:

```bash
cp .env.example .env
nano .env
```

Sửa ít nhất:
- `POSTGRES_PASSWORD`: mật khẩu mạnh cho DB.
- `JWT_SECRET`: chuỗi bí mật ≥ 32 ký tự (production nên dùng chuỗi random).

Lưu file (Ctrl+O, Enter, Ctrl+X).

**Bước 4 – Build và chạy**

```bash
docker compose up -d --build
```

Lần đầu sẽ build image API và tải image PostgreSQL, có thể mất vài phút. Kiểm tra:

```bash
docker compose ps
```

Hai container `bonddy-api` và `bonddy-postgres` phải ở trạng thái **Up**. API lắng nghe port **8080** trên VPS (Nginx sẽ proxy 80/443 → 8080 ở bước sau).

**Migration database:** Ứng dụng Bonddy đã cấu hình tự chạy migration khi khởi động (`MigrateAsync` trong `Program.cs`), nên không cần chạy `dotnet ef database update` thủ công. Lần đầu `docker compose up -d`, API sẽ áp dụng migration và tạo bảng.

### Cách B: Cài trực tiếp

- **.NET 8 Runtime**: theo hướng dẫn Microsoft cho Ubuntu 24.04.
- **PostgreSQL**: `sudo apt install postgresql postgresql-contrib`.
- **Nginx**: `sudo apt install nginx` (làm reverse proxy + SSL).

---

## Phần 5: Reverse proxy và SSL (HTTPS)

### 5.1 Cài Nginx (nếu chưa dùng Docker cho Nginx)

```bash
sudo apt install nginx -y
```

### 5.2 Cài Certbot (Let's Encrypt)

```bash
sudo apt install certbot python3-certbot-nginx -y
```

### 5.3 Tạo file cấu hình Nginx

Trên VPS, tạo file cấu hình cho **api.bonddy.vn** (proxy về API port 8080) và **bonddy.vn** / **www.bonddy.vn**.

**Cách 1 – Dùng file mẫu trong repo**

Trong thư mục `~/BonddyPlatform` (sau khi clone/pull):

```bash
sudo cp docs/nginx-bonddy.conf.example /etc/nginx/sites-available/bonddy.vn
```

**Cách 2 – Tạo file bằng tay**

```bash
sudo nano /etc/nginx/sites-available/bonddy.vn
```

Dán nội dung sau (API chạy port **8080**):

```nginx
server {
    listen 80;
    server_name api.bonddy.vn;
    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
    location /hubs/ {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
server {
    listen 80;
    server_name bonddy.vn www.bonddy.vn;
    root /var/www/html;
    index index.html;
    location / { try_files $uri $uri/ /index.html; }
}
```

Lưu file (nano: Ctrl+O, Enter, Ctrl+X).

**Bật site và kiểm tra:**

```bash
sudo ln -sf /etc/nginx/sites-available/bonddy.vn /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

`nginx -t` phải báo "syntax is ok". Nếu có site mặc định conflict (default), có thể xóa: `sudo rm /etc/nginx/sites-enabled/default`.

### 5.4 Lấy SSL (Certbot)

```bash
sudo certbot --nginx -d bonddy.vn -d www.bonddy.vn -d api.bonddy.vn
```

Làm theo hướng dẫn (email, đồng ý điều khoản). Certbot sẽ tự chỉnh file Nginx để dùng HTTPS.

---

## Phần 6: Deploy Backend (Bonddy API)

### 6.1 Đưa code lên VPS

- Dùng **git clone** (nếu repo private: SSH key hoặc deploy key), hoặc **SCP/rsync** đẩy thư mục publish.

### 6.2 Cấu hình ứng dụng

- File **appsettings.Production.json** hoặc biến môi trường: `ConnectionStrings__DefaultConnection`, JWT secret, CORS origins (https://bonddy.vn, https://www.bonddy.vn), v.v.

### 6.3 Chạy API

- **Docker**: chạy container từ image, map port (ví dụ 5000).
- **Trực tiếp**: `dotnet BonddyPlatform.API.dll` hoặc dùng **systemd** (tạo file service) để chạy nền và tự khởi động lại.

### 6.4 Chạy migration database

```bash
dotnet ef database update
# hoặc chạy trong container nếu dùng Docker
```

---

## Phần 7: Frontend (Next.js)

- **Nếu deploy trên cùng VPS**: Build Next.js, chạy bằng Node (PM2) hoặc serve static qua Nginx.
- **Nếu dùng Vercel**: Deploy Next.js lên Vercel, cấu hình domain bonddy.vn (hoặc www) trong Vercel; API gọi về `https://api.bonddy.vn`.

---

## Phần 8: Kiểm tra cuối

- Truy cập https://bonddy.vn (hoặc https://www.bonddy.vn).
- Gọi https://api.bonddy.vn (ví dụ health hoặc swagger nếu bật).
- Kiểm tra CORS, JWT, kết nối DB.

---

## Tóm tắt thứ tự làm

| Bước | Nội dung |
|------|----------|
| 1 | Có IP VPS + thông tin đăng nhập, mua xong tên miền |
| 2 | Trỏ DNS: A record @, www, api → IP VPS |
| 3 | SSH vào VPS, cập nhật OS, firewall, (tùy chọn) user mới |
| 4 | Cài Docker (hoặc .NET + Nginx + PostgreSQL) |
| 5 | Cấu hình Nginx + Certbot SSL cho bonddy.vn và api.bonddy.vn |
| 6 | Deploy API, cấu hình connection string và env, chạy migration |
| 7 | Deploy frontend (trên VPS hoặc Vercel) |
| 8 | Kiểm tra HTTPS, API, CORS |

Khi bạn đã mua xong và có IP VPS + tên miền đang dùng Vietnix, báo lại để đi chi tiết từng bước (ví dụ từ Bước 2 hoặc 3) với thông tin thực tế của bạn.
