# STS Compliance POS — MDM Vending Station Web App

ASP.NET Core MVC web application for STS-compliant POS vending systems with **real hardware testing**
via COM port connection to STS6 HSM (STSA-VSM-1). Implements all CTSA tests per **STS 531-1-07 Edition 2.2** (DKGA04/EA07).

## Features

- 🔌 **COM Port Connection** — Connect to STS6 HSM via serial port (configurable)
- 🧪 **Real-time Compliance Testing** — Run all CTSA tests against actual hardware
- ⚡ **Token Generation** — Generate real STS tokens via VSM
- 📊 **Live Progress** — SignalR-powered real-time test updates
- 📄 **Export Reports** — CSV, HTML, TXT export with print support
- ⚙️ **Fully Configurable** — All settings in `appsettings.json`
- 📋 **Full Test Vectors** — All expected tokens from STS 531-1-07 Ed 2.2

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- STS6 HSM device (STSA-VSM-1 or compatible)
- Pre-loaded VUDK keys in VSM registers

## Quick Start

```bash
cd STSCompliancePOS
dotnet restore
dotnet run
```

Open `https://localhost:5001` in your browser.

## Configuration (appsettings.json)

All settings are configurable in `appsettings.json`:

```json
{
  "VSMConfiguration": {
    "DeviceModel": "STSA-VSM-1",
    "FirmwareVersion": "STS6-001",
    "Protocol": "STS 600-8-6",
    
    "SerialPort": {
      "DefaultPort": "COM3",
      "BaudRate": 9600,
      "DataBits": 8,
      "Parity": "None",
      "StopBits": 1,
      "ReadTimeout": 5000,
      "WriteTimeout": 3000
    },

    "TokenGeneration": {
      "EA": 7,
      "TCT": 1,
      "DKGA": 4,
      "RND": 5,
      "MaxRetries": 3
    },

    "KeyRegisters": {
      "VKREG01": {
        "Register": "01",
        "SGC": "201457",
        "KRN": 1,
        "BaseDate": 2014,
        "KEN": 255,
        "IsDefault": true
      }
    },

    "TestPANs": {
      "PAN_11": "600727000000000009",
      "PAN_13": "000001000000000082",
      "PAN_CTSA15": "600727111111111153"
    }
  }
}
```

## Project Structure

```
STSCompliancePOS/
├── Program.cs                          # App entry + SignalR setup
├── appsettings.json                    # All configurable settings
├── Controllers/
│   ├── HomeController.cs               # Dashboard
│   ├── VendingController.cs            # Token vending UI
│   ├── ComplianceController.cs         # Test suite UI
│   ├── ConnectionController.cs         # COM port connection UI
│   └── VSMController.cs                # REST API for VSM
├── Services/
│   ├── SmDriver.cs                     # Serial port driver for STS6 HSM
│   ├── StsHelper.cs                    # TID calc, amount encoding, Luhn
│   ├── VSMConnectionService.cs         # Singleton COM port manager
│   ├── VSMConfiguration.cs             # Strongly-typed configuration
│   ├── ComplianceTestService.cs        # Test runner with all vectors
│   └── ReportService.cs                # CSV/HTML/TXT export
├── Hubs/
│   └── TestHub.cs                      # SignalR hub for real-time updates
├── Views/
│   ├── Connection/Index.cshtml         # COM port selection UI
│   └── ...
└── wwwroot/
    ├── js/vsm-connection.js            # SignalR client
    └── reports/                        # Exported reports folder
```

## Screens

| Screen | Route | Description |
|--------|-------|-------------|
| Dashboard | `/` | System status, config overview, VSM connection status |
| **VSM Connection** | `/Connection` | **COM port selection, quick token test, key registers** |
| Vend Token | `/Vending` | Manual token generation form |
| Compliance Suite | `/Compliance` | All CTSA tests with run/export options |
| Test Detail | `/Compliance/Detail/CTSA01` | Step-by-step token verification |
| Key Change | `/KeyChange` | Keychange token generation |
| Meter Setup | `/MeterSetup` | DRN/Luhn validation, CTSA16/18 |

## COM Port Communication

The app uses `System.IO.Ports.SerialPort` to communicate with the STS6 HSM:

