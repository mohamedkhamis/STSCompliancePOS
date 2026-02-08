using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Services;

namespace STSCompliancePOS.Controllers;

public class ConnectionController(VSMConnectionService vsm) : Controller
{
    public IActionResult Index()
    {
        var status = new ConnectionStatus
        {
            IsConnected = vsm.IsConnected,
            ConnectedPort = vsm.ConnectedPort,
            AvailablePorts = VSMConnectionService.GetAvailablePorts(),
            LastError = vsm.LastError
        };
        return View(status);
    }
}
