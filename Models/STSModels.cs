namespace STSCompliancePOS.Models;

// ─── Core STS Configuration ─────────────────────────────────────
// ReSharper disable  InconsistentNaming
public class APDUConfig
{
    public string MeterPAN { get; set; } = "600727000000000009";
    // ReSharper disable  UnusedMember.Global
    public string DRN => MeterPAN.Length == 18 ? MeterPAN[4..15] : MeterPAN[4..17];
    public string TCT { get; set; } = "01";
    public int DKGA { get; set; } = 04;
    public int EA { get; set; } = 07;
    public string SGC { get; set; } = "201457";
    public int BaseDate { get; set; } = 2014;
    public int TI { get; set; } = 01;
    public int KRN { get; set; } = 1;
    public int KT { get; set; } = 2;
    public int KeyExpiryNumber { get; set; } = 255;
    public string VUDK { get; set; } = "ABABABABABABABAB94949494949494940123456716";
    public string GetIDRecord(string tct) =>
        $"{MeterPAN}==={tct}{EA:D2}{SGC}{KRN:D2}{TI}";
}

// ─── Test Models ─────────────────────────────────────────────────
public class CTSATest
{
    public string TestId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Classification { get; set; } = "";
    public string IECClause { get; set; } = "";
    public List<CTSATestStep> Steps { get; set; } = [];
    public int PassedSteps => Steps.Count(s => s.Status == TestStatus.Pass);
    public int TotalSteps => Steps.Count;
    public TestStatus OverallStatus => Steps.All(s => s.Status == TestStatus.Pass)
        ? TestStatus.Pass
        : Steps.Any(s => s.Status == TestStatus.Fail)
            ? TestStatus.Fail
            : TestStatus.Pending;
}

public class CTSATestStep
{
    public int StepNumber { get; set; }
    public string Instruction { get; set; } = "";
    public string ExpectedToken { get; set; } = "";
    public string GeneratedToken { get; set; } = "";
    public string TokenIssueDate { get; set; } = "";
    public string UtilityType { get; set; } = "";
    public TestStatus Status { get; set; } = TestStatus.Pending;
    public Dictionary<string, string> Parameters { get; set; } = new();
}

public enum TestStatus { Pending, Running, Pass, Fail }

// ─── Vending Models ──────────────────────────────────────────────
public class VendRequest
{
    public string MeterPAN { get; set; } = "600727000000000009";
    public string TokenType { get; set; } = "TransferCredit";
    public string UtilityType { get; set; } = "Electricity";
    public decimal Amount { get; set; } = 0.1m;
    public string Unit { get; set; } = "kWh";
    public DateTime TokenIssueDate { get; set; } = new(2014, 3, 1, 13, 0, 0);
    public string SGC { get; set; } = "201457";
    public int BaseDate { get; set; } = 2014;
    public int KRN { get; set; } = 1;
    public int TI { get; set; } = 1;
}

public class VendResult
{
    public string GeneratedToken { get; set; } = "";
    public string ExpectedToken { get; set; } = "";
    public bool IsMatch => GeneratedToken == ExpectedToken;
    public string TokenType { get; set; } = "";
    public VendRequest? Request { get; set; }
    public TokenBreakdown? Breakdown { get; set; }
}

public class TokenBreakdown
{
    public string TokenClass { get; set; } = "";
    public string Subclass { get; set; } = "";
    public string TID { get; set; } = "";
    public string AmountEncoded { get; set; } = "";
    public int RND { get; set; } = 5;
    public bool CRCValid { get; set; } = true;
}

// ─── KeyChange Models ────────────────────────────────────────────
public class KeyChangeRequest
{
    public string MeterPAN { get; set; } = "600727000000000009";
    public string InitialSGC { get; set; } = "201457";
    public string NewSGC { get; set; } = "203557";
    public int InitialKRN { get; set; } = 1;
    public int NewKRN { get; set; } = 6;
    public int InitialTI { get; set; } = 1;
    public int NewTI { get; set; } = 2;
    public int InitialBaseDate { get; set; } = 2014;
    public int NewBaseDate { get; set; } = 2035;
    public int InitialKEN { get; set; } = 255;
    public int NewKEN { get; set; } = 255;
    public int RolloverBit { get; set; } = 0;
}

