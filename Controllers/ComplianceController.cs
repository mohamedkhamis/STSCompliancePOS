using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Models;

namespace STSCompliancePOS.Controllers;

public class ComplianceController(STSTestDataService testData) : Controller
{
    public IActionResult Index()
    {
        var tests = testData.GetAllTests();
        return View(tests);
    }

    public IActionResult Detail(string id)
    {
        var test = testData.GetTest(id);
        if (test == null) return NotFound();
        return View(test);
    }
}
