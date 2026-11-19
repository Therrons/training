namespace fraud_poc_project_buss.Dto
{
    // A customer's recent activity, taken from previously evaluated transactions.
    // The current transaction is never included in these figures.
    public record CustomerHistory(
        int RecentCount,      // transactions inside the velocity window
        int BaselineCount,    // transactions inside the lookback window
        decimal AverageAmount // average absolute amount inside the lookback window
    );
}
