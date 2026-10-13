using fraud_poc_project_buss.Exceptions;
using System;

namespace fraud_poc_project.Exceptions
{
    /// <summary>
    /// Thrown when authentication/JWT operations fail
    /// </summary>
    public class AuthenticationException : FraudDetectionException
    {
        public AuthenticationException(string message) : base(message) { }

        public AuthenticationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
