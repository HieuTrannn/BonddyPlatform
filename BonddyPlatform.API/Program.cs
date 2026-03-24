using System.Text;
using System.Text.Json.Serialization;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Implements;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;
using BonddyPlatform.Services.Implements;
using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BonddyPlatform.API.Hubs;
using BonddyPlatform.API.Realtime;
using BonddyPlatform.API.BackgroundJobs;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection(FirebaseOptions.SectionName));
builder.Services.Configure<PayPalOptions>(options =>
{
    builder.Configuration.GetSection(PayPalOptions.SectionName).Bind(options);
    options.BaseUrl = builder.Configuration["PAYPAL_BASE_URL"] ?? options.BaseUrl;
    options.ClientId = builder.Configuration["PAYPAL_CLIENT_ID"] ?? options.ClientId;
    options.Secret = builder.Configuration["PAYPAL_SECRET"] ?? options.Secret;
    options.WebhookId = builder.Configuration["PAYPAL_WEBHOOK_ID"] ?? options.WebhookId;
    options.ReturnUrl = builder.Configuration["PAYPAL_RETURN_URL"] ?? options.ReturnUrl;
    options.CancelUrl = builder.Configuration["PAYPAL_CANCEL_URL"] ?? options.CancelUrl;
});
builder.Services.AddHttpClient();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? new JwtOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };

        // SignalR uses a websocket/negotiation request; when the FE passes JWT via
        // access_token query string, we must read it from the query here.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                var accessToken = context.Request.Query["access_token"].FirstOrDefault();

                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/hubs/chat") ||
                     path.StartsWithSegments("/hubs/notifications")))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "https://bonddy.vn",
            "https://www.bonddy.vn",
            "https://bonddy.vercel.app",
            "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.MapType<Gender>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames(typeof(Gender))
            .Select(n => new Microsoft.OpenApi.Any.OpenApiString(n))
            .Cast<Microsoft.OpenApi.Any.IOpenApiAny>()
            .ToList()
    });
    c.MapType<Role>(() => new OpenApiSchema
    {
        Type = "string",
        Enum = Enum.GetNames(typeof(Role))
            .Select(n => new Microsoft.OpenApi.Any.OpenApiString(n))
            .Cast<Microsoft.OpenApi.Any.IOpenApiAny>()
            .ToList()
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var conn = builder.Configuration.GetConnectionString("DefaultConnection");

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    var port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port;

    conn = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={userInfo[0]};Password={userInfo[1]};Ssl Mode=Require;Trust Server Certificate=true";

}

builder.Services.AddDbContext<BonddyDbContext>(opt => opt.UseNpgsql(conn));

// Repositories + UoW
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IOtpVerificationRepository, OtpVerificationRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITripRepository, TripRepository>();
builder.Services.AddScoped<ITripRequestRepository, TripRequestRepository>();
builder.Services.AddScoped<IChatRoomRepository, ChatRoomRepository>();
builder.Services.AddScoped<IChatParticipantRepository, ChatParticipantRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IChatRoomUserStateRepository, ChatRoomUserStateRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IBuddyRepository, BuddyRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<FirebaseService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFirebaseAuthService, FirebaseAuthService>();
builder.Services.AddSignalR();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ITripRequestService, TripRequestService>();
builder.Services.AddScoped<IBuddyService, BuddyService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IChatReadService, ChatReadService>();
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddSingleton<IPresenceTracker, PresenceTracker>();

// Background job: tự động expire Trip và Booking khi thời gian đã qua
builder.Services.AddHostedService<StatusExpiryJob>();

var app = builder.Build();

// Auto migrate (Production or when enabled)
var runMigrations =
    app.Environment.IsProduction() ||
    builder.Configuration["RUN_MIGRATIONS"] == "true";

if (runMigrations)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BonddyDbContext>();
    var logger = scope.ServiceProvider
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("AutoMigration");

    const int maxRetries = 5;
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            logger.LogInformation("Applying migrations... attempt {Attempt}/{Max}", attempt, maxRetries);
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations applied successfully.");
            break;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Migration failed (attempt {Attempt}/{Max})", attempt, maxRetries);
            if (attempt == maxRetries) throw;
            await Task.Delay(TimeSpan.FromSeconds(3 * attempt));
        }
    }
}

var enableSwagger = builder.Configuration["ENABLE_SWAGGER"] == "true"
                    || app.Environment.IsDevelopment();

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
