using Microsoft.AspNetCore.Mvc;
using STSCompliancePOS.Models;

namespace STSCompliancePOS.Controllers;

public class MeterSetupController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public IActionResult ValidateDrn(string drn)
    {
        var result = new DRNValidationResult { DRN = drn };

        if (string.IsNullOrWhiteSpace(drn) || drn.Length < 11)
        {
            result.IsValid = false;
            result.Message = "DRN must be at least 11 digits.";
            return View("Index", result);
        }

        // Luhn check
        int sum = 0;
        bool alternate = false;
        for (int i = drn.Length - 1; i >= 0; i--)
        {
            int n = drn[i] - '0';
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }

        result.IsValid = sum % 10 == 0;
        result.Message = result.IsValid
            ? "Luhn check digit valid — DRN accepted"
            : "Luhn check digit error — DRN rejected (CTSA17 PASS)";

        return View("Index", result);
    }
}
