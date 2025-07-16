# Hướng dẫn triển khai Kong API Gateway lên môi trường Production

Tài liệu này hướng dẫn cách cấu hình Kong API Gateway cho môi trường production. Chúng tôi khuyến khích sử dụng chế độ **DB-less** với file cấu hình khai báo (`kong.yml`), vì nó hiện đại, dễ quản lý qua Git và không cần vận hành một cơ sở dữ liệu riêng cho Kong.

## 1. Chuyển sang chế độ DB-less

Trong chế độ DB-less, toàn bộ cấu hình của Kong (services, routes, plugins) được định nghĩa trong một file YAML duy nhất.

### a. Tạo file cấu hình `kong.yml`

Tạo một file mới tên là `kong.yml` trong thư mục `kong-api-gateway`. File này sẽ định nghĩa cách Kong điều hướng request đến các dịch vụ của bạn.

Dưới đây là một ví dụ mẫu:

```yaml
_format_version: "3.0"
_transform: true

services:
  - name: backend-api-service
    url: http://datamanagementapi:8080 # Trỏ đến service backend API qua tên container
    routes:
      - name: api-routes
        paths:
          - /api
        strip_path: true
        
  - name: frontend-service
    url: http://ql-frontend:3000 # Trỏ đến service frontend qua tên container
    routes:
      - name: frontend-routes
        paths:
          - /
          
plugins:
  - name: jwt
    service: backend-api-service # Áp dụng plugin này cho service backend
    config:
      claims_to_verify:
        - exp
      key_claim_name: iss
      secret_is_base64: false
  - name: cors
    service: backend-api-service
    config:
      origins:
        - "https://your-frontend-domain.com" # Thay bằng domain frontend của bạn
      methods:
        - GET
        - POST
        - PUT
        - PATCH
        - DELETE
      headers:
        - "Authorization"
        - "Content-Type"
      exposed_headers:
        - "Content-Length"
      credentials: true
      max_age: 3600
```

### b. Plugin JWT và Keycloak

Kong cần xác thực các JWT được cấp bởi Keycloak. Để làm điều này, bạn cần đăng ký Keycloak như một `consumer` trong Kong và cung cấp public key của realm.

1.  **Lấy Public Key của Keycloak Realm:**
    Truy cập vào Keycloak Admin Console, vào **Realm Settings -> Keys**. Tìm public key của thuật toán RS256 và copy nó.

2.  **Tạo Consumer và JWT Credential qua Admin API:**
    Sau khi Kong khởi động, bạn cần chạy các lệnh sau (hoặc dùng tool như Postman) để cấu hình consumer cho Keycloak.

    ```bash
    # 1. Tạo một Consumer cho Keycloak
    curl -i -X POST http://localhost:8001/consumers/ \
      --data username=keycloak-issuer

    # 2. Thêm JWT credential cho consumer đó
    # Thay <your-keycloak-issuer-url> bằng giá trị issuer của bạn
    # Thay <your-realm-public-key> bằng public key bạn đã copy
    curl -i -X POST http://localhost:8001/consumers/keycloak-issuer/jwt \
      --data "key=<your-keycloak-issuer-url>" \
      --data "algorithm=RS256" \
      --data "rsa_public_key=<your-realm-public-key>"
    ```
    -   `key`: Chính là giá trị `iss` (issuer) trong token của Keycloak. Kong sẽ dùng claim này để tìm đúng public key để xác thực chữ ký.

## 2. Cấu hình Docker cho Kong DB-less

Trong file `docker-compose.yml` tổng, service Kong sẽ được cấu hình như sau:

```yaml
services:
  kong-gateway:
    image: kong/kong-gateway:latest
    container_name: kong-gateway
    volumes:
      - ./kong-api-gateway/kong.yml:/usr/local/kong/declarative/kong.yml
    environment:
      KONG_DATABASE: 'off'
      KONG_DECLARATIVE_CONFIG: /usr/local/kong/declarative/kong.yml
      KONG_PROXY_LISTEN: '0.0.0.0:8000, 0.0.0.0:8443 ssl'
      KONG_ADMIN_LISTEN: '0.0.0.0:8001'
      KONG_LOG_LEVEL: info
      KONG_PROXY_ACCESS_LOG: /dev/stdout
      KONG_ADMIN_ACCESS_LOG: /dev/stdout
      KONG_PROXY_ERROR_LOG: /dev/stderr
      KONG_ADMIN_ERROR_LOG: /dev/stderr
    ports:
      - "80:8000"   # Map cổng 80 của host tới cổng proxy HTTP của Kong
      - "443:8443"  # Map cổng 443 của host tới cổng proxy HTTPS của Kong
      - "8001:8001" # Admin API (chỉ nên expose trong mạng nội bộ hoặc được bảo vệ)
    restart: unless-stopped
```

### Lưu ý quan trọng:
-   `KONG_DATABASE: 'off'`: Bật chế độ DB-less.
-   `KONG_DECLARATIVE_CONFIG`: Trỏ đến file cấu hình YAML đã được mount vào container.
-   **Admin API (cổng 8001)**: Trong môi trường production, cổng này **KHÔNG** nên được mở ra Internet một cách công khai. Bạn chỉ nên truy cập nó qua một mạng nội bộ, VPN, hoặc một Bastion Host.
-   **HTTPS**: Cấu hình trên giả định bạn sẽ cung cấp chứng chỉ SSL/TLS cho Kong. Bạn có thể làm điều này bằng cách thêm các biến môi trường `KONG_SSL_CERT` và `KONG_SSL_CERT_KEY` hoặc quản lý chúng qua Kong Admin API. 