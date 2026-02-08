using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Services;
using System.Text;

namespace STSCompliancePOS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VsmController(
    VSMConnectionService vsm,
    ComplianceTestService testsEA07,
    ComplianceTestServiceEA11 testsEA11,
    ReportService report,
    VSMConfiguration config,
    TestResultsStore resultsStore)
    : ControllerBase
{
    // GET: api/vsm/status
    [HttpGet("status")]
    public ActionResult<ConnectionStatus> GetStatus()
    {
        return new ConnectionStatus
        {
            IsConnected = vsm.IsConnected,
            ConnectedPort = vsm.ConnectedPort,
            AvailablePorts = VSMConnectionService.GetAvailablePorts(),
            LastError = vsm.LastError
        };
    }

    // GET: api/vsm/ports
    [HttpGet("ports")]
    public ActionResult<string[]> GetPorts()
    {
        return VSMConnectionService.GetAvailablePorts();
    }

    // GET: api/vsm/config
    [HttpGet("config")]
    public ActionResult<VSMConfiguration> GetConfig()
    {
        return config;
    }

    // GET: api/vsm/registers
    [HttpGet("registers")]
    public ActionResult<Dictionary<string, KeyRegisterConfig>> GetRegisters()
    {
        return config.KeyRegisters;
    }

    // POST: api/vsm/connect
    [HttpPost("connect")]
    public ActionResult<ConnectionStatus> Connect([FromBody] ConnectRequest request)
    {
        bool success = vsm.Connect(request.PortName);
        return new ConnectionStatus
        {
            IsConnected = vsm.IsConnected,
            ConnectedPort = vsm.ConnectedPort,
            AvailablePorts = VSMConnectionService.GetAvailablePorts(),
            LastError = vsm.LastError
        };
    }

    // POST: api/vsm/disconnect
    [HttpPost("disconnect")]
    public ActionResult<ConnectionStatus> Disconnect()
    {
        vsm.Disconnect();
        return new ConnectionStatus
        {
            IsConnected = false,
            ConnectedPort = null,
            AvailablePorts = VSMConnectionService.GetAvailablePorts()
        };
    }

    // POST: api/vsm/generate-token
    [HttpPost("generate-token")]
    public async Task<ActionResult> GenerateToken([FromBody] TokenRequest request)
    {
        if (!vsm.IsConnected)
            return BadRequest(new { Error = "VSM not connected" });

        // Route to EA07 or EA11 based on request
        (string? token, string? error) result;
        if (request.EA == 11)
            result = await testsEA11.GenerateSingleToken(
                request.MeterPAN, request.Register, request.TI,
                request.CreditType, request.Amount, request.IssueDate, request.BaseDate);
        else
            result = await testsEA07.GenerateSingleToken(
                request.MeterPAN, request.Register, request.TI,
                request.CreditType, request.Amount, request.IssueDate, request.BaseDate);

        if (result.token != null)
            return Ok(new { Token = result.token, EA = request.EA });
        else
            return BadRequest(new { Error = result.error });
    }

    // POST: api/vsm/run-test
    [HttpPost("run-test")]
    public async Task<ActionResult<TestRunResult>> RunTest([FromBody] RunTestRequest request)
    {
        if (!vsm.IsConnected)
            return BadRequest(new { Error = "VSM not connected" });

        TestRunResult? result;

        if (request.EA == 11)
        {
            // Set driver to EA11
            if (vsm.Driver != null) vsm.Driver.EA = 11;

            result = request.TestId.ToUpper() switch
            {
                "CTSA01" => await testsEA11.RunCTSA01(request.UtilityType),
                "CTSA02" => await testsEA11.RunCTSA02(),
                "CTSA03" => await testsEA11.RunCTSA03(),
                "CTSA04" => await testsEA11.RunCTSA04(),
                "CTSA05" => await testsEA11.RunCTSA05(),
                "CTSA06" => await testsEA11.RunCTSA06(),
                "CTSA07" => await testsEA11.RunCTSA07(),
                "CTSA10" => await testsEA11.RunCTSA10(request.UtilityType),
                "CTSA11" => await testsEA11.RunCTSA11(),
                "CTSA12" => await testsEA11.RunCTSA12(),
                "CTSA13" => await testsEA11.RunCTSA13(),
                "CTSA14" => await testsEA11.RunCTSA14(request.UtilityType, true),
                "CTSA15" => await testsEA11.RunCTSA15(),
                "CTSA16" => await testsEA11.RunCTSA16(),
                "CTSA17" => await testsEA11.RunCTSA17(),
                "CTSA20" => await testsEA11.RunCTSA20(),
                "CTSA24" => await testsEA11.RunCTSA24(),
                _ => null
            };
        }
        else
        {
            // EA07 — original behavior
            if (vsm.Driver != null) vsm.Driver.EA = 7;

            result = request.TestId.ToUpper() switch
            {
                "CTSA01" => await testsEA07.RunCTSA01(request.UtilityType),
                "CTSA02" => await testsEA07.RunCTSA02(),
                "CTSA03" => await testsEA07.RunCTSA03(),
                "CTSA04" => await testsEA07.RunCTSA04(),
                "CTSA05" => await testsEA07.RunCTSA05(),
                "CTSA06" => await testsEA07.RunCTSA06(),
                "CTSA07" => await testsEA07.RunCTSA07(),
                "CTSA09" => await testsEA07.RunCTSA09(request.UtilityType),
                "CTSA10" => await testsEA07.RunCTSA10(request.UtilityType),
                "CTSA12" => await testsEA07.RunCTSA12(),
                "CTSA13" => await testsEA07.RunCTSA13(),
                "CTSA14" => await testsEA07.RunCTSA14(request.UtilityType, true),
                "CTSA15" => await testsEA07.RunCTSA15(),
                "CTSA16" => await testsEA07.RunCTSA16(),
                "CTSA17" => await testsEA07.RunCTSA17(),
                "CTSA20" => await testsEA07.RunCTSA20(),
                "CTSA24" => await testsEA07.RunCTSA24(),
                _ => null
            };
        }

        if (result == null)
            return BadRequest(new { Error = $"Unknown test: {request.TestId}" });

        resultsStore.StoreTestResult(result);
        return result;
    }

    // POST: api/vsm/run-suite
    [HttpPost("run-suite")]
    public async Task<ActionResult<FullTestSuiteResult>> RunSuite([FromBody] RunSuiteRequest request)
    {
        if (!vsm.IsConnected)
            return BadRequest(new { Error = "VSM not connected" });

        FullTestSuiteResult result;

        if (request.EA == 11)
        {
            if (vsm.Driver != null) vsm.Driver.EA = 11;
            result = await testsEA11.RunFullSuite(
                request.UtilityType, request.IncludeCurrency,
                request.IncludeKeychange, request.IncludeExtended);
        }
        else
        {
            if (vsm.Driver != null) vsm.Driver.EA = 7;
            result = await testsEA07.RunFullSuite(
                request.UtilityType, request.IncludeCurrency,
                request.IncludeKeychange, request.IncludeExtended);
        }

        resultsStore.StoreSuiteResult(result);
        return result;
    }

    // POST: api/vsm/run-full-pos — Run ALL 160 tests (both EA07 + EA11)
    [HttpPost("run-full-pos")]
    public async Task<ActionResult<POSFullTestResult>> RunFullPOS([FromBody] RunSuiteRequest request)
    {
        if (!vsm.IsConnected)
            return BadRequest(new { Error = "VSM not connected" });

        var posResult = new POSFullTestResult { StartTime = DateTime.UtcNow };

        // Run EA07 suite (tests 0001-0080)
        if (vsm.Driver != null) vsm.Driver.EA = 7;
        posResult.EA07Result = await testsEA07.RunFullSuite(
            request.UtilityType, request.IncludeCurrency,
            request.IncludeKeychange, request.IncludeExtended);

        // Run EA11 suite (tests 0081-0160)
        if (vsm.Driver != null) vsm.Driver.EA = 11;
        posResult.EA11Result = await testsEA11.RunFullSuite(
            request.UtilityType, request.IncludeCurrency,
            request.IncludeKeychange, request.IncludeExtended);

        posResult.EndTime = DateTime.UtcNow;

        // Store both results
        resultsStore.StoreSuiteResult(posResult.EA07Result);

        return posResult;
    }

    // =========================================================================
    //  Export Endpoints
    // =========================================================================

    [HttpGet("export/csv")]
    public ActionResult ExportCsv()
    {
        if (!resultsStore.HasSuiteResults)
            return BadRequest(new { Error = "No test results available. Run tests first." });

        var csv = report.ExportToCsv(resultsStore.LastSuiteResult!);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var filename = $"STS531_Compliance_{resultsStore.LastSuiteResult!.UtilityType}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", filename);
    }

    [HttpGet("export/html")]
    public ActionResult ExportHtml()
    {
        if (!resultsStore.HasSuiteResults)
            return BadRequest(new { Error = "No test results available. Run tests first." });

        var html = report.GenerateHtmlReport(resultsStore.LastSuiteResult!);
        var bytes = Encoding.UTF8.GetBytes(html);
        var filename = $"STS531_Compliance_{resultsStore.LastSuiteResult!.UtilityType}_{DateTime.Now:yyyyMMdd_HHmmss}.html";

        return File(bytes, "text/html", filename);
    }

    [HttpGet("export/txt")]
    public ActionResult ExportTxt()
    {
        if (!resultsStore.HasSuiteResults)
            return BadRequest(new { Error = "No test results available. Run tests first." });

        var txt = report.GenerateTextReport(resultsStore.LastSuiteResult!);
        var bytes = Encoding.UTF8.GetBytes(txt);
        var filename = $"STS531_Compliance_{resultsStore.LastSuiteResult!.UtilityType}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

        return File(bytes, "text/plain", filename);
    }

    [HttpPost("export/save")]
    public async Task<ActionResult> SaveReport([FromBody] SaveReportRequest request)
    {
        if (!resultsStore.HasSuiteResults)
            return BadRequest(new { Error = "No test results available. Run tests first." });

        var path = await report.SaveReportAsync(resultsStore.LastSuiteResult!, request.Format);
        return Ok(new { Path = path });
    }

    [HttpGet("export/test-csv/{testId}")]
    public ActionResult ExportTestCsv(string testId)
    {
        if (!resultsStore.HasTestResults || resultsStore.LastTestResult!.TestId != testId.ToUpper())
            return BadRequest(new { Error = $"No results for {testId}. Run the test first." });

        var csv = report.ExportTestToCsv(resultsStore.LastTestResult!);
        var bytes = Encoding.UTF8.GetBytes(csv);
        var filename = $"{testId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        return File(bytes, "text/csv", filename);
    }

    [HttpGet("report/print")]
    public ActionResult GetPrintReport()
    {
        if (!resultsStore.HasSuiteResults)
            return BadRequest(new { Error = "No test results available. Run tests first." });

        return Content(report.GenerateHtmlReport(resultsStore.LastSuiteResult!), "text/html");
    }

    [HttpGet("results/status")]
    public ActionResult GetResultsStatus()
    {
        return Ok(new
        {
            resultsStore.HasSuiteResults,
            resultsStore.HasTestResults,
            resultsStore.LastRunTime,
            SuiteUtilityType = resultsStore.LastSuiteResult?.UtilityType,
            SuiteTotalPassed = resultsStore.LastSuiteResult?.TotalPassed,
            SuiteTotalSteps = resultsStore.LastSuiteResult?.TotalSteps,
            LastTestId = resultsStore.LastTestResult?.TestId
        });
    }
}

