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

builder.Services.AddDbContext<StoreContext>(x =>
    x.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddSignalR();

//================== Config Serilog write logs to file line 29-35 ===================
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
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<StoreContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICouponService, CouponService>();

builder.Services.AddScoped<IVnPayService, VnPayService>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().AllowCredentials()
    .WithOrigins("http://localhost:4200", "https://localhost:4200"));

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
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        await context.Database.MigrateAsync();
        await StoreSeedContext.SeedAsync(context, userManager);
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
	throw;
}

app.Run();
