using Isa.Flow.Manager;
using Isa.Flow.Manager.Data;
using Isa.Flow.Manager.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Serilog;
using Environment = Isa.Flow.Manager.Environment;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Logger(c => c.MinimumLevel.Verbose().WriteTo.Console())
    .WriteTo.Logger(c => c.MinimumLevel.Error().WriteTo.File("error.log", rollingInterval: RollingInterval.Day))
    .CreateLogger();

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddSerilog();
});

var config = builder.Configuration;
builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(config["ConnectionString"]));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton(sp =>
{
    var memoryCache = sp.GetRequiredService<IMemoryCache>();

    return new ManagerActor(Environment.RabbitConnectionFactory, builder.Configuration, memoryCache, builder.Configuration["ActorId"]);
});

builder.Services.AddTransient<SourceService>();
builder.Services.AddTransient<ArticleService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}");

app.Run();