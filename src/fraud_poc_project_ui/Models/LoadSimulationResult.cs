namespace fraud_poc_project.Models
{
    // A summary of how a load-simulation run went (see LoadSimulatorController),
    // returned to whoever triggered the test.
    public class LoadSimulationResult
    {
        public int Requested { get; init; }
        public int Produced { get; init; }
        public int Failed { get; init; }
        public string Topic { get; init; } = string.Empty;
    }
}
