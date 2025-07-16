using DataManagementApi.Data;
using DataManagementApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using DataManagementApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Register DataSeeder
builder.Services.AddScoped<DataSeeder>();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
		options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
	});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Jwt:Authority"];
    options.Audience = builder.Configuration["Jwt:Audience"];
    
    // Chỉ sử dụng cho môi trường development. Tắt yêu cầu HTTPS cho metadata.
    if (builder.Environment.IsDevelopment())
    {
        options.RequireHttpsMetadata = false;
    }

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Authority"],
        ValidAudience = builder.Configuration["Jwt:Audience"]
    };
    
    // --- ĐÂY LÀ NƠI XỬ LÝ LOGIC ---
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            // Lấy các service cần thiết từ Dependency Injection Container
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            
            // Lấy thông tin người dùng từ token đã được xác thực
            var claimsPrincipal = context.Principal;
            if (claimsPrincipal == null) return;

            // `sub` claim là ID duy nhất của user bên Keycloak
            var keycloakUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                context.Fail("Token không chứa Keycloak User ID (sub).");
                return;
            }

            // Kiểm tra xem user đã tồn tại trong DB của chúng ta chưa
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.KeycloakUserId == keycloakUserId);

            // Nếu user chưa tồn tại, tạo mới (Just-in-Time Provisioning)
            if (user == null)
            {
                var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
                var name = claimsPrincipal.FindFirst("name")?.Value ?? // Thử lấy claim "name"
                           claimsPrincipal.FindFirst("preferred_username")?.Value ?? // Hoặc "preferred_username"
                           "New User"; // Tên mặc định

                var newUser = new User
                {
                    KeycloakUserId = keycloakUserId,
                    Email = email,
                    Name = name,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                dbContext.Users.Add(newUser);
                await dbContext.SaveChangesAsync();
            }
        }
    };
    // --- KẾT THÚC LOGIC ---
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(myAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Seed data in development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
