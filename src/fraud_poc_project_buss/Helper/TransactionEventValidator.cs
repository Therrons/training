using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_buss.Helper
{
    
    // Structural validation for inbound transaction events, applied before fraud evaluation.
    // Events that fail validation are dead-lettered rather than scored or persisted.
    
    public static class TransactionEventValidator
    {
        // Largest amount the fraud_event.amount column (NUMERIC(18,2)) can hold, with headroom.
        private const decimal MaxAbsoluteAmount = 1_000_000_000_000m;

        // Transaction types the rules understand. Extend here if new categories are onboarded.
        private static readonly HashSet<string> AllowedTransactionTypes =
            new(StringComparer.OrdinalIgnoreCase) { "POS", "ATM", "EFT", "CNP" };

        // Tolerance for clock skew between the producer and this service.
        private static readonly TimeSpan FutureTimeTolerance = TimeSpan.FromMinutes(5);

        
        // Treats a timestamp with no offset (Kind = Unspecified) as UTC. Npgsql rejects Unspecified
        // values for timestamp with time zone columns, so this must run before persisting.
        
        public static void NormalizeTransactionTime(TransactionEvent transactionEvent)
        {
            if (transactionEvent.TransactionTime.Kind == DateTimeKind.Unspecified)
            {
                transactionEvent.TransactionTime = DateTime.SpecifyKind(transactionEvent.TransactionTime, DateTimeKind.Utc);
            }
        }

        
        // Returns every validation failure found. An empty list means the event is valid.
        
        public static IReadOnlyList<string> Validate(TransactionEvent e, DateTime? utcNow = null)
        {
            var errors = new List<string>();

            if (e.TransactionId == Guid.Empty)
                errors.Add("TransactionId is required.");

            if (string.IsNullOrWhiteSpace(e.CustomerId))
                errors.Add("CustomerId is required.");
            else if (e.CustomerId.Length > 100)
                errors.Add("CustomerId must be 100 characters or fewer.");

            if (string.IsNullOrWhiteSpace(e.AccountId))
                errors.Add("AccountId is required.");
            else if (e.AccountId.Length > 100)
                errors.Add("AccountId must be 100 characters or fewer.");

            if (e.Amount == 0m)
                errors.Add("Amount must be non-zero.");
            else if (Math.Abs(e.Amount) > MaxAbsoluteAmount)
                errors.Add($"Amount must be within ±{MaxAbsoluteAmount:N0}.");

            if (string.IsNullOrWhiteSpace(e.Currency) || !System.Text.RegularExpressions.Regex.IsMatch(e.Currency, "^[A-Z]{3}$"))
                errors.Add("Currency must be a 3-letter uppercase ISO code.");

            if (string.IsNullOrWhiteSpace(e.TransactionType))
                errors.Add("TransactionType is required.");
            else if (!AllowedTransactionTypes.Contains(e.TransactionType))
                errors.Add($"TransactionType '{e.TransactionType}' is not supported.");

            if (e.Channel?.Length > 50)
                errors.Add("Channel must be 50 characters or fewer.");

            if (e.CountryCode?.Length > 10)
                errors.Add("CountryCode must be 10 characters or fewer.");

            if (e.MerchantCategory?.Length > 100)
                errors.Add("MerchantCategory must be 100 characters or fewer.");

            if (e.MerchantName?.Length > 250)
                errors.Add("MerchantName must be 250 characters or fewer.");

            if (e.TransactionTime == default)
                errors.Add("TransactionTime is required.");
            else
            {
                var now = utcNow ?? DateTime.UtcNow;
                var asUtc = e.TransactionTime.Kind == DateTimeKind.Local ? e.TransactionTime.ToUniversalTime() : DateTime.SpecifyKind(e.TransactionTime, DateTimeKind.Utc);
                if (asUtc > now.Add(FutureTimeTolerance))
                    errors.Add("TransactionTime cannot be in the future.");
            }

            return errors;
        }
    }
}
