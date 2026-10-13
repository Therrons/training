namespace fraud_poc_project_buss.Exceptions
{
    /// <summary>
    /// Thrown when fraud rule evaluation fails
    /// </summary>
    public class FraudEvaluationException : FraudDetectionException
    {
        public FraudEvaluationException(string message) : base(message) { }

        public FraudEvaluationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
