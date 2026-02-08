// =============================================================================
//  TestResultsStore.cs — Shared storage for test results
//  Allows SignalR hub and REST API to access the same results
// =============================================================================

namespace STSCompliancePOS.Services;

public class TestResultsStore
{
    private readonly object _lock = new();
    
    public FullTestSuiteResult? LastSuiteResult { get; private set; }
    public TestRunResult? LastTestResult { get; private set; }
    public DateTime? LastRunTime { get; private set; }

    public void StoreSuiteResult(FullTestSuiteResult result)
    {
        lock (_lock)
        {
            LastSuiteResult = result;
            LastRunTime = DateTime.UtcNow;
        }
    }

    public void StoreTestResult(TestRunResult result)
    {
        lock (_lock)
        {
            LastTestResult = result;
            LastRunTime = DateTime.UtcNow;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            LastSuiteResult = null;
            LastTestResult = null;
            LastRunTime = null;
        }
    }

    public bool HasSuiteResults => LastSuiteResult != null;
    public bool HasTestResults => LastTestResult != null;
}
