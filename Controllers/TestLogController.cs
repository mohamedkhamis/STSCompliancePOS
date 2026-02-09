using System.Text;
using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Services;

namespace STSCompliancePOS.Controllers;

public class TestLogController(TestLogService testLog) : Controller
{
    public IActionResult Index()
    {
        var stats = testLog.GetStatistics();
        ViewBag.Total = stats["Total"];
        ViewBag.Passed = stats["Passed"];
        ViewBag.Failed = stats["Failed"];
        ViewBag.PassRate = stats["PassRate"];
        return View();
    }

    [HttpGet]
    public IActionResult GetLogs()
    {
        var logs = testLog.GetAllLogs();
        return Json(logs);
    }

    [HttpGet]
    public IActionResult ExportCsv()
    {
        var logs = testLog.GetAllLogs();
        var sb = new StringBuilder();

        sb.AppendLine("Id,Timestamp,TestVectorId,EA,Category,PAN,KeyRegister,TI,CreditType,Amount,MgmtType,MgmtValue,IssueDate,BaseDate,ExpectedToken,GeneratedToken,Passed,Error");

        foreach (var row in logs)
        {
            var fields = new[]
            {
                Csv(row.GetValueOrDefault("Id")),
                Csv(row.GetValueOrDefault("Timestamp")),
                Csv(row.GetValueOrDefault("TestVectorId")),
                Csv(row.GetValueOrDefault("EA")),
                Csv(row.GetValueOrDefault("Category")),
                Csv(row.GetValueOrDefault("PAN")),
                Csv(row.GetValueOrDefault("KeyRegister")),
                Csv(row.GetValueOrDefault("TI")),
                Csv(row.GetValueOrDefault("CreditType")),
                Csv(row.GetValueOrDefault("Amount")),
                Csv(row.GetValueOrDefault("MgmtType")),
                Csv(row.GetValueOrDefault("MgmtValue")),
                Csv(row.GetValueOrDefault("IssueDate")),
                Csv(row.GetValueOrDefault("BaseDate")),
                Csv(row.GetValueOrDefault("ExpectedToken")),
                Csv(row.GetValueOrDefault("GeneratedToken")),
                row.GetValueOrDefault("Passed")?.ToString() == "1" ? "PASS" : "FAIL",
                Csv(row.GetValueOrDefault("Error"))
            };
            sb.AppendLine(string.Join(",", fields));
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", $"TestLog_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }

    private static string Csv(object? value)
    {
        if (value == null) return "";
        var s = value.ToString() ?? "";
        if (s.Contains(',') || s.Contains('"') || s.Contains('\n'))
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
