using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Dto
{
    public record FraudQueryDto
    {
        /// <summary>
        /// The starting date and time for the query range. Must be in the format yyyy-MM-dd HH:mm:ss.
        /// </summary>
        // Enforces strict yyyy-MM-dd HH:mm:ss format
        [RegularExpression(
        @"^([0-9]{4})-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01]) ([01][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9])$",
        ErrorMessage = "Date From must match the format yyyy-MM-dd HH:mm:ss.")]
        public string DateFrom { get; set; }

        /// <summary>
        /// The ending date and time for the query range. Must be in the format yyyy-MM-dd HH:mm:ss.
        /// </summary>
        // Enforces strict yyyy-MM-dd HH:mm:ss format
        [RegularExpression(
        @"^([0-9]{4})-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01]) ([01][0-9]|2[0-3]):([0-5][0-9]):([0-5][0-9])$",
        ErrorMessage = "Date To must match the format yyyy-MM-dd HH:mm:ss.")]
        public string DateTo { get; set; }

        /// <summary>
        /// Optional filter to return only events for a specific customer ID. If provided, only events associated with this customer ID will be returned.
        /// </summary>
        public string? CustomerId { get; set; }

        /// <summary>
        /// Optional filter to return only flagged events. If true, only events that have been flagged as potentially fraudulent will be returned. If false or null, all events will be returned regardless of their flagged status.
        /// </summary>
        public bool? IsFlaggedOnly { get; set; }

        /// <summary>
        /// Optional transaction type filter. If provided, only events with this transaction type will be returned.
        /// </summary>
        public string? TransactionType { get; set; }

        /// <summary>
        /// Optional minimum fraud score filter. If provided, only events with a fraud score greater than or equal to this value will be returned.
        /// </summary>
        public decimal? MinFraudScore { get; set; }
    }
}
