namespace OptionsModels.KafkaOptions.ConsumerOptions;

public class DynamicLoanCommunicationJourneyOptions
{
    public const string Section = "DynamicLoanCommunicationJourneyOptions";

    public required string OfferAwarenessJourney { get; set; }

    public required string OfferOnboardingJourney { get; set; }

    public required string OfferTrackingJourney { get; set; }

    public required string ExecutionMode { get; set; }
}

/// <summary>
/// This indicator is used by the communication processor to determine the mode. This will help ensure communication is not sent to clients in NON-PROD environment(s).
/// </summary>
public static class ExecutionMode
{
    public const string TESTER = "tester";
    public const string DEVELOPER = "developer";
    public const string PRODUCTION = "production";
}