// ── Request/Response Models ────────────────────────────────────────────────

public class ConnectRequest
{
    public string PortName { get; set; } = "";
}

public class TokenRequest
{
    public string MeterPAN { get; set; } = "600727000000000009";
    public string Register { get; set; } = "01";
    public string TI { get; set; } = "01";
    public string CreditType { get; set; } = "0";
    public decimal Amount { get; set; } = 0.1m;
    public DateTime IssueDate { get; set; } = new(2014, 3, 1, 13, 0, 0);
    public int BaseDate { get; set; } = 2014;
    public int EA { get; set; } = 7; // 7 or 11
}

public class RunTestRequest
{
    public string TestId { get; set; } = "";
    public string UtilityType { get; set; } = "E";
    public int EA { get; set; } = 7; // 7 or 11
}

public class RunSuiteRequest
{
    public string UtilityType { get; set; } = "E";
    public bool IncludeCurrency { get; set; } = false;
    public bool IncludeKeychange { get; set; } = false;
    public bool IncludeExtended { get; set; } = false;
    public int EA { get; set; } = 7; // 7 or 11
}

public class SaveReportRequest
{
    public string Format { get; set; } = "csv";
}

// Combined POS result for all 160 tests
public class POSFullTestResult
{
    public FullTestSuiteResult EA07Result { get; set; } = new();
    public FullTestSuiteResult EA11Result { get; set; } = new();
    public int TotalTests => (EA07Result?.TotalSteps ?? 0) + (EA11Result?.TotalSteps ?? 0);
    public int TotalPassed => (EA07Result?.TotalPassed ?? 0) + (EA11Result?.TotalPassed ?? 0);
    public int TotalFailed => (EA07Result?.TotalFailed ?? 0) + (EA11Result?.TotalFailed ?? 0);
    public bool AllPassed => (EA07Result?.AllPassed ?? false) && (EA11Result?.AllPassed ?? false);
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}
