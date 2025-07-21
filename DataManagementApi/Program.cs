using DataManagementApi.Data;
using DataManagementApi.Services;
using DataManagementApi.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
builder.Services.AddScoped<DepartmentAccessService>();
builder.Services.AddScoped<MatrixPermissionSeeder>();
builder.Services.AddScoped<MatrixPermissionService>();
builder.Services.AddScoped<CachedMatrixPermissionService>(); // Add cached service
builder.Services.AddScoped<MenuPathMigrationService>();
builder.Services.AddScoped<AdminUserSeeder>();

// Add Memory Cache for performance
builder.Services.AddMemoryCache();

// Add Authorization with Global Matrix Policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("GlobalMatrixPolicy", policy =>
        policy.Requirements.Add(new GlobalMatrixRequirement()));
    
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new GlobalMatrixRequirement())
        .Build();
});

// Register Global Authorization Handler
builder.Services.AddScoped<IAuthorizationHandler, GlobalMatrixAuthorizationHandler>();

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

    var validateAudience = builder.Configuration.GetValue<bool>("Jwt:ValidateAudience", true);
    Console.WriteLine($"JWT Debug: ValidateAudience setting: {validateAudience}");
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = validateAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Authority"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        // Cho phép multiple audiences để support cả Kong Gateway và Keycloak default
        ValidAudiences = new[]
        {
            builder.Configuration["Jwt:Audience"], // kong-gateway-client
            "account", // Keycloak default audience
            "realm-management" // Keycloak realm management
        }
    };
    
    // --- ĐÂY LÀ NƠI XỬ LÝ LOGIC ---
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault();
            Console.WriteLine($"JWT Debug: Authorization header: {(string.IsNullOrEmpty(token) ? "MISSING" : "PRESENT")}");
            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            Console.WriteLine("JWT Debug: Token validated successfully");
            
            // Debug JWT token claims
            var claimsPrincipal = context.Principal;
            if (claimsPrincipal != null)
            {
                Console.WriteLine("JWT Debug: All claims in token:");
                foreach (var claim in claimsPrincipal.Claims)
                {
                    Console.WriteLine($"  {claim.Type}: {claim.Value}");
                }
            }
            
            // Lấy các service cần thiết từ Dependency Injection Container
            var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
            
            // Lấy thông tin người dùng từ token đã được xác thực
            if (claimsPrincipal == null) 
            {
                Console.WriteLine("JWT Debug: ClaimsPrincipal is null");
                return;
            }

            // `sub` claim là ID duy nhất của user bên Keycloak
            var keycloakUserId = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"JWT Debug: Keycloak User ID: {keycloakUserId}");
            
            if (string.IsNullOrEmpty(keycloakUserId))
            {
                Console.WriteLine("JWT Debug: Token không chứa Keycloak User ID (sub)");
                context.Fail("Token không chứa Keycloak User ID (sub).");
                return;
            }

            // Lấy thông tin từ JWT token
            var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var name = claimsPrincipal.FindFirst("name")?.Value ?? // Thử lấy claim "name"
                       claimsPrincipal.FindFirst("preferred_username")?.Value ?? // Hoặc "preferred_username"
                       "New User"; // Tên mặc định

            // Kiểm tra xem user đã tồn tại trong DB của chúng ta chưa (theo KeycloakUserId hoặc email)
            var user = await dbContext.Users.FirstOrDefaultAsync(u => 
                u.KeycloakUserId == keycloakUserId || 
                (!string.IsNullOrEmpty(email) && u.Email == email));

            if (user == null)
            {
                // Nếu user chưa tồn tại, tạo mới (Just-in-Time Provisioning)
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

                // Gán role mặc định "Student" cho user mới
                var studentRole = await dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
                if (studentRole != null)
                {
                    var userRole = new UserRole
                    {
                        UserId = newUser.Id,
                        RoleId = studentRole.Id
                    };
                    dbContext.UserRoles.Add(userRole);
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine($"JWT Debug: Gán role Student cho user mới - UserId: {newUser.Id}, RoleId: {studentRole.Id}");
                }

                Console.WriteLine($"JWT Debug: Tạo user mới - Email: {email}, KeycloakUserId: {keycloakUserId}");
            }
            else if (string.IsNullOrEmpty(user.KeycloakUserId))
            {
                // Nếu user đã tồn tại nhưng chưa có KeycloakUserId, cập nhật nó
                user.KeycloakUserId = keycloakUserId;
                user.Name = name; // Cập nhật name nếu cần
                user.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                Console.WriteLine($"JWT Debug: Cập nhật KeycloakUserId cho user hiện tại - Email: {email}, KeycloakUserId: {keycloakUserId}");
            }
            else
            {
                Console.WriteLine($"JWT Debug: User đã tồn tại - Email: {email}, KeycloakUserId: {keycloakUserId}");
            }
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"JWT Debug: Authentication failed: {context.Exception.Message}");
            if (context.Exception.Message.Contains("Audience validation failed"))
            {
                Console.WriteLine("JWT Debug: This is an audience validation error. Check Keycloak client configuration.");
            }
            return Task.CompletedTask;
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
    
    // Migrate menu paths (remove /admin prefix)
    var menuMigrator = scope.ServiceProvider.GetRequiredService<MenuPathMigrationService>();
    await menuMigrator.MigrateMenuPathsAsync();
    
    // Seed matrix permissions
    var matrixSeeder = scope.ServiceProvider.GetRequiredService<MatrixPermissionSeeder>();
    await matrixSeeder.SeedMatrixPermissionsAsync();
    
    // Seed admin user and debug users
    var adminSeeder = scope.ServiceProvider.GetRequiredService<AdminUserSeeder>();
    await adminSeeder.SeedDefaultAdminAsync();
    await adminSeeder.ListAllUsersWithRolesAsync();
}

app.Run();