| Setting | Default | Configurable |
|---------|---------|--------------|
| Baud Rate | 9600 | ✓ appsettings.json |
| Data Bits | 8 | ✓ |
| Parity | None | ✓ |
| Stop Bits | 1 | ✓ |
| Read Timeout | 5000ms | ✓ |
| Write Timeout | 3000ms | ✓ |

### VSM Commands (STS 600-8-6)

| Command | Description |
|---------|-------------|
| `SM?VC` | Vend Credit Token |
| `SM?VM` | Vend Management Token |
| `SM?VK` | Vend Keychange Tokens |
| `SM?DI` | Get Identification |
| `SM?GA` | Get Key Attributes |

## Pre-loaded Key Registers

Configure in `appsettings.json` → `KeyRegisters`:

| Register | Description | SGC | KRN | BD | KEN |
|----------|-------------|-----|-----|-----|-----|
| VKREG01 | Main VUDK (default) | 201457 | 1 | 2014 | 255 |
| VKREG06 | BD=2035 | 203557 | 6 | 2035 | 255 |
| VKREG07 | Expired (KEN=85) | 201460 | 7 | 2014 | 85 |
| VKREG09 | Swapped VUDK | 201462 | 1 | 2014 | 255 |

## API Endpoints

```
GET  /api/vsm/status          — Connection status + available ports
GET  /api/vsm/ports           — List available COM ports
GET  /api/vsm/config          — Get full configuration
GET  /api/vsm/registers       — Get key register configuration
POST /api/vsm/connect         — Connect to port { portName: "COM3" }
POST /api/vsm/disconnect      — Disconnect
POST /api/vsm/generate-token  — Generate single token
POST /api/vsm/run-test        — Run individual CTSA test
POST /api/vsm/run-suite       — Run full compliance suite

# Export endpoints
GET  /api/vsm/export/csv      — Download CSV report
GET  /api/vsm/export/html     — Download HTML report
GET  /api/vsm/export/txt      — Download TXT report
GET  /api/vsm/report/print    — Get printable HTML report
```

## SignalR Events

The `/testhub` endpoint provides real-time updates:

| Event | Direction | Description |
|-------|-----------|-------------|
| `ReceiveConnectionStatus` | Server→Client | Connection state changed |
| `ReceiveProgress` | Server→Client | Test progress message |
| `ReceiveStepResult` | Server→Client | Individual step result |
| `ReceiveTestComplete` | Server→Client | Full suite completed |
| `ReceiveSingleTestComplete` | Server→Client | Single test completed |
| `ReceiveToken` | Server→Client | Token generation result |

## Test Data

All test vectors from **STS 531-1-07 Edition 2.2 (July 2025)**:

- VUDK: `ABABABABABABABAB94949494949494940123456716`
- PANs: Configurable in `appsettings.json`
- RND: Fixed at 5 for all test tokens (configurable)
- Base Dates: 2014 and 2035

## Export Formats

### CSV Export
```csv
TestID,Step,Description,Expected,Actual,Result,Error,Timestamp
"CTSA01",1,"0.1 E TransferCredit","7063 0503 4700 2872 6114","7063050347002872611",PASS,"","2024-03-01 13:00:00"
```

### HTML Export
Full styled report with test summary, pass/fail statistics, and detailed results.

### TXT Export
Console-style report matching the reference solution output format.

## Running Individual Tests

From the Compliance page or Connection page, select a test from the dropdown:
- CTSA01 — TransferCredit
- CTSA02 — InitiateMeterTest/Display
- CTSA03 — SetMaximumPowerLimit
- CTSA04 — ClearCredit
- CTSA05 — Keychange tokens
- CTSA06 — ClearTamperCondition
- CTSA07 — SetMaxPhasePowerUnbalance
- CTSA09 — TokenIdentifier (TID)
- CTSA10 — TransferAmount
- CTSA12 — MaximumPowerLimit
- CTSA13 — MaxPhasePowerUnbalance
- CTSA14 — RegisterToClear
- CTSA15 — Keychange Fields
- CTSA16 — KeyExpiryNumber
- CTSA17 — DRN Check Digit
- CTSA20 — Currency (Electricity)
- CTSA24 — Extended Token Set

## License

Internal use — STS test data is copyright © STS Association.
