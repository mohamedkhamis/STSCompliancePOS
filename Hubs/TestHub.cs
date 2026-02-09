// =============================================================================
//  TestHub.cs — SignalR Hub for real-time compliance test updates
//  Supports both EA07 (STS 531-1-07) and EA11 (STS 531-1-11)
// =============================================================================

using Microsoft.AspNetCore.SignalR;
using STSCompliancePOS.Controllers;
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

    // Run all 160 POS tests (EA07 + EA11) with real-time progress
    public async Task RunFullPOS(string utilityType, bool includeCurrency,
        bool includeKeychange, bool includeExtended)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        await Clients.Caller.SendAsync("ReceiveProgress", "═══ Running ALL 160 POS Tests (EA07 + EA11) ═══");

        // Run EA07 suite (tests 0001-0080)
        await Clients.Caller.SendAsync("ReceiveProgress", "Starting EA07 suite (80 tests)...");
        if (vsm.Driver != null) vsm.Driver.EA = 7;
        var ea07Result = await testsEA07.RunFullSuite(utilityType, includeCurrency, includeKeychange, includeExtended,
            async msg => await Clients.Caller.SendAsync("ReceiveProgress", $"[EA07] {msg}"));

        await Clients.Caller.SendAsync("ReceiveProgress",
            $"═══ EA07 Done: {ea07Result.TotalPassed}/{ea07Result.TotalSteps} passed ═══");

        // Run EA11 suite (tests 0081-0160)
        await Clients.Caller.SendAsync("ReceiveProgress", "Starting EA11 suite (80 tests)...");
        if (vsm.Driver != null) vsm.Driver.EA = 11;
        var ea11Result = await testsEA11.RunFullSuite(utilityType, includeCurrency, includeKeychange, includeExtended,
            async msg => await Clients.Caller.SendAsync("ReceiveProgress", $"[EA11] {msg}"));

        await Clients.Caller.SendAsync("ReceiveProgress",
            $"═══ EA11 Done: {ea11Result.TotalPassed}/{ea11Result.TotalSteps} passed ═══");

        // Store results
        resultsStore.StoreSuiteResult(ea07Result);

        var posResult = new POSFullTestResult
        {
            EA07Result = ea07Result,
            EA11Result = ea11Result,
            StartTime = ea07Result.StartTime,
            EndTime = DateTime.UtcNow
        };

        await Clients.Caller.SendAsync("ReceiveFullPOSComplete", posResult);
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

        try
        {
            await Clients.Caller.SendAsync("ReceiveProgress", $"Generating {creditType} token (EA{(ea == 11 ? "11" : "07")})...");

            DateTime issueDate = DateTime.Parse(issueDateStr);

            (string? token, string? error) result;
            if (ea == 11)
                result = await testsEA11.GenerateSingleToken(pan, reg, ti, creditType, amount, issueDate, baseDate);
            else
                result = await testsEA07.GenerateSingleToken(pan, reg, ti, creditType, amount, issueDate, baseDate);

            if (result.token != null)
            {
                await Clients.Caller.SendAsync("ReceiveProgress", $"Token generated: {result.token}");
                await Clients.Caller.SendAsync("ReceiveToken", new { Token = result.token, Error = (string?)null, EA = ea });
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveProgress", $"Token error: {result.error}");
                await Clients.Caller.SendAsync("ReceiveToken", new { Token = (string?)null, Error = result.error, EA = ea });
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", $"Exception: {ex.Message}");
            await Clients.Caller.SendAsync("ReceiveToken", new { Token = (string?)null, Error = ex.Message, EA = ea });
        }
    }

    // Generate management token (ClearCredit, SetMaxPowerLimit, ClearTamper, SetMPUL)
    public async Task GenerateManagementToken(string pan, string reg, string ti, string mgmtType,
        ushort value, string issueDateStr, int baseDate, int ea = 7)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        try
        {
            await Clients.Caller.SendAsync("ReceiveProgress", $"Generating {mgmtType} management token (EA{(ea == 11 ? "11" : "07")})...");

            DateTime issueDate = DateTime.Parse(issueDateStr);

            (string? token, string? error) result;
            if (ea == 11)
                result = await testsEA11.GenerateSingleManagementToken(pan, reg, ti, mgmtType, value, issueDate, baseDate);
            else
                result = await testsEA07.GenerateSingleManagementToken(pan, reg, ti, mgmtType, value, issueDate, baseDate);

            if (result.token != null)
            {
                await Clients.Caller.SendAsync("ReceiveProgress", $"Token generated: {result.token}");
                await Clients.Caller.SendAsync("ReceiveToken", new { Token = result.token, Error = (string?)null, EA = ea });
            }
            else
            {
                await Clients.Caller.SendAsync("ReceiveProgress", $"Token error: {result.error}");
                await Clients.Caller.SendAsync("ReceiveToken", new { Token = (string?)null, Error = result.error, EA = ea });
            }
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", $"Exception: {ex.Message}");
            await Clients.Caller.SendAsync("ReceiveToken", new { Token = (string?)null, Error = ex.Message, EA = ea });
        }
    }

    // Generate keychange tokens (2 KCTs for EA07, 4 KCTs for EA11)
    public async Task GenerateKeychangeTokens(string pan, string oldReg, string newReg,
        string oldTi, string newTi, int ea = 7)
    {
        if (!vsm.IsConnected)
        {
            await Clients.Caller.SendAsync("ReceiveProgress", "Error: VSM not connected");
            return;
        }

        if (vsm.Driver == null)
        {
            await Clients.Caller.SendAsync("ReceiveKeychangeResult", new
            {
                FirstKCT = (string?)null, SecondKCT = (string?)null,
                ThirdKCT = (string?)null, FourthKCT = (string?)null,
                Error = "VSM driver not available"
            });
            return;
        }

        try
        {
            if (ea == 11)
            {
                vsm.Driver.EA = 11;
                var (t1, t2, t3, t4) = vsm.Driver.GenerateKeychangeQuad(pan, oldReg, newReg,
                    "", "", oldTi, newTi, '1', '1', 255, 255, '0');

                if (t1 != null && t2 != null && t3 != null && t4 != null)
                {
                    await Clients.Caller.SendAsync("ReceiveKeychangeResult", new
                    {
                        FirstKCT = StsHelper.FormatToken(t1),
                        SecondKCT = StsHelper.FormatToken(t2),
                        ThirdKCT = (string?)StsHelper.FormatToken(t3),
                        FourthKCT = (string?)StsHelper.FormatToken(t4),
                        Error = (string?)null
                    });
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveKeychangeResult", new
                    {
                        FirstKCT = (string?)null, SecondKCT = (string?)null,
                        ThirdKCT = (string?)null, FourthKCT = (string?)null,
                        Error = vsm.Driver.LastError ?? "Unknown error"
                    });
                }
            }
            else
            {
                vsm.Driver.EA = 7;
                var (t1, t2) = vsm.Driver.GenerateKeychangeTokens(pan, oldReg, newReg,
                    "", "", oldTi, newTi, '1', '1', 255, 255, '0');

                if (t1 != null && t2 != null)
                {
                    await Clients.Caller.SendAsync("ReceiveKeychangeResult", new
                    {
                        FirstKCT = StsHelper.FormatToken(t1),
                        SecondKCT = StsHelper.FormatToken(t2),
                        ThirdKCT = (string?)null,
                        FourthKCT = (string?)null,
                        Error = (string?)null
                    });
                }
                else
                {
                    await Clients.Caller.SendAsync("ReceiveKeychangeResult", new
                    {
                        FirstKCT = (string?)null, SecondKCT = (string?)null,
                        ThirdKCT = (string?)null, FourthKCT = (string?)null,
                        Error = vsm.Driver.LastError ?? "Unknown error"
                    });
                }
            }

            await Task.Delay(50);
        }
        catch (Exception ex)
        {
            await Clients.Caller.SendAsync("ReceiveKeychangeResult", new
            {
                FirstKCT = (string?)null, SecondKCT = (string?)null,
                ThirdKCT = (string?)null, FourthKCT = (string?)null,
                Error = ex.Message
            });
        }
    }
}
