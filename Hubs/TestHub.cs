// =============================================================================
//  TestHub.cs — SignalR Hub for real-time compliance test updates
//  Supports both EA07 (STS 531-1-07) and EA11 (STS 531-1-11)
// =============================================================================

using Microsoft.AspNetCore.SignalR;
using STSCompliancePOS.Services;

namespace STSCompliancePOS.Hubs;

public class TestHub(
    VSMConnectionService vsm,
    ComplianceTestService testsEA07,
    ComplianceTestServiceEA11 testsEA11,
    TestResultsStore resultsStore)
    : Hub
{
    public async Task SendProgress(string message)
    {
        await Clients.All.SendAsync("ReceiveProgress", message);
    }

    public async Task SendStepResult(TestStepResult result)
    {
        await Clients.All.SendAsync("ReceiveStepResult", result);
    }

    public async Task GetConnectionStatus()
    {
        var status = new ConnectionStatus
        {
            IsConnected = vsm.IsConnected,
            ConnectedPort = vsm.ConnectedPort,
            AvailablePorts = VSMConnectionService.GetAvailablePorts(),
            LastError = vsm.LastError
        };
        await Clients.Caller.SendAsync("ReceiveConnectionStatus", status);
    }

    public async Task Connect(string portName)
    {
        await Clients.Caller.SendAsync("ReceiveProgress", $"Connecting to {portName}...");
        bool success = vsm.Connect(portName);

        var status = new ConnectionStatus
        {
            IsConnected = vsm.IsConnected,
            ConnectedPort = vsm.ConnectedPort,
            AvailablePorts = VSMConnectionService.GetAvailablePorts(),
            LastError = vsm.LastError
        };

        await Clients.All.SendAsync("ReceiveConnectionStatus", status);
        await Clients.Caller.SendAsync("ReceiveProgress",
            success ? $"Connected to {portName}" : $"Connection failed: {vsm.LastError}");
    }

    public async Task Disconnect()
    {
        vsm.Disconnect();
        var status = new ConnectionStatus
        {
            IsConnected = false,
            ConnectedPort = null,
            AvailablePorts = VSMConnectionService.GetAvailablePorts(),
            LastError = null
        };
        await Clients.All.SendAsync("ReceiveConnectionStatus", status);
        await Clients.Caller.SendAsync("ReceiveProgress", "Disconnected");
    }

    // Run full test suite — EA parameter selects EA07 or EA11
    public async Task RunFullSuite(string utilityType, bool includeCurrency,
        bool includeKeychange, bool includeExtended, int ea = 7)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        string eaLabel = ea == 11 ? "EA11" : "EA07";
        await Clients.Caller.SendAsync("ReceiveProgress", $"Starting {eaLabel} compliance test suite...");

        FullTestSuiteResult result;

        if (ea == 11)
        {
            if (vsm.Driver != null) vsm.Driver.EA = 11;
            result = await testsEA11.RunFullSuite(utilityType, includeCurrency, includeKeychange, includeExtended,
                async msg => await Clients.Caller.SendAsync("ReceiveProgress", $"[EA11] {msg}"));
        }
        else
        {
            if (vsm.Driver != null) vsm.Driver.EA = 7;
            result = await testsEA07.RunFullSuite(utilityType, includeCurrency, includeKeychange, includeExtended,
                async msg => await Clients.Caller.SendAsync("ReceiveProgress", $"[EA07] {msg}"));
        }

        resultsStore.StoreSuiteResult(result);
        await Clients.Caller.SendAsync("ReceiveTestComplete", result);
    }

    // Run individual test
    public async Task RunTest(string testId, string utilityType, int ea = 7)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        string eaLabel = ea == 11 ? "EA11" : "EA07";
        await Clients.Caller.SendAsync("ReceiveProgress", $"Running {testId} ({eaLabel})...");

        TestRunResult? result;

        if (ea == 11)
        {
            if (vsm.Driver != null) vsm.Driver.EA = 11;
            result = testId.ToUpper() switch
            {
                "CTSA01" => await testsEA11.RunCTSA01(utilityType),
                "CTSA02" => await testsEA11.RunCTSA02(),
                "CTSA03" => await testsEA11.RunCTSA03(),
                "CTSA04" => await testsEA11.RunCTSA04(),
                "CTSA05" => await testsEA11.RunCTSA05(),
                "CTSA06" => await testsEA11.RunCTSA06(),
                "CTSA07" => await testsEA11.RunCTSA07(),
                "CTSA10" => await testsEA11.RunCTSA10(utilityType),
                "CTSA11" => await testsEA11.RunCTSA11(),
                "CTSA12" => await testsEA11.RunCTSA12(),
                "CTSA13" => await testsEA11.RunCTSA13(),
                "CTSA14" => await testsEA11.RunCTSA14(utilityType, true),
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
            if (vsm.Driver != null) vsm.Driver.EA = 7;
            result = testId.ToUpper() switch
            {
                "CTSA01" => await testsEA07.RunCTSA01(utilityType),
                "CTSA02" => await testsEA07.RunCTSA02(),
                "CTSA03" => await testsEA07.RunCTSA03(),
                "CTSA04" => await testsEA07.RunCTSA04(),
                "CTSA05" => await testsEA07.RunCTSA05(),
                "CTSA06" => await testsEA07.RunCTSA06(),
                "CTSA07" => await testsEA07.RunCTSA07(),
                "CTSA09" => await testsEA07.RunCTSA09(utilityType),
                "CTSA10" => await testsEA07.RunCTSA10(utilityType),
                "CTSA12" => await testsEA07.RunCTSA12(),
                "CTSA13" => await testsEA07.RunCTSA13(),
                "CTSA14" => await testsEA07.RunCTSA14(utilityType, true),
                "CTSA15" => await testsEA07.RunCTSA15(),
                "CTSA16" => await testsEA07.RunCTSA16(),
                "CTSA17" => await testsEA07.RunCTSA17(),
                "CTSA20" => await testsEA07.RunCTSA20(),
                "CTSA24" => await testsEA07.RunCTSA24(),
                _ => null
            };
        }

        if (result != null)
        {
            resultsStore.StoreTestResult(result);
            await Clients.Caller.SendAsync("ReceiveSingleTestComplete", result);
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveProgress", $"Unknown test: {testId}");
        }
    }

    // Generate single token
    public async Task GenerateToken(string pan, string reg, string ti, string creditType,
        decimal amount, string issueDateStr, int baseDate, int ea = 7)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        DateTime issueDate = DateTime.Parse(issueDateStr);

        (string? token, string? error) result;
        if (ea == 11)
            result = await testsEA11.GenerateSingleToken(pan, reg, ti, creditType, amount, issueDate, baseDate);
        else
            result = await testsEA07.GenerateSingleToken(pan, reg, ti, creditType, amount, issueDate, baseDate);

        if (result.token != null)
            await Clients.Caller.SendAsync("ReceiveToken", new { Token = result.token, Error = (string?)null, EA = ea });
        else
            await Clients.Caller.SendAsync("ReceiveToken", new { Token = (string?)null, Error = result.error, EA = ea });
    }
}
