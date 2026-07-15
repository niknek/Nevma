namespace Nevma.EndToEndTests;

public sealed class LiveInfrastructureFactAttribute : FactAttribute
{
    public LiveInfrastructureFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("NEVMA_RUN_E2E"),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set NEVMA_RUN_E2E=true and start the local backend to run this test.";
        }
    }
}
