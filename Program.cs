using STSCompliancePOS.Models;
using STSCompliancePOS.Services;
using STSCompliancePOS.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Load VSM configuration from appsettings.json
var vsmConfig = builder.Configuration.GetSection("VSMConfiguration").Get<VSMConfiguration>() 
    ?? new VSMConfiguration();
builder.Services.AddSingleton(vsmConfig);

// Register services
builder.Services.AddSingleton<STSTestDataService>();
builder.Services.AddSingleton<VSMConnectionService>();
builder.Services.AddSingleton<TestResultsStore>();
builder.Services.AddScoped<ComplianceTestService>();
builder.Services.AddScoped<ReportService>();

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
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR hub
app.MapHub<TestHub>("/testhub");

app.Run();
