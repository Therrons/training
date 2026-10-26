namespace fraud_poc_project_buss.Exceptions
{

    public class FraudDetectionException : Exception
    {
        public FraudDetectionException(string message) : base(message) { }

        public FraudDetectionException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
