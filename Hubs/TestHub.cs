// =============================================================================
//  TestHub.cs — SignalR Hub for real-time compliance test updates
// =============================================================================

using Microsoft.AspNetCore.SignalR;
using STSCompliancePOS.Services;

namespace STSCompliancePOS.Hubs;

public class TestHub(VSMConnectionService vsm, ComplianceTestService tests, TestResultsStore resultsStore)
    : Hub
{
    // Send progress update to all clients
    // ReSharper disable  UnusedMember.Global
    public async Task SendProgress(string message)
    {
        await Clients.All.SendAsync("ReceiveProgress", message);
    }

    // Send step result to all clients
    public async Task SendStepResult(TestStepResult result)
    {
        await Clients.All.SendAsync("ReceiveStepResult", result);
    }

    // Get connection status
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

    // Connect to COM port
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

        if (success)
            await Clients.Caller.SendAsync("ReceiveProgress", $"Connected to {portName}");
        else
            await Clients.Caller.SendAsync("ReceiveProgress", $"Connection failed: {vsm.LastError}");
    }

    // Disconnect
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

    // Run full test suite
    public async Task RunFullSuite(string utilityType, bool includeCurrency, bool includeKeychange, bool includeExtended)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        await Clients.Caller.SendAsync("ReceiveProgress", "Starting full compliance test suite...");

        var result = await tests.RunFullSuite(utilityType, includeCurrency, includeKeychange, includeExtended,
            // ReSharper disable once AsyncVoidLambda
            async msg => await Clients.Caller.SendAsync("ReceiveProgress", msg));

        // Store results for export
        resultsStore.StoreSuiteResult(result);

        await Clients.Caller.SendAsync("ReceiveTestComplete", result);
    }

    // Run individual test
    // ReSharper disable once UnusedMember.Global
    public async Task RunTest(string testId, string utilityType)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        await Clients.Caller.SendAsync("ReceiveProgress", $"Running {testId}...");

        TestRunResult? result = testId.ToUpper() switch
        {
            "CTSA01" => await tests.RunCTSA01(utilityType),
            "CTSA02" => await tests.RunCTSA02(),
            "CTSA03" => await tests.RunCTSA03(),
            "CTSA04" => await tests.RunCTSA04(),
            "CTSA05" => await tests.RunCTSA05(),
            "CTSA06" => await tests.RunCTSA06(),
            "CTSA07" => await tests.RunCTSA07(),
            "CTSA09" => await tests.RunCTSA09(utilityType),
            "CTSA10" => await tests.RunCTSA10(utilityType),
            "CTSA12" => await tests.RunCTSA12(),
            "CTSA13" => await tests.RunCTSA13(),
            "CTSA14" => await tests.RunCTSA14(utilityType, true),
            "CTSA15" => await tests.RunCTSA15(),
            "CTSA16" => await tests.RunCTSA16(),
            "CTSA17" => await tests.RunCTSA17(),
            "CTSA20" => await tests.RunCTSA20(),
            "CTSA24" => await tests.RunCTSA24(),
            _ => null
        };

        if (result != null)
        {
            // Store results for export
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
        decimal amount, string issueDateStr, int baseDate)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        DateTime issueDate = DateTime.Parse(issueDateStr);
        var (token, error) = await tests.GenerateSingleToken(pan, reg, ti, creditType, amount, issueDate, baseDate);

        if (token != null)
        {
            await Clients.Caller.SendAsync("ReceiveToken", new { Token = token, Error = (string?)null });
        }
        else
        {
            await Clients.Caller.SendAsync("ReceiveToken", new { Token = (string?)null, Error = error });
        }
    }
}
