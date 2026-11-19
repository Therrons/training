namespace fraud_poc_project_buss.Interfaces
{
    // The part of the metrics service that login needs. The full IMetricsService in the UI project implements it.
    public interface IAuthenticationMetrics
    {
        void RecordAuthenticationAttempt(bool successful);
    }
}
