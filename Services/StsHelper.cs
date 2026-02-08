// =============================================================================
//  StsHelper.cs — STS utility functions
//  TID calculation (configurable base date), STS amount encoding, Luhn check
// =============================================================================

using System;

namespace STSCompliancePOS.Services
{
    public static class StsHelper
    {
        // =====================================================================
        //  TID Calculation — minutes since BaseDate-01-01 00:00
        //  Exact port from sts_common.c with configurable base year
        // =====================================================================
        public static uint CalcTid(int year, int month, int day, int hour, int min, int baseYear)
        {
            int tmYear = year - 1900;
            int tmMonth = month - 1;
            int baseOffset = baseYear - 1900;
            int y = tmYear - baseOffset;
            int d = day - 1;

            uint days = 0;
            int yr = y;
            while (yr > 0)
            {
                days += 365;
                if (((yr + baseYear) % 4) == 0) days++;
                yr--;
            }

            int mo = tmMonth;
            while (mo > 0)
            {
                days += 30;
                if (mo == 2) days -= 2;
                else if (mo == 1 || mo == 3 || mo == 5 || mo == 7 ||
                         mo == 8 || mo == 10 || mo == 12) days++;
                mo--;
            }

            days += (uint)d;
            uint minutes = days * 24 * 60 + (uint)(hour * 60) + (uint)min;

            // REQ.6.3.5.2: Special Reserved TID
            if (min == 1 && hour == 0)
                minutes++;

            return minutes;
        }

        // =====================================================================
        //  STS Amount Encoding (0.1 unit resolution)
        //  Input: value in 0.1 units (e.g., 25.6 kWh = 256)
        // =====================================================================
        public static ushort EncodeAmount(uint units)
        {
            ushort m, e;
            if (units <= 16383)           { m = (ushort)units; e = 0; }
            else if (units <= 180223)     { m = (ushort)((units - 0x4000) / 10); e = 1; }
            else if (units <= 1818623)    { m = (ushort)((units - 0x4000 * 11) / 100); e = 2; }
            else if (units <= 18201624)   { m = (ushort)((units - 0x4000 * 111) / 1000); e = 3; }
            else                          { m = 0x3FFF; e = 3; }
            return (ushort)((e << 14) | (m & 0x3FFF));
        }

        public static uint DecodeAmount(ushort encoded)
        {
            uint t = 0;
            ushort e = (ushort)((encoded >> 14) & 0x03);
            ushort m = (ushort)(encoded & 0x3FFF);
            ushort temp = 1;
            for (ushort i = e; i > 0; i--) { t += (uint)(0x4000 * temp); temp *= 10; }
            t += (uint)(temp * m);
            return t;
        }

        public static uint RoundAmount(uint amount)
        {
            if (amount == 0) return 0;
            uint r = amount - 1;
            do { r++; } while (DecodeAmount(EncodeAmount(r)) < amount);
            return r;
        }

        // =====================================================================
        //  Luhn Check Digit Validation
        // =====================================================================
        public static bool ValidateLuhn(string number)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = number.Length - 1; i >= 0; i--)
            {
                int n = number[i] - '0';
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }
            return (sum % 10) == 0;
        }

        // =====================================================================
        //  Token formatting: "12345678901234567890" → "1234 5678 9012 3456 7890"
        // =====================================================================
        public static string FormatToken(string token)
        {
            if (token == null || token.Length != 20) return token ?? "(null)";
            return $"{token.Substring(0, 4)} {token.Substring(4, 4)} {token.Substring(8, 4)} {token.Substring(12, 4)} {token.Substring(16, 4)}";
        }

        // Normalize expected token: remove spaces
        public static string NormalizeToken(string token)
        {
            return token?.Replace(" ", "") ?? "";
        }

        // =====================================================================
        //  Credit Token SubClass for SM?VC (IEC 62055-41 Table 14)
        // =====================================================================
        public const string CT_ELECTRICITY = "0";   // SubClass 0: Electricity (kWh)
        public const string CT_WATER       = "1";   // SubClass 1: Water (kL)
        public const string CT_GAS         = "2";   // SubClass 2: Gas (m³)
        public const string CT_TIME        = "3";   // SubClass 3: Time (hours)
        public const string CT_ELEC_CURR   = "4";   // SubClass 4: Currency (electricity)
        public const string CT_WATER_CURR  = "5";   // SubClass 5: Currency (water)
        public const string CT_GAS_CURR    = "6";   // SubClass 6: Currency (gas)
        public const string CT_TIME_CURR   = "7";   // SubClass 7: Currency (time)

        // =====================================================================
        //  Management Token SubClass for SM?VM (STS 600-8-6 Table 7)
        // =====================================================================
        public const string MT_SET_MAX_POWER    = "0";   // SubClass 0: SetMaximumPowerLimit
        public const string MT_CLEAR_CREDIT     = "1";   // SubClass 1: ClearCredit
        public const string MT_SET_TARIFF       = "2";   // SubClass 2: SetTariffRate
        public const string MT_CLEAR_TAMPER     = "5";   // SubClass 5: ClearTamperCondition
        public const string MT_SET_MPUL         = "6";   // SubClass 6: SetMaximumPhasePowerUnbalanceLimit
        public const string MT_SET_WMF          = "7";   // SubClass 7: SetWaterMeterFactor
    }
}
