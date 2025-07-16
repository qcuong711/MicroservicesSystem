# Hướng dẫn triển khai toàn bộ hệ thống với Docker Compose

Tài liệu này cung cấp một file `docker-compose.yml` hoàn chỉnh để triển khai toàn bộ hệ thống (Frontend, Backend, Keycloak, Kong) lên môi trường production.

## 1. Tổng quan kiến trúc

Kiến trúc production sẽ bao gồm các container sau:
1.  **kong-gateway**: API Gateway, là điểm vào (entrypoint) duy nhất cho tất cả traffic từ bên ngoài. Xử lý SSL/TLS, routing, và xác thực JWT.
2.  **ql-frontend**: Container chứa ứng dụng Next.js đã được build.
3.  **datamanagementapi**: Container chứa ứng dụng ASP.NET Core API.
4.  **keycloak**: Identity Server, cấp và xác thực token.
5.  **keycloak-db**: Cơ sở dữ liệu PostgreSQL cho Keycloak.

Tất cả các service sẽ được kết nối qua một mạng chung (`production-net`) và giao tiếp với nhau qua tên container.

## 2. Chuẩn bị file

### a. File `.env`
Tạo một file tên là `.env` ở thư mục gốc của dự án (cùng cấp với `docker-compose.yml`). File này sẽ chứa tất cả các biến môi trường và thông tin bí mật. **KHÔNG** commit file này vào source control.

```env
# Domain của bạn
#=============================
FRONTEND_DOMAIN=your-domain.com
KEYCLOAK_DOMAIN=auth.your-domain.com
API_DOMAIN=api.your-domain.com

# Keycloak Configuration
#=============================
KEYCLOAK_ADMIN_USER=admin
KEYCLOAK_ADMIN_PASSWORD=a_very_strong_password_for_keycloak_admin
KC_DB_USERNAME=keycloak
KC_DB_PASSWORD=a_very_strong_password_for_kc_db
KC_DB_NAME=keycloak_db
KC_DB_HOST=keycloak-db

# Frontend (NextAuth.js) Configuration
#=============================
# Client ID và Secret này bạn tạo trong Keycloak Admin Console
AUTH_KEYCLOAK_ID=my-frontend-client
AUTH_KEYCLOAK_SECRET=a_very_strong_client_secret
# Tạo bằng lệnh 'openssl rand -base64 32'
AUTH_SECRET=a_super_secret_string_for_nextauth

# Backend API Configuration
#=============================
# Chuỗi kết nối này trỏ đến DB của ứng dụng, không phải DB của Keycloak
DB_CONNECTION_STRING="Server=your_external_db_host;Database=school_management_prod_db;User Id=user;Password=your_db_password"

# Kong Configuration
#=============================
KONG_ADMIN_USER=kong_admin
KONG_ADMIN_PASSWORD=a_very_strong_password_for_kong_admin
```

### b. File `kong.yml`
Đảm bảo bạn đã tạo file `kong-api-gateway/kong.yml` như hướng dẫn trong `kong-api-gateway/DEPLOY_PRODUCTION.md`.

## 3. File `docker-compose.production.yml`

Tạo một file mới tên là `docker-compose.production.yml` ở thư mục gốc.

