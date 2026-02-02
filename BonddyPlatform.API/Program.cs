using System.Text;
using BonddyPlatform.API.Options;
using BonddyPlatform.API.Services;
using BonddyPlatform.Repositories.Implements;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;
using BonddyPlatform.Services.Implements;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));

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
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(
            "https://bonddy.vercel.app",
            "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddSwaggerGen(c =>
{
    c.EnableAnnotations();
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
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
builder.Services.AddScoped<IOtpVerificationRepository, OtpVerificationRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BonddyDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("DB migrate failed: " + ex.Message);
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

app.Run();
