// =============================================================================
//  VSMConnectionService.cs — Manages COM port connection to STS6 HSM
//  Singleton service for ASP.NET Core
// =============================================================================

using System.IO.Ports;

namespace STSCompliancePOS.Services;

public class VSMConnectionService : IDisposable
{
    private SmDriver? _driver;
    private string? _connectedPort;
    private readonly object _lock = new();

    public bool IsConnected => _driver != null && _connectedPort != null;
    public string? ConnectedPort => _connectedPort;
    public SmDriver? Driver => _driver;
    public string LastError { get; private set; } = "";

    // Get available COM ports
    public static string[] GetAvailablePorts()
    {
        try
        {
            return SerialPort.GetPortNames().OrderBy(p => p).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    // Connect to specified COM port
    public bool Connect(string portName)
    {
        lock (_lock)
        {
            // Close existing connection
            Disconnect();

            try
            {
                _driver = new SmDriver();
                if (_driver.Open(portName))
                {
                    _connectedPort = portName;
                    LastError = "";

                    // Try to get identification
                    var id = _driver.GetIdentification();
                    if (id != null)
                    {
                        Console.WriteLine($"[VSM] Connected to {portName}: {id}");
                    }
                    else
                    {
                        Console.WriteLine($"[VSM] Connected to {portName} (ID query not supported)");
                    }

                    return true;
                }
                else
                {
                    LastError = _driver.LastError;
                    _driver = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _driver?.Dispose();
                _driver = null;
                return false;
            }
        }
    }

    // Disconnect
    public void Disconnect()
    {
        lock (_lock)
        {
            if (_driver != null)
            {
                Console.WriteLine($"[VSM] Disconnecting from {_connectedPort}");
                _driver.Close();
                _driver = null;
                _connectedPort = null;
            }
        }
    }

    // Get key register status
    public string? GetKeyStatus(string register)
    {
        lock (_lock)
        {
            if (_driver == null) return null;
            return _driver.GetKeyStatus(register);
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}

// Connection status model for API
public class ConnectionStatus
{
    public bool IsConnected { get; set; }
    public string? ConnectedPort { get; set; }
    public string[] AvailablePorts { get; set; } = Array.Empty<string>();
    public string? LastError { get; set; }
}
