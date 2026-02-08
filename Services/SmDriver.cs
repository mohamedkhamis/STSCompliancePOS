// =============================================================================
//  SmDriver.cs — Serial communication driver for STS6 HSM (STS 600-8-6)
//  Implements SM?VC, SM?VM, SM?VK commands with PTVD field encoding
//  CRC-16/ARC: poly=0x8005, init=0, refIn/refOut=true
//  Target: STSA-VSM-1 (STS6-001 firmware)
// =============================================================================

using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace STSCompliancePOS.Services
{
    public class SmDriver : IDisposable
    {
        private SerialPort _port;
        private const int MAX_RETRIES = 3;
        private const int RSP_BUF_SIZE = 1024;

        // EA and TCT defaults for compliance testing (EA07=STA/DES, TCT=1=numeric)
        public int EA { get; set; } = 7;
        public int TCT { get; set; } = 1;

        public string LastError { get; private set; } = "";
        public string LastTx { get; private set; } = "";
        public string LastRx { get; private set; } = "";

        // Legacy compat aliases
        public string LastTxHex => LastTx;
        public string LastRxHex => LastRx;

        // =================================================================
        //  Serial Port Open / Close
        // =================================================================
        public bool Open(string portName)
        {
            try
            {
                _port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                _port.ReadTimeout = 5000;
                _port.WriteTimeout = 3000;
                _port.Open();
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }

        public void Close()
        {
            _port?.Close();
            _port?.Dispose();
            _port = null;
        }

        public void Dispose() => Close();

        // =================================================================
        //  PTVD Field Encoding (STS 600-8-6 Section 5.4)
        // =================================================================

        /// <summary>Number: "N" + decimal digits + "~" (no leading zeros)</summary>
        public static string PtvdN(int value)
        {
            return $"N{value}~";
        }

        /// <summary>PrintableString: "P" + chars + "~"</summary>
        public static string PtvdP(string value)
        {
            return $"P{value}~";
        }

        /// <summary>HexOctets: "H" + uppercase hex + "~" (even digit count, minimal)</summary>
        public static string PtvdH(uint value)
        {
            string hex = value.ToString("X");
            if (hex.Length % 2 != 0) hex = "0" + hex;
            return $"H{hex}~";
        }

        /// <summary>HexOctets from raw hex string</summary>
        public static string PtvdHRaw(string hexStr)
        {
            return $"H{hexStr.ToUpper()}~";
        }

        /// <summary>DateTime: "D" + CCYYMMDDhhmmss + "~"</summary>
        public static string PtvdD(DateTime dt)
        {
            return $"D{dt:yyyyMMddHHmmss}~";
        }

        // =================================================================
        //  PTVD Response Parsing
        // =================================================================

        /// <summary>
        /// Parse PTVD fields from response payload string.
        /// Returns array of (type, value) pairs.
        /// </summary>
        public static string[] ParsePtvdFields(string payload)
        {
            var fields = new System.Collections.Generic.List<string>();
            int i = 0;
            while (i < payload.Length)
            {
                char type = payload[i];
                if (type != 'N' && type != 'P' && type != 'H' && type != 'D')
                    break;

                int tilde = payload.IndexOf('~', i + 1);
                if (tilde < 0) break;

                string value = payload.Substring(i + 1, tilde - i - 1);
                fields.Add(value);
                i = tilde + 1;
            }
            return fields.ToArray();
        }

        // =================================================================
        //  SM?DI — Get Identification (diagnostic)
        // =================================================================
        public string GetIdentification()
        {
            return SendCommand("SM?DI", "");
        }

        // =================================================================
        //  SM?GA — Get VK Attributes (verify register exists)
        // =================================================================
        public string GetKeyStatus(string reg)
        {
            int regNum = int.Parse(reg);
            return SendCommand("SM?GA", PtvdN(regNum));
        }

        // =================================================================
        //  SM?VC — Vend STS Credit Token (STS 600-8-6 Section 6.4.2)
        //
        //  Request fields:
        //    N<KeyRegister>~  P<MeterPAN>~  N<TI>~  N<EA>~
        //    N<TCT>~  N<SubClass>~  H<Amount>~  H<TID>~
        //
        //  Response fields:
        //    P<TokenHex17>~  P<TokenDec20>~
        //
        //  NOTE: sgc, krn, ken are VK register attributes in STS6.
        //  Kept in method signature for ComplianceTests.cs compatibility.
        // =================================================================
        public string GenerateCreditToken(string pan, string reg, string sgc,
            string ti, char krn, int ken, string creditType, uint tid, ushort stsAmount)
        {
            int regNum = int.Parse(reg);
            int tiNum = int.Parse(ti);
            int subClass = int.Parse(creditType);

            string payload = PtvdN(regNum) +
                             PtvdP(pan) +
                             PtvdN(tiNum) +
                             PtvdN(EA) +
                             PtvdN(TCT) +
                             PtvdN(subClass) +
                             PtvdH(stsAmount) +
                             PtvdH(tid);

            string response = SendCommand("SM?VC", payload);
            if (response == null) return null;

            // Parse: P<tokenHex17>~ P<tokenDec20>~
            string[] fields = ParsePtvdFields(response);
            if (fields.Length >= 2)
                return fields[1]; // 20-digit decimal token

            LastError = "Unexpected SM!VC response format";
            return null;
        }

        // =================================================================
        //  SM?VM — Vend STS Management Token (STS 600-8-6 Section 6.5.2)
        //
        //  Request fields:
        //    N<KeyRegister>~  P<MeterPAN>~  N<TI>~  N<EA>~
        //    N<TCT>~  N<SubClass>~  H<TransferValue>~  H<TID>~
        //
        //  Response fields:
        //    P<TokenHex17>~  P<TokenDec20>~
        //
        //  SubClass mapping (Table 7):
        //    0=SetMaxPowerLimit, 1=ClearCredit, 2=SetTariffRate,
        //    5=ClearTamper, 6=SetMPUL, 7=SetWMF, 10=Extended
        // =================================================================
        public string GenerateManagementToken(string pan, string reg, string sgc,
            string ti, char krn, int ken, string mgmtType, uint tid, ushort amountOrReg)
        {
            int regNum = int.Parse(reg);
            int tiNum = int.Parse(ti);
            int subClass = int.Parse(mgmtType);

            string payload = PtvdN(regNum) +
                             PtvdP(pan) +
                             PtvdN(tiNum) +
                             PtvdN(EA) +
                             PtvdN(TCT) +
                             PtvdN(subClass) +
                             PtvdH(amountOrReg) +
                             PtvdH(tid);

            string response = SendCommand("SM?VM", payload);
            if (response == null) return null;

            string[] fields = ParsePtvdFields(response);
            if (fields.Length >= 2)
                return fields[1]; // 20-digit decimal token

            LastError = "Unexpected SM!VM response format";
            return null;
        }

        // =================================================================
        //  SM?VK — Vend STS Key Change Tokens (STS 600-8-6 Section 6.5.3)
        //
        //  Request fields:
        //    N<KeyRegisterOld>~  N<KeyRegisterNew>~  P<MeterPAN>~
        //    N<TIOld>~  N<EA>~  N<TCT>~  N<TINew>~  N<NumTokens>~
        //
        //  Response fields:
        //    N<Rollover>~  N<NumTokens>~
        //    P<TokensHex(N×17)>~  P<TokensDec(N×20)>~
        //
        //  EA=07: 2 or 3 tokens; EA=11: 4 tokens
        // =================================================================
        public (string, string) GenerateKeychangeTokens(string pan,
            string oldReg, string newReg,
            string oldSgc, string newSgc,
            string oldTi, string newTi,
            char oldKrn, char newKrn,
            int oldKen, int newKen,
            char rolloverBit)
        {
            int oldRegNum = int.Parse(oldReg);
            int newRegNum = int.Parse(newReg);
            int tiOldNum = int.Parse(oldTi);
            int tiNewNum = int.Parse(newTi);
            int numTokens = (EA == 11) ? 4 : 2;

            string payload = PtvdN(oldRegNum) +
                             PtvdN(newRegNum) +
                             PtvdP(pan) +
                             PtvdN(tiOldNum) +
                             PtvdN(EA) +
                             PtvdN(TCT) +
                             PtvdN(tiNewNum) +
                             PtvdN(numTokens);

            string response = SendCommand("SM?VK", payload);
            if (response == null) return (null, null);

            // Parse: N<rollover>~ N<numTokens>~ P<tokensHex>~ P<tokensDec>~
            string[] fields = ParsePtvdFields(response);
            if (fields.Length >= 4)
            {
                string tokensDec = fields[3];
                if (tokensDec.Length >= 40)
                {
                    return (tokensDec.Substring(0, 20),
                            tokensDec.Substring(20, 20));
                }
            }

            LastError = "Unexpected SM!VK response format";
            return (null, null);
        }

        // SM?VK triplet variant (3 KCTs)
        public (string, string, string) GenerateKeychangeTriplet(string pan,
            string oldReg, string newReg,
            string oldSgc, string newSgc,
            string oldTi, string newTi,
            char oldKrn, char newKrn,
            int oldKen, int newKen,
            char rolloverBit)
        {
            int oldRegNum = int.Parse(oldReg);
            int newRegNum = int.Parse(newReg);
            int tiOldNum = int.Parse(oldTi);
            int tiNewNum = int.Parse(newTi);

            string payload = PtvdN(oldRegNum) +
                             PtvdN(newRegNum) +
                             PtvdP(pan) +
                             PtvdN(tiOldNum) +
                             PtvdN(EA) +
                             PtvdN(TCT) +
                             PtvdN(tiNewNum) +
                             PtvdN(3); // triplet

            string response = SendCommand("SM?VK", payload);
            if (response == null) return (null, null, null);

            string[] fields = ParsePtvdFields(response);
            if (fields.Length >= 4)
            {
                string tokensDec = fields[3];
                if (tokensDec.Length >= 60)
                {
                    return (tokensDec.Substring(0, 20),
                            tokensDec.Substring(20, 20),
                            tokensDec.Substring(40, 20));
                }
            }

            LastError = "Unexpected SM!VK triplet response format";
            return (null, null, null);
        }

        // =================================================================
        //  SM?VT — Verify encrypted STS token (diagnostic)
        // =================================================================
        public string VerifyToken(string pan, string reg, string ti,
            string tokenHex17)
        {
            int regNum = int.Parse(reg);
            int tiNum = int.Parse(ti);

            string payload = PtvdN(regNum) +
                             PtvdP(pan) +
                             PtvdN(tiNum) +
                             PtvdN(EA) +
                             PtvdN(TCT) +
                             PtvdP(tokenHex17);

            return SendCommand("SM?VT", payload);
        }

        // =================================================================
        //  Core Send / Receive
        // =================================================================
        private string SendCommand(string header, string payload)
        {
            // Build: Header + Payload + CRC16(Header+Payload) + CR
            string message = header + payload;
            uint crc = Crc16Arc(Encoding.ASCII.GetBytes(message));
            string request = message + crc.ToString("X4") + "\r";

            LastTx = message + crc.ToString("X4");

            byte[] txBuf = Encoding.ASCII.GetBytes(request);
            byte[] rxBuf = new byte[RSP_BUF_SIZE];
            int rxLen = 0;

            for (int retry = 0; retry < MAX_RETRIES; retry++)
            {
                try
                {
                    _port.DiscardInBuffer();
                    _port.Write(txBuf, 0, txBuf.Length);
                    Thread.Sleep(500);

                    // Read until CR or timeout
                    rxLen = 0;
                    int deadline = Environment.TickCount + 5000;
                    while (Environment.TickCount < deadline)
                    {
                        if (_port.BytesToRead > 0)
                        {
                            int n = _port.Read(rxBuf, rxLen, RSP_BUF_SIZE - rxLen);
                            rxLen += n;
                            // Check for CR terminator
                            for (int i = 0; i < rxLen; i++)
                            {
                                if (rxBuf[i] == 0x0D)
                                {
                                    rxLen = i;
                                    goto gotResponse;
                                }
                            }
                        }
                        Thread.Sleep(50);
                    }

                    if (rxLen == 0) { LastError = "Timeout"; continue; }

                gotResponse:
                    string rxStr = Encoding.ASCII.GetString(rxBuf, 0, rxLen);
                    LastRx = rxStr;

                    if (rxStr.Length < 11)
                    {
                        LastError = $"Response too short ({rxStr.Length})";
                        continue;
                    }

                    // GL!ER — generic error (unrecoverable)
                    if (rxStr.StartsWith("GL!ER"))
                    {
                        string errCode = rxStr.Substring(5, 2);
                        LastError = $"GL!ER{errCode}: {GetErrorDesc(errCode)}";
                        return null;
                    }

                    // Validate response header
                    string expectedHdr = header.Replace('?', '!');
                    if (!rxStr.StartsWith(expectedHdr))
                    {
                        LastError = $"Header mismatch: got {rxStr.Substring(0, Math.Min(5, rxStr.Length))}";
                        continue;
                    }

                    // Status
                    string status = rxStr.Substring(5, 2);

                    if (status == "EE")
                    {
                        string errPayload = rxStr.Substring(7, rxStr.Length - 11);
                        LastError = $"ExtErr: {errPayload}";
                        return null;
                    }

                    if (status != "00")
                    {
                        LastError = $"Error {status}: {GetErrorDesc(status)}";
                        return null;
                    }

                    // Validate CRC: over everything before last 4 chars
                    string body = rxStr.Substring(0, rxStr.Length - 4);
                    string rxCrcStr = rxStr.Substring(rxStr.Length - 4, 4);
                    uint expectedCrc = Crc16Arc(Encoding.ASCII.GetBytes(body));

                    if (!rxCrcStr.Equals(expectedCrc.ToString("X4"), StringComparison.OrdinalIgnoreCase))
                    {
                        LastError = $"CRC error: expect {expectedCrc:X4} got {rxCrcStr}";
                        continue;
                    }

                    // Extract payload (between status and CRC)
                    LastError = "";
                    return body.Substring(7); // skip header(5) + status(2)
                }
                catch (TimeoutException) { LastError = "Timeout"; }
                catch (Exception ex) { LastError = ex.Message; }
            }

            return null;
        }

        // =================================================================
        //  CRC-16/ARC (STS 600-8-6 Section 5.2)
        //  Poly=0x8005, Init=0x0000, RefIn=true, RefOut=true
        //  Verify: "123456789" → 0xBB3D
        // =================================================================
        public static uint Crc16Arc(byte[] data)
        {
            uint crc = 0;
            for (int i = 0; i < data.Length; i++)
            {
                byte k = (byte)(data[i] ^ crc);
                crc = (crc / 256) ^ ((uint)k * 128) ^ ((uint)k * 64);
                if ((BitsSet(k) & 1) != 0)
                    crc ^= 0xC001;
            }
            return crc;
        }

        private static uint BitsSet(byte ch)
        {
            uint n = 0;
            while (ch != 0) { n += (uint)(ch & 1); ch >>= 1; }
            return n;
        }

        // =================================================================
        //  Error Descriptions (Annex E)
        // =================================================================
        private static string GetErrorDesc(string code)
        {
            return code switch
            {
                "20" => "CHECKSUM_ERROR",
                "21" => "INVALID_REQUEST_HEADER",
                "22" => "CSP_RECORD_EMPTY",
                "23" => "PAYLOAD_FORMAT_ERROR",
                "24" => "INVALID_FIELD_VALUE",
                "25" => "LUHN_CHECK_FAILED",
                "26" => "TOKEN_GENERATION_FAILED",
                "27" => "KEK_SLOT_NOT_FOUND",
                "28" => "VK_REGISTER_NOT_FOUND",
                "30" => "KEY_LOAD_STATE_ERROR",
                "31" => "KEY_LOAD_DATA_ERROR",
                "40" => "VENDING_INHIBITED",
                "41" => "KEY_EXPIRED",
                "42" => "TID_RTC_WINDOW_ERROR",
                "43" => "VENDING_LIMIT_EXCEEDED",
                "44" => "KEY_CHANGE_RULE_VIOLATION",
                "50" => "RTC_NOT_SET",
                "98" => "PERMISSION_ERROR",
                _ =>    $"UNKNOWN_{code}"
            };
        }

        // =================================================================
        //  Legacy compatibility stubs
        // =================================================================
        public bool LoadKey(string reg, string key)
        {
            LastError = "SM?LK not available in STS6 — keys pre-loaded via KMC";
            return false;
        }

        public bool ClearKey(string reg)
        {
            LastError = "SM?CK not available in STS6";
            return false;
        }
    }
}
