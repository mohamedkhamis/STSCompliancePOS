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
builder.Services.AddScoped<ComplianceTestServiceEA11>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddSingleton<TestLogService>();

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

// Quick test endpoint for running individual CTSA tests
app.MapGet("/api/run/{testId}", async (string testId, string? utility, int? ea, IServiceProvider sp) =>
{
    using var scope = sp.CreateScope();
    var ea11 = scope.ServiceProvider.GetRequiredService<ComplianceTestServiceEA11>();
    var ea07 = scope.ServiceProvider.GetRequiredService<ComplianceTestService>();
    var vsm = sp.GetRequiredService<VSMConnectionService>();
    if (!vsm.IsConnected) return Results.Json(new { error = "VSM not connected" });
    if (vsm.Driver != null) vsm.Driver.EA = (ea ?? 11);
    var ut = utility ?? "E";
    TestRunResult result = testId.ToUpper() switch
    {
        "CTSA03" => ea == 7 ? await ea07.RunCTSA03() : await ea11.RunCTSA03(),
        "CTSA04" => ea == 7 ? await ea07.RunCTSA04() : await ea11.RunCTSA04(),
        "CTSA06" => ea == 7 ? await ea07.RunCTSA06() : await ea11.RunCTSA06(),
        "CTSA07" => ea == 7 ? await ea07.RunCTSA07() : await ea11.RunCTSA07(),
        "CTSA10" => ea == 7 ? await ea07.RunCTSA10(ut) : await ea11.RunCTSA10(ut),
        "CTSA12" => ea == 7 ? await ea07.RunCTSA12() : await ea11.RunCTSA12(),
        "CTSA13" => ea == 7 ? await ea07.RunCTSA13() : await ea11.RunCTSA13(),
        "CTSA16" => ea == 7 ? await ea07.RunCTSA16() : await ea11.RunCTSA16(),
        _ => new TestRunResult { TestId = testId, TestName = "Unknown" }
    };
    return Results.Json(new { result.TestId, result.TestName, Total = result.Steps.Count, result.PassedCount,
        Steps = result.Steps.Select(s => new { s.Description, s.Expected, s.Actual, s.Passed }) });
});

// Run full test suite
app.MapGet("/api/suite", async (string? utility, int? ea, bool? keychange, bool? currency, bool? extended, IServiceProvider sp) =>
{
    using var scope = sp.CreateScope();
    var ea11 = scope.ServiceProvider.GetRequiredService<ComplianceTestServiceEA11>();
    var ea07 = scope.ServiceProvider.GetRequiredService<ComplianceTestService>();
    var vsm = sp.GetRequiredService<VSMConnectionService>();
    if (!vsm.IsConnected) return Results.Json(new { error = "VSM not connected" });
    var ut = utility ?? "E";
    var kc = keychange ?? false;
    var cur = currency ?? false;
    var ext = extended ?? false;
    var results = new List<object>();
    async Task RunSuite(int eaVal)
    {
        if (vsm.Driver != null) vsm.Driver.EA = eaVal;
        var suite = eaVal == 11
            ? await ea11.RunFullSuite(ut, cur, kc, ext)
            : await ea07.RunFullSuite(ut, cur, kc, ext);
        foreach (var t in suite.Tests)
            results.Add(new { EA = eaVal, t.TestId, t.TestName, Total = t.Steps.Count, t.PassedCount,
                Steps = t.Steps.Select(s => new { s.Description, s.Expected, s.Actual, s.Passed }) });
    }
    if (ea == null || ea == 7) await RunSuite(7);
    if (ea == null || ea == 11) await RunSuite(11);
    int total = results.Count;
    return Results.Json(new { total, results });
});

// Diagnostic: query VSM register attributes
app.MapGet("/api/reg/{regId}", (string regId, IServiceProvider sp) =>
{
    var vsm = sp.GetRequiredService<VSMConnectionService>();
    if (!vsm.IsConnected || vsm.Driver == null) return Results.Json(new { error = "VSM not connected" });
    var response = vsm.Driver.GetKeyStatus(regId);
    return Results.Json(new { register = regId, response });
});

app.Run();
