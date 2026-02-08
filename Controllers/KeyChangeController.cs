using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Models;

namespace STSCompliancePOS.Controllers;

public class KeyChangeController : Controller
{
    public IActionResult Index()
    {
        var request = new KeyChangeRequest();
        return View(request);
    }

    [HttpPost]
    public IActionResult Generate(KeyChangeRequest request)
    {
        // Mockup: return CTSA05 Step 2 result
        var result = new KeyChangeResult
        {
            FirstKCT = "6541 3070 5436 8815 6280",
            SecondKCT = "2746 9374 5674 7695 3413",
            ExpectedFirstKCT = "6541 3070 5436 8815 6280",
            ExpectedSecondKCT = "2746 9374 5674 7695 3413",
            Request = request
        };
        return View("Result", result);
    }
}
