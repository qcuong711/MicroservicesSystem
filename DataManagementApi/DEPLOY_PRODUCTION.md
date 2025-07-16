# Hướng dẫn triển khai DataManagementApi lên môi trường Production

Tài liệu này hướng dẫn các bước cần thiết để cấu hình và triển khai dự án `DataManagementApi` lên môi trường production một cách an toàn và hiệu quả.

## 1. Cấu hình Production

### a. Tạo file `appsettings.Production.json`

Trong môi trường production, chúng ta không nên sử dụng cài đặt từ `appsettings.json`. Thay vào đó, hãy tạo một file mới `DataManagementApi/appsettings.Production.json` với nội dung sau và điều chỉnh cho phù hợp với môi trường của bạn:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_production_db_server;Database=your_prod_db_name;User Id=your_user;Password=your_password;"
  },
  "Jwt": {
    "Authority": "https://your-keycloak-domain.com/realms/your-realm",
    "Audience": "kong-gateway-client"
  },
  "AllowedHosts": "your-api-domain.com",
  "CorsOrigins": "https://your-frontend-domain.com"
}
```

**Lưu ý quan trọng:**
- **`ConnectionStrings__DefaultConnection`**: **KHÔNG BAO GIỜ** lưu chuỗi kết nối chứa thông tin nhạy cảm (mật khẩu) trực tiếp vào file cấu hình trong source control. Thay vào đó, hãy sử dụng **biến môi trường** hoặc một dịch vụ quản lý bí mật như Azure Key Vault, AWS Secrets Manager. Tên biến môi trường tương ứng sẽ là `ConnectionStrings__DefaultConnection`.
- **`Jwt__Authority`**: Cập nhật với URL của Keycloak server production.
- **`AllowedHosts`**: Chỉ định domain mà API của bạn sẽ phục vụ.
- **`CorsOrigins`**: Thêm một thuộc tính mới để cấu hình CORS động. Chúng ta sẽ cập nhật `Program.cs` để đọc giá trị này.

### b. Cập nhật `Program.cs` để đọc cấu hình CORS

Mở file `Program.cs` và thay đổi phần cấu hình CORS để đọc giá trị từ `appsettings.Production.json`:

```csharp
// ... existing code ...
var myAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
                      policy =>
                      {
                          var corsOrigins = builder.Configuration.GetValue<string>("CorsOrigins");
                          if (builder.Environment.IsDevelopment())
                          {
                              policy.WithOrigins("http://localhost:5500", "http://localhost:5173", "http://localhost:5174")
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                          }
                          else if (!string.IsNullOrEmpty(corsOrigins))
                          {
                              policy.WithOrigins(corsOrigins.Split(','))
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                          }
                      });
});

// ... existing code ...

// Trong pipeline, bật lại HTTPS Redirection cho production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(myAllowSpecificOrigins);
// ... existing code ...
```

## 2. Đóng gói với Docker (Containerization)

Sử dụng Docker là phương pháp hiện đại và linh hoạt để triển khai.

### a. Tạo `Dockerfile`

Tạo một file mới tên là `Dockerfile` trong thư mục gốc của `DataManagementApi` với nội dung sau:

```dockerfile
# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["DataManagementApi.csproj", "."]
RUN dotnet restore "./DataManagementApi.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/."
RUN dotnet build "DataManagementApi.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM build AS publish
RUN dotnet publish "DataManagementApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Create the final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Set the environment to Production
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port 8080 for ASP.NET Core 8.0 default
EXPOSE 8080

ENTRYPOINT ["dotnet", "DataManagementApi.dll"]
```

### b. Tạo file `.dockerignore`
Để tối ưu hóa quá trình build Docker image, tạo file `.dockerignore` trong thư mục `DataManagementApi` để loại bỏ các file/thư mục không cần thiết:

```
**/.dockerignore
**/.env
**/.git
**/.gitignore
**/.vs
**/.vscode
**/bin/
**/obj/
```

## 3. Build và Chạy ứng dụng
1.  **Build Docker image:**
    Mở terminal trong thư mục `DataManagementApi` và chạy lệnh:
    ```sh
    docker build -t datamanagementapi:latest .
    ```

2.  **Chạy Docker container:**
    Khi chạy, bạn cần cung cấp các biến môi trường cho các thông tin nhạy cảm.
    ```sh
    docker run -d -p 8080:8080 \
      -e ASPNETCORE_ENVIRONMENT=Production \
      -e ConnectionStrings__DefaultConnection="your_production_connection_string" \
      -e Jwt__Authority="https://your-keycloak-domain.com/realms/your-realm" \
      -e CorsOrigins="https://your-frontend-domain.com" \
      --name my-api datamanagementapi:latest
    ```
    - `-d`: Chạy container ở chế độ detached (nền).
    - `-p 8080:8080`: Map cổng 8080 của máy host tới cổng 8080 của container.
    - `-e`: Dùng để truyền các biến môi trường vào container. Đây là cách an toàn để quản lý cấu hình production.

Bằng cách tuân theo các bước trên, bạn có thể triển khai `DataManagementApi` lên môi trường production một cách an toàn và có cấu trúc. 