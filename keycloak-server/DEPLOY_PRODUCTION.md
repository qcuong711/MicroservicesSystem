# Hướng dẫn triển khai Keycloak lên môi trường Production

Tài liệu này hướng dẫn các bước cần thiết để cấu hình và triển khai `keycloak-server` cho môi trường production. Chạy Keycloak trong production đòi hỏi cấu hình cẩn thận về cơ sở dữ liệu, mạng, và bảo mật.

## 1. Sử dụng Biến Môi Trường (Quan trọng)

Thay vì cấu hình cứng trong file `conf/keycloak.conf`, cách tốt nhất trong môi trường container là sử dụng **biến môi trường**. Keycloak hỗ trợ việc ghi đè cấu hình qua các biến môi trường theo một định dạng cụ thể.

Ví dụ, `db-url` trong file config sẽ tương ứng với biến môi trường `KC_DB_URL`.

## 2. Cấu hình Production

Dưới đây là các cấu hình quan trọng bạn cần thiết lập cho môi trường production thông qua các biến môi trường khi chạy container Keycloak.

### a. Cơ sở dữ liệu (Database)

Keycloak không nên sử dụng cơ sở dữ liệu H2 mặc định cho production. Dự án đã được cấu hình để dùng PostgreSQL, điều này là tốt. Tuy nhiên, bạn cần trỏ đến một instance PostgreSQL của production.

-   `KC_DB`: `postgres` (đã được cấu hình)
-   `KC_DB_URL`: `jdbc:postgresql://<your_db_host>:<port>/<your_db_name>` (ví dụ: `jdbc:postgresql://postgres-db:5432/keycloak_db` nếu chạy qua Docker Compose với service tên là `postgres-db`)
-   `KC_DB_USERNAME`: Tên người dùng của cơ sở dữ liệu production.
-   `KC_DB_PASSWORD`: Mật khẩu của cơ sở dữ liệu production.

### b. Hostname

Đây là cấu hình **quan trọng nhất**. Nó xác định địa chỉ public mà người dùng và ứng dụng sẽ sử dụng để truy cập Keycloak. Tất cả các URL trong token và trong giao diện của Keycloak sẽ được tạo dựa trên giá trị này.

-   `KC_HOSTNAME`: `your-keycloak-domain.com` (ví dụ: `auth.my-awesome-project.com`)

### c. Cấu hình HTTPS/TLS

Production **bắt buộc** phải dùng HTTPS. Keycloak cần được cấu hình để làm việc phía sau một reverse proxy (như Kong, Nginx) nơi sẽ xử lý TLS termination.

-   `KC_PROXY`: `edge`
    -   Chế độ này cho Keycloak biết rằng nó đang chạy sau một reverse proxy và các request đến nó qua kênh không mã hóa (HTTP), nhưng kết nối từ client đến proxy đã được mã hóa (HTTPS).
-   `KC_HTTP_ENABLED`: `true` (Keycloak sẽ lắng nghe trên cổng HTTP bên trong mạng container)
-   `KC_HTTP_PORT`: `8080`

Reverse proxy của bạn (API Gateway) sẽ nhận request trên cổng 443 (HTTPS) và forward đến Keycloak container trên cổng 8080 (HTTP).

### d. Tối ưu hóa cho Production

Bật các tính năng sau để tối ưu hóa và giám sát Keycloak.
-   `KC_HEALTH_ENABLED`: `true` (Bật endpoint `/health`)
-   `KC_METRICS_ENABLED`: `true` (Bật endpoint `/metrics` cho Prometheus)

## 3. Lệnh chạy Docker ví dụ

Dưới đây là một ví dụ về lệnh `docker run` để khởi chạy Keycloak với các cấu hình production. Lệnh này nên được tích hợp vào file `docker-compose.yml` của bạn.

```sh
docker run -d -p 8080:8080 \
  --name keycloak_prod \
  -e KC_DB=postgres \
  -e KC_DB_URL="jdbc:postgresql://<db_host>/<db_name>" \
  -e KC_DB_USERNAME="<user>" \
  -e KC_DB_PASSWORD="<password>" \
  -e KC_HOSTNAME="auth.your-domain.com" \
  -e KC_PROXY="edge" \
  -e KC_HTTP_ENABLED="true" \
  -e KC_HEALTH_ENABLED="true" \
  -e KC_METRICS_ENABLED="true" \
  -e KEYCLOAK_ADMIN="admin" \
  -e KEYCLOAK_ADMIN_PASSWORD="<strong_password>" \
  quay.io/keycloak/keycloak:latest \
  start
```

-   **`KEYCLOAK_ADMIN` / `KEYCLOAK_ADMIN_PASSWORD`**: Dùng để tạo tài khoản admin ban đầu. Hãy sử dụng một mật khẩu mạnh.
-   **Image `quay.io/keycloak/keycloak:latest`**: Sử dụng image chính thức từ Quay.io.
-   **Command `start`**: Khởi chạy Keycloak ở chế độ production (tối ưu hóa). Lệnh `start-dev` chỉ dành cho development.

## 4. Realm Configuration

-   **Export & Import**: Cấu hình realm của bạn từ môi trường development (clients, roles, users) nên được export ra một file JSON. Sau đó, bạn có thể import file này vào instance Keycloak production để đảm bảo tính nhất quán.
-   **Tự động Import (Tùy chọn)**: Bạn có thể tự động import một realm khi Keycloak khởi động bằng cách mount file export vào thư mục `/opt/keycloak/data/import` trong container.
    ```sh
    docker run ... -v /path/to/your/realm-export.json:/opt/keycloak/data/import/realm.json ...
    ``` 