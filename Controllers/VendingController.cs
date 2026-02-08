using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Models;

namespace STSCompliancePOS.Controllers;

public class VendingController : Controller
{
    public IActionResult Index()
    {
        var request = new VendRequest();
        return View(request);
    }

    [HttpPost]
    public IActionResult Generate(VendRequest request)
    {
        // In production, this calls the actual VSM/token engine
        // For mockup, return the expected CTSA01 Step 1 result
        var result = new VendResult
        {
            GeneratedToken = "7063 0503 4700 2872 6114",
            ExpectedToken = "7063 0503 4700 2872 6114",
            TokenType = "TransferCredit (Electricity)",
            Request = request,
            Breakdown = new TokenBreakdown
            {
                TokenClass = "Class 0 (TransferCredit)",
                Subclass = "00 (Electricity)",
                TID = request.TokenIssueDate.ToString("yyyy-MM-dd HH:mm"),
                AmountEncoded = $"{request.Amount} {request.Unit}",
                RND = 5,
                CRCValid = true
            }
        };
        return View("Result", result);
    }
}
