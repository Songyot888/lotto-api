using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using lotto_api.Data;

var builder = WebApplication.CreateBuilder(args);

// อ่านจาก ConnectionStrings__DefaultConnection
var conn = builder.Configuration.GetConnectionString("DefaultConnection");

try
{
    using var c = new MySqlConnection(conn);
    await c.OpenAsync();
    Console.WriteLine("MySQL connected OK");
    await c.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine("MySQL connect failed: " + ex.Message);
    // ไม่ return; ให้ EF ลองด้วยก็ได้ แต่ log จะบอกชัดว่าพังเพราะอะไร
}

builder.Services.AddDbContext<ApplicationDBContext>(opt =>
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn)));

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
app.MapControllers();
app.Run();
