
using lotto_api.Data;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// อ่านคอนเนกชันสตริงจาก appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// ระบุเวอร์ชัน DB ให้ตรง (แนะนำใช้รูปแบบตัวพิมพ์เล็ก)
var serverVersion = ServerVersion.Parse("10.6.21-mariadb");

builder.Services.AddDbContext<ApplicationDBContext>(options =>
    options.UseMySql(connectionString, serverVersion)
);

// Swagger & MVC
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddNewtonsoftJson(opt =>
{
    opt.SerializerSettings.ReferenceLoopHandling =
        Newtonsoft.Json.ReferenceLoopHandling.Ignore;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
// app.UseHttpsRedirection();
app.MapControllers();

app.Run();
