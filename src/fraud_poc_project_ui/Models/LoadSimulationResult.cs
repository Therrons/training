namespace fraud_poc_project.Models
{
    public class LoadSimulationResult
    {
        public int Requested { get; init; }
        public int Produced { get; init; }
        public int Failed { get; init; }
        public string Topic { get; init; } = string.Empty;
    }
}
