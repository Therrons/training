namespace fraud_poc_project_buss.Exceptions
{
    /// <summary>
    /// Base exception for all fraud detection related errors
    /// </summary>
    public class FraudDetectionException : Exception
    {
        public FraudDetectionException(string message) : base(message) { }

        public FraudDetectionException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
