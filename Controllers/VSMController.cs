using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Services;
using System.Text;

namespace STSCompliancePOS.Controllers;

[Route("api/[controller]")]
[ApiController]
public class VsmController(
    VSMConnectionService vsm,
    ComplianceTestService tests,
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
        // ReSharper disable once UnusedVariable
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

        var (token, error) = await tests.GenerateSingleToken(
            request.MeterPAN,
            request.Register,
            request.TI,
            request.CreditType,
            request.Amount,
            request.IssueDate,
            request.BaseDate
        );

        if (token != null)
            return Ok(new { Token = token });
        else
            return BadRequest(new { Error = error });
    }

    // POST: api/vsm/run-test
    [HttpPost("run-test")]
    public async Task<ActionResult<TestRunResult>> RunTest([FromBody] RunTestRequest request)
    {
        if (!vsm.IsConnected)
            return BadRequest(new { Error = "VSM not connected" });

        TestRunResult? result = request.TestId.ToUpper() switch
        {
            "CTSA01" => await tests.RunCTSA01(request.UtilityType),
            "CTSA02" => await tests.RunCTSA02(),
            "CTSA03" => await tests.RunCTSA03(),
            "CTSA04" => await tests.RunCTSA04(),
            "CTSA05" => await tests.RunCTSA05(),
            "CTSA06" => await tests.RunCTSA06(),
            "CTSA07" => await tests.RunCTSA07(),
            "CTSA09" => await tests.RunCTSA09(request.UtilityType),
            "CTSA10" => await tests.RunCTSA10(request.UtilityType),
            "CTSA12" => await tests.RunCTSA12(),
            "CTSA13" => await tests.RunCTSA13(),
            "CTSA14" => await tests.RunCTSA14(request.UtilityType, true),
            "CTSA15" => await tests.RunCTSA15(),
            "CTSA16" => await tests.RunCTSA16(),
            "CTSA17" => await tests.RunCTSA17(),
            "CTSA20" => await tests.RunCTSA20(),
            "CTSA24" => await tests.RunCTSA24(),
            _ => null
        };

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

        var result = await tests.RunFullSuite(
            request.UtilityType,
            request.IncludeCurrency,
            request.IncludeKeychange,
            request.IncludeExtended
        );

        resultsStore.StoreSuiteResult(result);
        return result;
    }

    // =========================================================================
    //  Export Endpoints
    // =========================================================================

    // GET: api/vsm/export/csv
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

    // GET: api/vsm/export/html
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

    // GET: api/vsm/export/txt
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

    // POST: api/vsm/export/save
    [HttpPost("export/save")]
    public async Task<ActionResult> SaveReport([FromBody] SaveReportRequest request)
    {
        if (!resultsStore.HasSuiteResults)
            return BadRequest(new { Error = "No test results available. Run tests first." });

        var path = await report.SaveReportAsync(resultsStore.LastSuiteResult!, request.Format);
        return Ok(new { Path = path });
    }

    // GET: api/vsm/export/test-csv/{testId}
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

    // GET: api/vsm/report/print
    [HttpGet("report/print")]
    public ActionResult GetPrintReport()
    {
        if (!resultsStore.HasSuiteResults)
            return BadRequest(new { Error = "No test results available. Run tests first." });

        return Content(report.GenerateHtmlReport(resultsStore.LastSuiteResult!), "text/html");
    }

    // GET: api/vsm/results/status
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

// Request models
public class ConnectRequest
{
    public string PortName { get; set; } = "";
}

public class TokenRequest
{
    // ReSharper disable once InconsistentNaming
    public string MeterPAN { get; set; } = "600727000000000009";
    public string Register { get; set; } = "01";
    // ReSharper disable once InconsistentNaming
    public string TI { get; set; } = "01";
    public string CreditType { get; set; } = "0";
    public decimal Amount { get; set; } = 0.1m;
    public DateTime IssueDate { get; set; } = new(2014, 3, 1, 13, 0, 0);
    public int BaseDate { get; set; } = 2014;
}

public class RunTestRequest
{
    public string TestId { get; set; } = "";
    public string UtilityType { get; set; } = "E";
}

public class RunSuiteRequest
{
    public string UtilityType { get; set; } = "E";
    public bool IncludeCurrency { get; set; } = false;
    public bool IncludeKeychange { get; set; } = false;
    public bool IncludeExtended { get; set; } = false;
}

public class SaveReportRequest
{
    public string Format { get; set; } = "csv";
}
