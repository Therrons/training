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

        public string? CustomerId { get; set; }

        public bool? IsFlaggedOnly { get; set; }

        public string? TransactionType { get; set; }

        public decimal? MinFraudScore { get; set; }
    }
}
