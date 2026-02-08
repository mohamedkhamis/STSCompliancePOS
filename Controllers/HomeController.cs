using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Models;
using STSCompliancePOS.Services;

namespace STSCompliancePOS.Controllers;

public class HomeController(STSTestDataService testData, VSMConnectionService vsm, VSMConfiguration config)
    : Controller
{
    public IActionResult Index()
    {
        var tests = testData.GetAllTests();
        var defaultReg = config.GetDefaultRegister();
        
        var model = new DashboardViewModel
        {
            TotalTests = tests.Count,
            PassedTests = tests.Count(t => t.OverallStatus == TestStatus.Pass),
            TotalSteps = tests.Sum(t => t.TotalSteps),
            VendingTests = tests.Count(t => t.Classification.Contains("V")),
            EngineeringTests = tests.Count(t => t.Classification.Contains("E")),
            KeychangeTests = tests.Count(t => t.Classification.Contains("K")),
            LastTestRun = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC",
            ActiveConfig = new APDUConfig
            {
                MeterPAN = config.TestPANs.PAN_11,
                SGC = defaultReg?.SGC ?? "201457",
                DKGA = config.TokenGeneration.DKGA,
                EA = config.TokenGeneration.EA,
                BaseDate = defaultReg?.BaseDate ?? 2014,
                KRN = defaultReg?.KRN ?? 1,
                TI = 1,
                KT = 1,
                KeyExpiryNumber = defaultReg?.KEN ?? 255
            },
            IsVSMConnected = vsm.IsConnected,
            ConnectedPort = vsm.ConnectedPort,
            DeviceModel = config.DeviceModel,
            FirmwareVersion = config.FirmwareVersion
        };
        return View(model);
    }
}
