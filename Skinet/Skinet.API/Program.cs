using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Skinet.API.Middlewares;
using Skinet.API.SignalR;
using Skinet.Core.Entities;
using Skinet.Core.Interfaces;
using Skinet.Infastructure.Data;
using Skinet.Infastructure.Data.Repositories;
using Skinet.Infastructure.Services;
using Skinet.Infastructure.Services.VNPay;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<StoreContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSignalR();

//================== Config Serilog write logs to file
//Log.Logger = new LoggerConfiguration()
//    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
//    .CreateLogger();

//// Change default application logger
//builder.Host.UseSerilog();

builder.Services.AddCors();
builder.Services.AddSingleton<IConnectionMultiplexer>(config =>
{
    var connString = builder.Configuration.GetConnectionString("Redis")
     ?? throw new Exception("Can not get Redis connection");
    var configuration = ConfigurationOptions.Parse(connString, true);
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddSingleton<ICartService, CartService>();

builder.Services.AddIdentityApiEndpoints<AppUser>()
    .AddEntityFrameworkStores<StoreContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = false;  // Bảo mật cookie khỏi truy cập JavaScript
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // Cho phép trên HTTP (không HTTPS)
    options.Cookie.SameSite = SameSiteMode.None;  // Cho phép gửi trong cross-origin request
    //options.Cookie.Name = ".AspNetCore.Identity.Application";  // Đảm bảo tên chính xác
    //options.LoginPath = "/account/login";  // Đường dẫn login
});

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICouponService, CouponService>();

builder.Services.AddScoped<IVnPayService, VnPayService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

//app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
//     .WithOrigins("http://localhost:4200"));

app.UseCors(policy =>
    policy.WithOrigins("http://localhost:4200")  // Đúng địa chỉ frontend
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials());  // Cho phép gửi cookie

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGroup("api").MapIdentityApi<AppUser>();
app.MapHub<NotificationHub>("/hub/notifications");

// Seed Data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<StoreContext>();
        await context.Database.MigrateAsync();
        await StoreSeedContext.SeedAsync(context);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
	throw;
}

app.Run();