public class KeyChangeResult
{
    public string FirstKCT { get; set; } = "";
    public string SecondKCT { get; set; } = "";
    public string? ThirdKCT { get; set; }
    public string ExpectedFirstKCT { get; set; } = "";
    public string ExpectedSecondKCT { get; set; } = "";
    public string? ExpectedThirdKCT { get; set; }
    public bool FirstMatch => FirstKCT == ExpectedFirstKCT;
    public bool SecondMatch => SecondKCT == ExpectedSecondKCT;
    public bool ThirdMatch => ThirdKCT == null || ThirdKCT == ExpectedThirdKCT;
    public bool AllMatch => FirstMatch && SecondMatch && ThirdMatch;
    public KeyChangeRequest? Request { get; set; }
}

// ─── DRN Validation ──────────────────────────────────────────────
public class DRNValidationResult
{
    public string DRN { get; set; } = "";
    public bool IsValid { get; set; }
    public int ExpectedCheckDigit { get; set; }
    public int ActualCheckDigit { get; set; }
    public string Message { get; set; } = "";
}

// ─── Dashboard ───────────────────────────────────────────────────
public class DashboardViewModel
{
    public APDUConfig ActiveConfig { get; set; } = new();
    public int TotalTests { get; set; }
    public int PassedTests { get; set; }
    public int TotalSteps { get; set; }
    public int VendingTests { get; set; }
    public int EngineeringTests { get; set; }
    public int KeychangeTests { get; set; }
    public string VSMVersion { get; set; } = "6.2";
    public string AppVersion { get; set; } = "3.2.1";
    public string LastTestRun { get; set; } = "";
    
    // VSM Connection Status
    public bool IsVSMConnected { get; set; } = false;
    public string? ConnectedPort { get; set; }
    public string DeviceModel { get; set; } = "STSA-VSM-1";
    public string FirmwareVersion { get; set; } = "STS6-001";
}

// ─── Test Data Service ───────────────────────────────────────────
public class STSTestDataService
{
    public List<CTSATest> GetAllTests()
    {
        return
        [
            BuildCTSA01(),
            BuildCTSA02(),
            BuildCTSA03(),
            BuildCTSA04(),
            BuildCTSA05(),
            BuildCTSA06(),
            BuildCTSA07(),
            BuildCTSA09(),
            BuildCTSA10(),
            BuildCTSA11(),
            BuildCTSA12(),
            BuildCTSA13(),
            BuildCTSA14(),
            BuildCTSA15(),
            BuildCTSA16(),
            BuildCTSA17(),
            BuildCTSA18(),
            BuildCTSA19(),
            BuildCTSA20(),
            BuildCTSA21(),
            BuildCTSA22(),
            BuildCTSA23(),
            BuildCTSA24()
        ];
    }

    public CTSATest? GetTest(string testId) =>
        GetAllTests().FirstOrDefault(t => t.TestId.Equals(testId, StringComparison.OrdinalIgnoreCase));

    // ── CTSA01: TransferCredit ──
    private CTSATest BuildCTSA01() => new()
    {
        TestId = "CTSA01", Name = "TransferCredit",
        Description = "Verifies general compliance with respect to the generation of a TransferCredit token.",
        Classification = "V", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "0.1kWh TransferCredit, PAN=60072700..09, BD=2014, KRN=1",
                "7063 0503 4700 2872 6114", "2014-03-01 13:00", "Electricity"),

            Step(2, "0.1kl TransferCredit, PAN=60072700..09, BD=2014, KRN=1",
                "2551 0558 0109 3250 6821", "2014-03-01 13:05", "Water"),

            Step(3, "0.1m³ TransferCredit, PAN=60072700..09, BD=2014, KRN=1",
                "1762 3693 5256 1492 7277", "2014-03-01 13:10", "Gas"),

            Step(4, "0.1min TransferCredit, PAN=60072700..09, BD=2014, KRN=1",
                "0974 6334 4999 1398 8084", "2014-03-01 13:15", "Time"),

