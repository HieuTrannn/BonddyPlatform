using System;
using BonddyPlatform.Repositories.Implements;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Persistences;
using BonddyPlatform.Services.Implements;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<BonddyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories + UoW
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services
builder.Services.AddScoped<IContactService, ContactService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BonddyDbContext>();
    db.Database.Migrate();
}


var enableSwagger = builder.Configuration["ENABLE_SWAGGER"] == "true"
                    || app.Environment.IsDevelopment();

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
