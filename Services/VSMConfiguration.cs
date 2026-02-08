// =============================================================================
//  VSMConfiguration.cs — Strongly-typed configuration for VSM settings
//  All settings can be modified in appsettings.json
// =============================================================================

namespace STSCompliancePOS.Services;

public class VSMConfiguration
{
    public string DeviceModel { get; set; } = "STSA-VSM-1";
    public string FirmwareVersion { get; set; } = "STS6-001";
    public string Protocol { get; set; } = "STS 600-8-6";

    public SerialPortConfig SerialPort { get; set; } = new();
    public TokenGenerationConfig TokenGeneration { get; set; } = new();
    public Dictionary<string, KeyRegisterConfig> KeyRegisters { get; set; } = new();
    public TestPANsConfig TestPANs { get; set; } = new();
    public ComplianceTestConfig ComplianceTest { get; set; } = new();

    // Helper to get register by name or number
    public KeyRegisterConfig? GetRegister(string regOrName)
    {
        // Try direct key lookup
        if (KeyRegisters.TryGetValue(regOrName, out var reg))
            return reg;

        // Try by register number
        foreach (var kvp in KeyRegisters)
        {
            if (kvp.Value.Register == regOrName)
                return kvp.Value;
        }

        return null;
    }

    // Get default register
    public KeyRegisterConfig? GetDefaultRegister()
    {
        foreach (var kvp in KeyRegisters)
        {
            if (kvp.Value.IsDefault)
                return kvp.Value;
        }
        return KeyRegisters.Values.FirstOrDefault();
    }
}

public class SerialPortConfig
{
    public string DefaultPort { get; set; } = "COM3";
    public int BaudRate { get; set; } = 9600;
    public int DataBits { get; set; } = 8;
    public string Parity { get; set; } = "None";
    public int StopBits { get; set; } = 1;
    public int ReadTimeout { get; set; } = 5000;
    public int WriteTimeout { get; set; } = 3000;
}

public class TokenGenerationConfig
{
    public int EA { get; set; } = 7;
    public int TCT { get; set; } = 1;
    public int DKGA { get; set; } = 4;
    public int RND { get; set; } = 5;
    public int MaxRetries { get; set; } = 3;
}

public class KeyRegisterConfig
{
    public string Register { get; set; } = "01";
    public string Description { get; set; } = "";
    public string VUDK { get; set; } = "";
    public string SGC { get; set; } = "201457";
    public int KRN { get; set; } = 1;
    public int BaseDate { get; set; } = 2014;
    public int KEN { get; set; } = 255;
    public bool IsDefault { get; set; } = false;
    public bool IsExpired { get; set; } = false;
}

public class TestPANsConfig
{
    public string PAN_11 { get; set; } = "600727000000000009";
    public string PAN_13 { get; set; } = "000001000000000082";
    public string PAN_CTSA15 { get; set; } = "600727111111111153";
}

public class ComplianceTestConfig
{
    public string Specification { get; set; } = "STS 531-1-07";
    public string Edition { get; set; } = "2.2";
    public string Date { get; set; } = "July 2025";
    public string EntityType { get; set; } = "A";
    public string Classification { get; set; } = "POSToTokenCarrierInterface";
}
