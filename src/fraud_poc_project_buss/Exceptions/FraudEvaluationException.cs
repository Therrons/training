namespace fraud_poc_project_buss.Exceptions
{
    public class FraudEvaluationException : FraudDetectionException
    {
        public FraudEvaluationException(string message) : base(message) { }

        public FraudEvaluationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
