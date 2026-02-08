// =============================================================================
//  ReportService.cs — CSV Export and Report Generation
//  Generates compliance test reports in various formats
// =============================================================================

using System.Text;

namespace STSCompliancePOS.Services;

public class ReportService(VSMConfiguration config, IWebHostEnvironment env)
{
    // =========================================================================
    //  Export to CSV
    // =========================================================================
    public string ExportToCsv(FullTestSuiteResult result)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("STS 531-1-07 Edition 2.2 Compliance Test Report");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Device: {config.DeviceModel} ({config.FirmwareVersion})");
        sb.AppendLine($"Utility Type: {result.UtilityType}");
        sb.AppendLine($"Duration: {(result.EndTime - result.StartTime).TotalSeconds:F1}s");
        sb.AppendLine($"Total: {result.TotalPassed}/{result.TotalSteps} PASSED");
        sb.AppendLine();

        // Column headers
        sb.AppendLine("TestID,Step,Description,Expected,Actual,Result,Error,Timestamp");

        // Data rows
        foreach (var test in result.Tests)
        {
            foreach (var step in test.Steps)
            {
                string resultStr = step.Passed ? "PASS" : 
                    (step.ErrorInfo?.StartsWith("MANUAL") == true ? "SKIP" : "FAIL");

                sb.AppendLine($"\"{test.TestId}\",{step.Step},\"{EscapeCsv(step.Description)}\",\"{EscapeCsv(step.Expected)}\",\"{EscapeCsv(step.Actual)}\",{resultStr},\"{EscapeCsv(step.ErrorInfo ?? "")}\",\"{step.Timestamp:yyyy-MM-dd HH:mm:ss}\"");
            }
        }

