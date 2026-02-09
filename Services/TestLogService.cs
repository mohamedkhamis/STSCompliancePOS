using Microsoft.Data.Sqlite;

namespace STSCompliancePOS.Services;

public class TestLogService : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestLogService()
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
        Directory.CreateDirectory(dataDir);

        var dbPath = Path.Combine(dataDir, "testlog.db");
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS TestLog (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Timestamp TEXT NOT NULL,
                TestVectorId TEXT,
                EA INTEGER NOT NULL,
                Category TEXT NOT NULL,
                PAN TEXT NOT NULL,
                KeyRegister TEXT,
                TI TEXT,
                CreditType TEXT,
                Amount REAL,
                MgmtType TEXT,
                MgmtValue INTEGER,
                IssueDate TEXT,
                BaseDate INTEGER,
                ExpectedToken TEXT,
                GeneratedToken TEXT,
                Passed INTEGER,
                Error TEXT
            );";
        cmd.ExecuteNonQuery();
    }

    public void LogTokenTest(string? testVectorId, int ea, string category, string pan,
        string? reg, string? ti, string? creditType, double? amount,
        string? mgmtType, int? mgmtValue, string? issueDate, int? baseDate,
        string? expectedToken, string? generatedToken, bool passed, string? error = null)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO TestLog (Timestamp, TestVectorId, EA, Category, PAN, KeyRegister, TI,
                CreditType, Amount, MgmtType, MgmtValue, IssueDate, BaseDate,
                ExpectedToken, GeneratedToken, Passed, Error)
            VALUES ($ts, $tvId, $ea, $cat, $pan, $reg, $ti,
                $ct, $amt, $mt, $mv, $isd, $bd,
                $exp, $gen, $pass, $err);";

        cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("o"));
        cmd.Parameters.AddWithValue("$tvId", (object?)testVectorId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ea", ea);
        cmd.Parameters.AddWithValue("$cat", category);
        cmd.Parameters.AddWithValue("$pan", pan);
        cmd.Parameters.AddWithValue("$reg", (object?)reg ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ti", (object?)ti ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ct", (object?)creditType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$amt", (object?)amount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$mt", (object?)mgmtType ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$mv", (object?)mgmtValue ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$isd", (object?)issueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$bd", (object?)baseDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$exp", (object?)expectedToken ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$gen", (object?)generatedToken ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pass", passed ? 1 : 0);
        cmd.Parameters.AddWithValue("$err", (object?)error ?? DBNull.Value);

        cmd.ExecuteNonQuery();
    }

    public List<Dictionary<string, object?>> GetRecentLogs(int count = 50)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM TestLog ORDER BY Id DESC LIMIT $count";
        cmd.Parameters.AddWithValue("$count", count);
        return ReadResults(cmd);
    }

    public List<Dictionary<string, object?>> GetAllLogs()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM TestLog ORDER BY Id DESC";
        return ReadResults(cmd);
    }

    private static List<Dictionary<string, object?>> ReadResults(SqliteCommand cmd)
    {
        var results = new List<Dictionary<string, object?>>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }
            results.Add(row);
        }
        return results;
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }
}
