using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_buss.Interfaces
{
    public interface ICustomerHistoryProvider
    {
        // Returns the customer's activity before transactionTime, ignoring the transaction itself.
        // velocityFrom and lookbackFrom are the start of the velocity and lookback windows.
        Task<CustomerHistory> GetHistoryAsync(
            string customerId,
            Guid transactionId,
            DateTime transactionTime,
            DateTime velocityFrom,
            DateTime lookbackFrom);
    }
}
