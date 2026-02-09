using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Services;

namespace STSCompliancePOS.Controllers;

public class KeyChangeController(VSMConnectionService vsm) : Controller
{
    public IActionResult Index()
    {
        ViewBag.IsConnected = vsm.IsConnected;
        ViewBag.ConnectedPort = vsm.ConnectedPort;
        return View();
    }
}