```yaml
version: '3.8'

networks:
  production-net:
    driver: bridge

volumes:
  keycloak-db-data:
    driver: local

services:
  # 1. Cơ sở dữ liệu cho Keycloak
  keycloak-db:
    image: postgres:13
    container_name: keycloak-db-prod
    volumes:
      - keycloak-db-data:/var/lib/postgresql/data
    environment:
      POSTGRES_DB: ${KC_DB_NAME}
      POSTGRES_USER: ${KC_DB_USERNAME}
      POSTGRES_PASSWORD: ${KC_DB_PASSWORD}
    networks:
      - production-net
    restart: unless-stopped

  # 2. Keycloak Identity Server
  keycloak:
    image: quay.io/keycloak/keycloak:latest
    container_name: keycloak-prod
    command: start
    environment:
      KEYCLOAK_ADMIN: ${KEYCLOAK_ADMIN_USER}
      KEYCLOAK_ADMIN_PASSWORD: ${KEYCLOAK_ADMIN_PASSWORD}
      KC_DB: postgres
      KC_DB_URL_HOST: ${KC_DB_HOST}
      KC_DB_URL_DATABASE: ${KC_DB_NAME}
      KC_DB_USERNAME: ${KC_DB_USERNAME}
      KC_DB_PASSWORD: ${KC_DB_PASSWORD}
      KC_HOSTNAME: ${KEYCLOAK_DOMAIN}
      KC_PROXY: edge
      KC_HTTP_ENABLED: true
      KC_HEALTH_ENABLED: true
      KC_METRICS_ENABLED: true
    networks:
      - production-net
    depends_on:
      - keycloak-db
    restart: unless-stopped

  # 3. Backend API
  datamanagementapi:
    build:
      context: ./DataManagementApi
      dockerfile: Dockerfile
    image: datamanagementapi-prod
    container_name: datamanagementapi-prod
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__DefaultConnection: ${DB_CONNECTION_STRING}
      Jwt__Authority: "https://${KEYCLOAK_DOMAIN}/realms/my-realm" # Chỉnh 'my-realm' nếu bạn dùng tên khác
      CorsOrigins: "https://${FRONTEND_DOMAIN}"
    networks:
      - production-net
    restart: unless-stopped

  # 4. Frontend Application
  ql-frontend:
    build:
      context: ./QL_frontend/quanly-khoaluan-thuctap
      dockerfile: Dockerfile
      args:
        NEXT_PUBLIC_API_BASE_URL: "https://${API_DOMAIN}/api"
    image: ql-frontend-prod
    container_name: ql-frontend-prod
    environment:
      NODE_ENV: production
      AUTH_KEYCLOAK_ID: ${AUTH_KEYCLOAK_ID}
      AUTH_KEYCLOAK_SECRET: ${AUTH_KEYCLOAK_SECRET}
      AUTH_KEYCLOAK_ISSUER: "https://${KEYCLOAK_DOMAIN}/realms/my-realm" # Chỉnh 'my-realm' nếu bạn dùng tên khác
      AUTH_SECRET: ${AUTH_SECRET}
      NEXT_PUBLIC_API_BASE_URL: "https://${API_DOMAIN}/api"
    networks:
      - production-net
    restart: unless-stopped

  # 5. Kong API Gateway
  kong-gateway:
    image: kong/kong-gateway:latest
    container_name: kong-gateway-prod
    volumes:
      - ./kong-api-gateway/kong.yml:/usr/local/kong/declarative/kong.yml
    environment:
      KONG_DATABASE: 'off'
      KONG_DECLARATIVE_CONFIG: /usr/local/kong/declarative/kong.yml
      KONG_PROXY_LISTEN: '0.0.0.0:8000, 0.0.0.0:8443 ssl'
      KONG_ADMIN_LISTEN: '0.0.0.0:8001' # Chỉ nên truy cập nội bộ
      KONG_LOG_LEVEL: info
    ports:
      - "80:8000"
      - "443:8443"
      - "8001:8001" # Cẩn thận khi mở cổng này ra ngoài
    networks:
      - production-net
    depends_on:
      - ql-frontend
      - datamanagementapi
      - keycloak
    restart: unless-stopped
```

## 4. Chạy hệ thống
1.  **Điền thông tin vào file `.env`**.
2.  **Build tất cả các image:**
    ```sh
    docker-compose -f docker-compose.production.yml build
    ```
3.  **Khởi chạy toàn bộ hệ thống:**
    ```sh
    docker-compose -f docker-compose.production.yml up -d
    ```
4.  **Cấu hình Kong Consumer:**
    Sau khi hệ thống khởi chạy, đừng quên cấu hình JWT consumer cho Keycloak như hướng dẫn trong `kong-api-gateway/DEPLOY_PRODUCTION.md`.

5.  **Dừng hệ thống:**
    ```sh
    docker-compose -f docker-compose.production.yml down
    ``` 