        return sb.ToString();
    }

    // =========================================================================
    //  Export Single Test to CSV
    // =========================================================================
    public string ExportTestToCsv(TestRunResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Test: {result.TestId} - {result.TestName}");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Result: {result.PassedCount}/{result.Steps.Count} PASSED");
        sb.AppendLine();

        sb.AppendLine("Step,Description,Expected,Actual,Result,Error");

        foreach (var step in result.Steps)
        {
            string resultStr = step.Passed ? "PASS" : "FAIL";
            sb.AppendLine($"{step.Step},\"{EscapeCsv(step.Description)}\",\"{EscapeCsv(step.Expected)}\",\"{EscapeCsv(step.Actual)}\",{resultStr},\"{EscapeCsv(step.ErrorInfo ?? "")}\"");
        }

        return sb.ToString();
    }

    // =========================================================================
    //  Generate HTML Report
    // =========================================================================
    public string GenerateHtmlReport(FullTestSuiteResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>STS Compliance Report - {DateTime.Now:yyyy-MM-dd}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(@"
            body { font-family: 'Segoe UI', Arial, sans-serif; margin: 20px; background: #1a1a2e; color: #eee; }
            h1 { color: #00d4aa; border-bottom: 2px solid #00d4aa; padding-bottom: 10px; }
            h2 { color: #3b82f6; margin-top: 30px; }
            table { border-collapse: collapse; width: 100%; margin: 15px 0; background: #16213e; }
            th, td { border: 1px solid #0f3460; padding: 10px; text-align: left; }
            th { background: #0f3460; color: #00d4aa; }
            .pass { color: #10b981; font-weight: bold; }
            .fail { color: #ef4444; font-weight: bold; }
            .skip { color: #f59e0b; }
            .token { font-family: 'Consolas', monospace; letter-spacing: 1px; color: #00d4aa; }
            .summary { background: #0f3460; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
            .summary-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 15px; }
            .stat { text-align: center; }
            .stat-value { font-size: 28px; font-weight: bold; }
            .stat-label { font-size: 12px; color: #94a3b8; }
            @media print { body { background: white; color: black; } table { background: white; } }
        ");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        sb.AppendLine($"<h1>STS 531-1-07 Ed {config.ComplianceTest.Edition} Compliance Report</h1>");

        // Summary
        sb.AppendLine("<div class=\"summary\">");
        sb.AppendLine("<div class=\"summary-grid\">");
        sb.AppendLine($"<div class=\"stat\"><div class=\"stat-value\" style=\"color:#00d4aa\">{result.TotalPassed}/{result.TotalSteps}</div><div class=\"stat-label\">STEPS PASSED</div></div>");
        sb.AppendLine($"<div class=\"stat\"><div class=\"stat-value\" style=\"color:#3b82f6\">{result.Tests.Count}</div><div class=\"stat-label\">TESTS RUN</div></div>");
        sb.AppendLine($"<div class=\"stat\"><div class=\"stat-value\" style=\"color:#8b5cf6\">{(result.EndTime - result.StartTime).TotalSeconds:F1}s</div><div class=\"stat-label\">DURATION</div></div>");
        sb.AppendLine($"<div class=\"stat\"><div class=\"stat-value\" style=\"color:{(result.AllPassed ? "#10b981" : "#ef4444")}\">{(result.AllPassed ? "PASS" : "FAIL")}</div><div class=\"stat-label\">RESULT</div></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("</div>");

        // Info table
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><th>Device</th><td>{config.DeviceModel} ({config.FirmwareVersion})</td></tr>");
        sb.AppendLine($"<tr><th>Specification</th><td>{config.ComplianceTest.Specification} Edition {config.ComplianceTest.Edition}</td></tr>");
        sb.AppendLine($"<tr><th>Utility Type</th><td>{GetUtilityName(result.UtilityType)}</td></tr>");
        sb.AppendLine($"<tr><th>Test Date</th><td>{result.StartTime:yyyy-MM-dd HH:mm:ss} UTC</td></tr>");
        sb.AppendLine($"<tr><th>DKGA / EA</th><td>{config.TokenGeneration.DKGA:D2} / {config.TokenGeneration.EA:D2}</td></tr>");
        sb.AppendLine($"<tr><th>RND (Fixed)</th><td>{config.TokenGeneration.RND}</td></tr>");
        sb.AppendLine("</table>");

        // Test results
        foreach (var test in result.Tests)
        {
            string statusClass = test.AllPassed ? "pass" : "fail";
            sb.AppendLine($"<h2>{test.TestId} — {test.TestName} <span class=\"{statusClass}\">[{test.PassedCount}/{test.Steps.Count}]</span></h2>");

            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Step</th><th>Description</th><th>Expected Token</th><th>Generated Token</th><th>Result</th></tr>");

            foreach (var step in test.Steps)
            {
                string resultClass = step.Passed ? "pass" : "fail";
                string resultText = step.Passed ? "PASS" : (step.ErrorInfo?.StartsWith("MANUAL") == true ? "SKIP" : "FAIL");

                sb.AppendLine("<tr>");
                sb.AppendLine($"<td>{step.Step}</td>");
                sb.AppendLine($"<td>{step.Description}</td>");
                sb.AppendLine($"<td class=\"token\">{step.Expected}</td>");
                sb.AppendLine($"<td class=\"token\">{step.Actual}</td>");
                sb.AppendLine($"<td class=\"{resultClass}\">{resultText}</td>");
                sb.AppendLine("</tr>");
            }

            sb.AppendLine("</table>");
        }

        // Footer
        sb.AppendLine($"<p style=\"margin-top:30px;color:#64748b;font-size:12px;\">Generated by STS Compliance POS v3.2.1 • {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    // =========================================================================
    //  Generate Summary Text Report
    // =========================================================================
    public string GenerateTextReport(FullTestSuiteResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔═══════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║  STS 531-1-07 Ed {config.ComplianceTest.Edition} COMPLIANCE REPORT                   ║");
        sb.AppendLine("╚═══════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine($"  Device    : {config.DeviceModel} ({config.FirmwareVersion})");
        sb.AppendLine($"  Utility   : {GetUtilityName(result.UtilityType)}");
        sb.AppendLine($"  Test Date : {result.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"  Duration  : {(result.EndTime - result.StartTime).TotalSeconds:F1} seconds");
        sb.AppendLine();
        sb.AppendLine($"  PASSED : {result.TotalPassed}");
        sb.AppendLine($"  FAILED : {result.TotalFailed}");
        sb.AppendLine($"  TOTAL  : {result.TotalSteps}");
        sb.AppendLine();

        foreach (var test in result.Tests)
        {
            string status = test.AllPassed ? "✓" : "✗";
            sb.AppendLine($"  {status} {test.TestId} — {test.TestName}: {test.PassedCount}/{test.Steps.Count}");
        }

        sb.AppendLine();
        if (result.AllPassed)
        {
            sb.AppendLine("  ═══ ALL TESTS PASSED ═══");
        }
        else
        {
            sb.AppendLine("  ─── FAILURES ───");
            foreach (var test in result.Tests)
            {
                foreach (var step in test.Steps.Where(s => !s.Passed))
                {
                    sb.AppendLine($"  {test.TestId} Step {step.Step}: {step.Description}");
                    sb.AppendLine($"    Expected: {step.Expected}");
                    sb.AppendLine($"    Actual  : {step.Actual}");
                    if (!string.IsNullOrEmpty(step.ErrorInfo))
                        sb.AppendLine($"    Error   : {step.ErrorInfo}");
                }
            }
        }

        return sb.ToString();
    }

    // =========================================================================
    //  Save Report to File
    // =========================================================================
    public async Task<string> SaveReportAsync(FullTestSuiteResult result, string format = "csv")
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"STS531_Compliance_{result.UtilityType}_{timestamp}.{format}";
        string folder = Path.Combine(env.WebRootPath, "reports");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string filepath = Path.Combine(folder, filename);
        string content;

        if (format.ToLower() == "html")
            content = GenerateHtmlReport(result);
        else if (format.ToLower() == "txt")
            content = GenerateTextReport(result);
        else
            content = ExportToCsv(result);

        await File.WriteAllTextAsync(filepath, content);
        return $"/reports/{filename}";
    }

    // =========================================================================
    //  Helpers
    // =========================================================================
    private static string EscapeCsv(string value)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (value == null) return "";
        return value.Replace("\"", "\"\"");
    }

    private static string GetUtilityName(string code)
    {
        return code switch
        {
            "E" => "Electricity (kWh)",
            "W" => "Water (kL)",
            "G" => "Gas (m³)",
            "T" => "Time (min)",
            _ => code
        };
    }
}