            Step(5, "0.1kWh TransferCredit, PAN=00000100..82, BD=2014, KRN=1",
                "0369 6784 3965 6719 3485", "2014-03-01 13:40", "Electricity"),

            Step(6, "0.1kl TransferCredit, PAN=00000100..82, BD=2014, KRN=1",
                "0353 0385 5551 6557 9931", "2014-03-01 13:45", "Water"),

            Step(7, "0.1m³ TransferCredit, PAN=00000100..82, BD=2014, KRN=1",
                "0545 5142 6230 5752 5239", "2014-03-01 13:50", "Gas"),

            Step(8, "0.1min TransferCredit, PAN=00000100..82, BD=2014, KRN=1",
                "3449 8403 1437 9750 2264", "2014-03-01 13:55", "Time"),

            Step(9, "0.1kWh TransferCredit, BD=2035, KRN=6, SGC=203557",
                "4921 7377 4370 6573 1889", "2035-01-01 08:00", "Electricity"),

            Step(10, "0.1kl TransferCredit, BD=2035, KRN=6, SGC=203557",
                "6588 8845 1435 4412 8525", "2035-01-01 08:05", "Water"),

            Step(11, "0.1m³ TransferCredit, BD=2035, KRN=6, SGC=203557",
                "4724 3053 8433 6996 6174", "2035-01-01 08:10", "Gas"),

