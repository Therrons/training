using fraud_poc_project_buss.Exceptions;
using System;

namespace fraud_poc_project_repo.Exceptions
{
    /// <summary>
    /// Thrown when database operations fail
    /// </summary>
    public class FraudRepositoryException : FraudDetectionException
    {
        public FraudRepositoryException(string message) : base(message) { }

        public FraudRepositoryException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
