using lotto_api.Data;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;


var builder = WebApplication.CreateBuilder(args);

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