            Step(12, "0.1min TransferCredit, BD=2035, KRN=6, SGC=203557",
                "3012 7467 7392 8379 0161", "2035-01-01 08:15", "Time")

        ]
    };

    // ── CTSA02: InitiateMeterTest/Display ──
    private CTSATest BuildCTSA02() => new()
    {
        TestId = "CTSA02", Name = "InitiateMeterTest/Display",
        Description = "Verifies compliance with respect to the generation of an InitiateMeterTest/Display token (Test 0).",
        Classification = "E", IECClause = "6.2, 6.3, 6.4",
        Steps =
        [
            Step(1, "InitiateMeterTest/Display, PAN=60072700..09 (11-digit DRN)",
                "5649 3153 7254 5031 3471", "", ""),

            Step(2, "InitiateMeterTest/Display, PAN=00000100..82 (13-digit DRN)",
                "0230 5843 0050 5295 1967", "", "")

        ]
    };

    // ── CTSA03: SetMaximumPowerLimit ──
    private CTSATest BuildCTSA03() => new()
    {
        TestId = "CTSA03", Name = "SetMaximumPowerLimit",
        Description = "Verifies general compliance with respect to the generation of a SetMaximumPowerLimit token.",
        Classification = "E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "SetMaximumPowerLimit 1kW, PAN=60072700..09",
                "6896 1683 0643 2623 4122", "2024-03-28 09:01", "Electricity")

        ]
    };

    // ── CTSA04: ClearCredit ──
    private CTSATest BuildCTSA04() => new()
    {
        TestId = "CTSA04", Name = "ClearCredit",
        Description = "Verifies general compliance with respect to the generation of a ClearCredit token.",
        Classification = "E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "ClearCredit, PAN=60072700..09, RegisterToClear=0xFFFF",
                "0006 6562 8693 3303 1750", "2024-03-28 09:15", ""),

            Step(2, "ClearCredit, PAN=00000100..82",
                "0948 5016 9352 0867 1611", "2016-08-30 09:20", "")

        ]
    };

    // ── CTSA05: Keychange Token Set ──
    private CTSATest BuildCTSA05() => new()
    {
        TestId = "CTSA05", Name = "KeyChange Token Set",
        Description = "Verifies general compliance with respect to the generation of a Keychange token set.",
        Classification = "K", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "Keychange pair with VUDK1, BD=2014→2014, SGC=201457→203557, TI=01→02",
                "0651 0894 1414 5301 8386 | 2297 9178 2799 9720 8272", "", ""),

            Step(2, "Keychange pair with VUDK2, BD=2014→2035, KRN=6, SGC=203557",
                "6541 3070 5436 8815 6280 | 2746 9374 5674 7695 3413", "", ""),

            Step(3, "Keychange pair, PAN=00000100..82",
                "2413 8686 0847 3991 1945 | 5070 7058 4795 3904 6218", "", ""),

            Step(4, "Keychange triplet (3 tokens)",
                "5250 6022 8503 9960 3630 | 2297 9178 2799 9720 8272 | 3962 0459 0596 1503 7790", "", "")

        ]
    };

    // ── CTSA06: ClearTamperCondition ──
    private CTSATest BuildCTSA06() => new()
    {
        TestId = "CTSA06", Name = "ClearTamperCondition",
        Description = "Verifies general compliance with respect to the generation of a ClearTamperCondition token.",
        Classification = "E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "ClearTamperCondition, PAN=60072700..09",
                "4254 5072 1128 4378 0101", "2025-03-28 10:00", "")

        ]
    };

    // ── CTSA07: SetMaximumPhasePowerUnbalanceLimit ──
    private CTSATest BuildCTSA07() => new()
    {
        TestId = "CTSA07", Name = "SetMaxPhasePowerUnbalance",
        Description = "Verifies generation of a SetMaximumPhasePowerUnbalanceLimit token.",
        Classification = "E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "SetMPUL 10W, PAN=60072700..09",
                "3171 2008 4993 5179 7283", "2025-03-28 10:20", "Electricity")

        ]
    };

    // ── CTSA09: TokenIdentifier ──
    private CTSATest BuildCTSA09() => new()
    {
        TestId = "CTSA09", Name = "TokenIdentifier (TID)",
        Description = "Verifies TID field, SpecialReservedTokenIdentifier and multiple tokens in the same minute.",
        Classification = "V,E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "ClearCredit token, 1st date/time 2026-07-29 00:00",
                "4249 4289 3188 2320 0303", "2026-07-29 00:00", ""),

            Step(2, "ClearCredit token, 2nd date/time 2026-07-29 00:01",
                "6864 5810 8010 6812 7886", "2026-07-29 00:01", ""),

            Step(3, "Three ClearCredit tokens same minute 2026-07-29 00:03",
                "2705 3689 8139 2723 1094", "2026-07-29 00:03", ""),

            Step(4, "TransferCredit 1kWh, 1st date/time (Electricity)",
                "2080 9920 0038 3276 4000", "2026-07-29 00:00", "Electricity"),

            Step(5, "TransferCredit 1kWh, 2nd date/time (Electricity)",
                "5600 3214 8830 0506 4177", "2026-07-29 00:01", "Electricity"),

            Step(6, "Three TransferCredit tokens same minute (Electricity)",
                "5900 6662 8636 1607 6238", "2026-07-29 00:03", "Electricity")

        ]
    };

    // ── CTSA10: TransferAmount ──
    private CTSATest BuildCTSA10() => new()
    {
        TestId = "CTSA10", Name = "TransferAmount",
        Description = "Verifies the calculation of the TransferAmount field across the full range.",
        Classification = "V", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "25.6 kWh", "3556 4630 5097 8570 1608", "2024-04-01 00:30", "Electricity"),
            Step(2, "1638.3 kWh", "4077 4607 5834 4958 9001", "2024-04-01 00:35", "Electricity"),
            Step(3, "1638.4 kWh", "4433 2531 3579 8763 1349", "2024-04-01 00:40", "Electricity"),
            Step(4, "2000.0 kWh", "0860 9861 4278 9558 3861", "2024-04-01 00:45", "Electricity"),
            Step(5, "18022.3 kWh", "1772 1942 4578 2032 1090", "2024-04-01 00:50", "Electricity"),
            Step(6, "18022.4 kWh", "3950 1554 8515 1130 5647", "2025-04-01 00:55", "Electricity"),
            Step(7, "181862.3 kWh", "2166 8945 0305 1739 5874", "2025-04-01 01:44", "Electricity"),
            Step(8, "181862.4 kWh", "5216 8966 5314 0946 2770", "2025-04-01 01:49", "Electricity"),
            Step(9, "1820162.4 kWh", "4573 4395 4774 6024 3909", "2025-04-01 01:54", "Electricity")
        ]
    };

    // ── CTSA11: DisplayControlField ──
    private CTSATest BuildCTSA11() => new()
    {
        TestId = "CTSA11", Name = "DisplayControlField",
        Description = "Verifies InitiateMeterTest/Display token control field calculation and bit positions.",
        Classification = "E", IECClause = "6.2, 6.3, 6.4",
        Steps =
        [
            Step(1, "DisplayControlField = 0x01", "0000 0000 0001 5099 7584", "", ""),
            Step(2, "DisplayControlField = 0x02", "0000 0000 0001 6777 4880", "", ""),
            Step(3, "DisplayControlField = 0x04", "0000 0000 0002 0132 8896", "", ""),
            Step(4, "DisplayControlField = 0x08", "1844 6744 0738 4377 2416", "", ""),
            Step(5, "DisplayControlField = 0x10", "3689 3488 1475 5332 2496", "", ""),
            Step(6, "DisplayControlField = 0x20", "0000 0000 0006 7109 3248", "", ""),
            Step(7, "DisplayControlField = 0x40", "0000 0000 0012 0797 4400", "", ""),
            Step(8, "DisplayControlField = 0x80", "0000 0000 0022 8172 8512", "", ""),
            Step(9, "DisplayControlField = 0x100", "0000 0000 0044 2920 8064", "", ""),
            Step(10, "DisplayControlField = 0x200", "0000 0000 0087 2419 5840", "", ""),
            Step(11, "DisplayControlField = 0x400", "0000 0000 0173 1410 5857", "", ""),
            Step(12, "DisplayControlField = 0x2000", "0000 0000 1375 7317 3770", "", ""),
            Step(13, "DisplayControlField = 0x4000", "0000 0000 2750 1212 7252", "", ""),
            Step(14, "DisplayControlField = 0x8000", "0000 0000 5498 9003 4216", "", ""),
            Step(15, "DisplayControlField = 0x10000", "0000 0001 0996 4584 8124", "", ""),
            Step(16, "DisplayControlField = 0x20000", "0000 0002 1991 5747 5960", "", "")
        ]
    };

    // ── CTSA12-24 simplified ──
    private CTSATest BuildCTSA12() => new()
    {
        TestId = "CTSA12", Name = "MaximumPowerLimit",
        Description = "Verifies MPL field calculation across the full range.",
        Classification = "E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "MPL=256W", "2564 9099 8064 3968 8818", "2026-04-01 07:00", "Electricity"),
            Step(2, "MPL=16383W", "5563 9962 3017 4504 3027", "2026-04-01 07:05", "Electricity"),
            Step(3, "MPL=16384W", "3367 4864 3919 1340 5372", "2026-04-01 07:10", "Electricity"),
            Step(4, "MPL=20000W", "0202 0070 4917 9716 0201", "2026-04-01 07:15", "Electricity"),
            Step(5, "MPL=180223W", "7362 8944 8139 7028 9630", "2026-04-01 07:20", "Electricity"),
            Step(6, "MPL=180224W", "5303 3624 2291 2612 5067", "2014-04-01 07:25", "Electricity"),
            Step(7, "MPL=1818623W", "2933 7964 5297 3710 1507", "2014-04-01 07:30", "Electricity"),
            Step(8, "MPL=1818624W", "0579 5204 3415 0178 1011", "2014-04-01 07:35", "Electricity"),
            Step(9, "MPL=18201624W", "0081 0499 0096 0932 9644", "2014-04-01 07:40", "Electricity")
        ]
    };

    private CTSATest BuildCTSA13() => new()
    {
        TestId = "CTSA13", Name = "MaxPhasePowerUnbalance",
        Description = "Verifies MPUL field calculation.", Classification = "E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "MPUL=256W", "4988 9284 1816 7609 5740", "2025-04-01 08:00", "Electricity"),
            Step(2, "MPUL=16383W", "6389 4111 6658 0214 0435", "2025-04-01 08:05", "Electricity"),
            Step(3, "MPUL=16384W", "2794 8862 9560 0408 4558", "2026-04-01 08:10", "Electricity"),
            Step(4, "MPUL=20000W", "3743 0198 7661 7112 8155", "2026-04-01 08:15", "Electricity"),
            Step(5, "MPUL=180223W", "3563 8674 0685 1738 1212", "2026-04-01 08:20", "Electricity"),
            Step(6, "MPUL=180224W", "0127 5579 2532 8342 4482", "2026-04-01 08:25", "Electricity"),
            Step(7, "MPUL=1818623W", "2518 9403 6086 5361 4131", "2026-04-01 08:30", "Electricity"),
            Step(8, "MPUL=1818624W", "2775 5565 7076 4464 4385", "2026-04-01 08:35", "Electricity"),
            Step(9, "MPUL=18201624W", "1821 7654 1633 3370 8860", "2026-04-01 08:40", "Electricity")
        ]
    };

    private CTSATest BuildCTSA14() => new()
    {
        TestId = "CTSA14", Name = "RegisterToClear",
        Description = "Verifies RegisterToClear field calculation.", Classification = "E", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "RegisterToClear=0x0000 (Elec)", "0989 3459 5444 3192 5054", "2026-04-01 09:00", "Electricity"),
            Step(2, "RegisterToClear=0xFFFF (All)", "6779 8335 6108 7015 3959", "2026-04-01 09:05", ""),
            Step(3, "RegisterToClear=0x0004 (Elec Currency)", "5360 0021 1660 7239 5417", "2026-04-01 09:10", ""),
            Step(4, "RegisterToClear=0x0005 (Water Currency)", "4552 0106 5407 8931 8612", "2026-04-01 09:15", ""),
            Step(5, "RegisterToClear=0x0006 (Gas Currency)", "0261 8248 0224 9352 6807", "2026-04-01 09:20", ""),
            Step(6, "RegisterToClear=0x0007 (Time Currency)", "6866 5590 5943 1680 3019", "2026-04-01 09:25", "")
        ]
    };

    private CTSATest BuildCTSA15() => new()
    {
        TestId = "CTSA15", Name = "KeyChange Fields (NKHO/NKLO/KENHO/KENLO/RO)",
        Description = "Verifies key change token fields.", Classification = "K", IECClause = "6.2, 6.3, 6.4, 6.5",
        Steps =
        [
            Step(1, "Keychange set, PAN=600727111111111153, Initial→New VUDK",
                "4668 6806 2833 3512 6857 | 0586 7422 7714 3986 8287", "", "")

        ]
    };

    private CTSATest BuildCTSA16() => new()
    {
        TestId = "CTSA16", Name = "KeyExpiryNumber",
        Description = "Verifies POS rejects tokens when vending key has expired.", Classification = "V,E,K", IECClause = "6.1, 6.3, 6.5",
        Steps =
        [
            Step(1, "TransferCredit with expired KEN=85 → Expect rejection", "KEY EXPIRY ERROR", "2032-10-27 08:00",
                ""),
            Step(2, "ClearAllCredit with expired KEN=85 → Expect rejection", "KEY EXPIRY ERROR", "2032-10-27 08:00",
                ""),
            Step(3, "Keychange with expired KEN=85 → Expect rejection", "KEY EXPIRY ERROR", "2032-10-27 08:00", ""),
            Step(4, "Keychange SGC2→SGC1 with valid new KEN=255",
                "2456 2245 0232 0355 5040 | 4155 6682 0774 2828 0522", "", "")

        ]
    };

    private CTSATest BuildCTSA17() => new()
    {
        TestId = "CTSA17", Name = "DRN Check Digit (Luhn)",
        Description = "Verifies Luhn check digit validation on DRN.", Classification = "V,E,K", IECClause = "6.1",
        Steps =
        [
            Step(1, "Enter DRN 12345678904 and 11111111113 → Expect Luhn error", "LUHN ERROR DETECTED", "", "")
        ]
    };

    private CTSATest BuildCTSA18() => new()
    {
        TestId = "CTSA18", Name = "DateOfExpiry",
        Description = "Verifies expired meter ID card is rejected.", Classification = "V,E,K", IECClause = "6.1",
        Steps =
        [
            Step(1, "Create ID card with DOE=06/2004", "ID CARD CREATED", "", ""),
            Step(2, "Read ID card after 01/07/2004 → Expect expired indication", "EXPIRED CARD DETECTED", "", "")
        ]
    };

    private CTSATest BuildCTSA19() => new()
    {
        TestId = "CTSA19", Name = "Auto KeyChange Generation",
        Description = "Verifies automatic keychange generation when attributes change.", Classification = "V,K", IECClause = "6.5.2.1",
        Steps =
        [
            Step(1, "Change TI 01→02, auto-KCT + credit token (Electricity)",
                "KCT: 0651 0894 1414 5301 8386 | 2297 9178 2799 9720 8272 + Credit: 3423 2103 3705 7539 7779",
                "2024-04-01 22:00", "Electricity"),

            Step(2, "Change KRN 1→2, auto-KCT + credit token (Electricity)",
                "KCT: 1379 2319 6741 3607 0115 | 2433 8605 6663 7543 7668 + Credit: 1574 7888 1821 1816 3921",
                "2024-04-01 22:10", "Electricity"),

            Step(3, "Change KEN 255→170, auto-KCT + credit token (Electricity)",
                "KCT: 4508 3024 0844 3428 7694 | 5967 2793 5037 0156 8138 + Credit: 5609 1860 3906 6570 9907",
                "2024-04-01 10:15", "Electricity"),

            Step(4, "Change SGC 201457→201461, auto-KCT + credit token (Electricity)",
                "KCT: 4126 6369 3453 5829 5161 | 2199 7097 8071 0914 8443 + Credit: 5706 6075 0594 8148 6900",
                "2024-04-01 22:20", "Electricity")

        ]
    };

    private CTSATest BuildCTSA20() => new()
    {
        TestId = "CTSA20", Name = "Currency Token (Electricity)",
        Description = "Verifies TransferCurrency token generation and exponent calculation for Electricity.", Classification = "V", IECClause = "5, 6",
        Steps =
        [
            Step(1, "Amount=1", "4233 4201 2699 4723 2996", "2024-04-21 10:01", "Elec Currency"),
            Step(2, "Amount=16383", "2408 1984 2660 6677 2647", "2024-04-21 10:02", "Elec Currency"),
            Step(3, "Amount=16384", "4505 9661 3519 4228 5883", "2025-04-21 10:03", "Elec Currency"),
            Step(4, "Amount=180224", "6422 0455 7843 4267 3901", "2025-04-22 10:04", "Elec Currency"),
            Step(5, "Amount=1818624", "5206 3757 1311 2542 2816", "2025-05-01 11:00", "Elec Currency"),
            Step(6, "Amount=18202624", "1755 0937 0728 0756 2588", "2025-05-11 11:01", "Elec Currency"),
            Step(7, "Amount=182042624", "4275 9331 6826 4411 0925", "2025-05-21 11:02", "Elec Currency"),
            Step(8, "Amount=1820442624", "6338 5403 0467 9476 0900", "2025-05-21 11:03", "Elec Currency"),
            Step(9, "Amount=18204442624", "5084 5361 4716 6437 1131", "2025-05-21 11:04", "Elec Currency"),
            Step(10, "Amount=1.82044E+14", "7066 3553 6287 7145 4741", "2025-05-21 11:05", "Elec Currency"),
            Step(11, "Amount=1.82044E+20", "1208 4156 5358 4592 1729", "2025-05-21 11:10", "Elec Currency"),
            Step(12, "Amount=1.82044E+29", "5912 8295 8155 0145 3280", "2025-05-21 11:11", "Elec Currency"),
            Step(13, "Amount=-1", "2779 2899 8336 6628 5579", "2025-05-21 11:12", "Elec Currency"),
            Step(14, "Amount=-180224", "1481 8605 6853 2889 6110", "2025-05-21 11:14", "Elec Currency"),
            Step(15, "Amount=-1.82044E+14", "6028 1485 3593 9320 3514", "2025-05-21 11:15", "Elec Currency")
        ]
    };

    private CTSATest BuildCTSA21() => new()
    {
        TestId = "CTSA21", Name = "Currency Token (Water)",
        Description = "Verifies TransferCurrency token for Water.", Classification = "V", IECClause = "5, 6",
        Steps =
        [
            Step(1, "Amount=1", "1386 8574 1912 6060 4003", "2025-05-22 10:01", "Water Currency"),
            Step(2, "Amount=16383", "3119 0854 9871 2699 7393", "2025-05-22 10:02", "Water Currency"),
            Step(3, "Amount=16384", "2952 3612 9699 0372 7226", "2026-04-21 10:03", "Water Currency"),
            Step(4, "Amount=180224", "3844 7230 9916 9843 7168", "2026-04-22 10:04", "Water Currency"),
            Step(5, "Amount=1818624", "4180 9072 1119 0881 8207", "2026-05-11 11:00", "Water Currency")
        ]
    };

    private CTSATest BuildCTSA22() => new()
    {
        TestId = "CTSA22", Name = "Currency Token (Gas)",
        Description = "Verifies TransferCurrency token for Gas.", Classification = "V", IECClause = "5, 6",
        Steps =
        [
            Step(1, "Amount=1", "3308 8459 8886 7553 4293", "2026-05-22 10:01", "Gas Currency"),
            Step(2, "Amount=16383", "2013 4722 7589 8200 4247", "2026-05-22 10:02", "Gas Currency"),
            Step(3, "Amount=16384", "0128 6833 8696 6247 0263", "2027-04-21 10:03", "Gas Currency"),
            Step(4, "Amount=180224", "3429 5922 1070 8024 0911", "2027-05-11 10:04", "Gas Currency"),
            Step(5, "Amount=1818624", "4672 9798 0444 8984 0104", "2027-05-21 11:00", "Gas Currency")
        ]
    };

    private CTSATest BuildCTSA23() => new()
    {
        TestId = "CTSA23", Name = "Currency Token (Time)",
        Description = "Verifies TransferCurrency token for Time.", Classification = "V", IECClause = "5, 6",
        Steps =
        [
            Step(1, "Amount=1", "7342 9236 0020 8368 9299", "2027-05-22 10:01", "Time Currency"),
            Step(2, "Amount=16383", "0324 6831 8657 5679 2924", "2027-05-22 10:02", "Time Currency"),
            Step(3, "Amount=16384", "3541 2165 9170 5229 7575", "2028-04-21 10:03", "Time Currency"),
            Step(4, "Amount=180224", "1480 1564 9822 3217 6235", "2028-05-11 10:04", "Time Currency"),
            Step(5, "Amount=1818624", "5378 9632 8119 3337 9082", "2028-05-21 11:00", "Time Currency")
        ]
    };

    private CTSATest BuildCTSA24() => new()
    {
        TestId = "CTSA24", Name = "Extended Token Set (STS202-5)",
        Description = "Verifies Class2 SubClass10 extended token set.", Classification = "E", IECClause = "STS202-5",
        Steps =
        [
            Step(1, "Class2 SC10: Index=63, FlagIndex=0, FlagValue=0", "2953 0401 2625 4856 3889", "2029-01-01 09:00",
                ""),
            Step(2, "Class2 SC10: Index=63, FlagIndex=0, FlagValue=1", "4691 6500 9528 3644 7494", "2029-01-01 09:05",
                ""),
            Step(3, "Class2 SC10: Index=0, ControlValue=0", "0290 5758 1423 5048 0527", "2029-01-01 09:10", ""),
            Step(4, "Class2 SC10: Index=0, ControlValue=0123", "2632 9856 3088 1775 0454", "2029-01-01 09:15", ""),
            Step(5, "Class1 SC2 ReadControl, value=0x0", "0230 5843 0093 4791 4912", "", ""),
            Step(6, "Class1 SC2 ReadFlag, value=0xFC0000000", "0344 0750 1154 4527 9822", "", "")
        ]
    };

    private static CTSATestStep Step(int num, string instruction, string token, string date, string utility) => new()
    {
        StepNumber = num,
        Instruction = instruction,
        ExpectedToken = token,
        GeneratedToken = token, // Simulating PASS
        TokenIssueDate = date,
        UtilityType = utility,
        Status = TestStatus.Pass
    };
}
