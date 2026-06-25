namespace OptionsModels.KafkaOptions.ConsumerOptions;

public class DynamicLoanCommunicationAppOptions
{
    public const string Section = "DynamicLoanCommunicationAppOptions";

    public required string WorkflowTenantId { get; set; }

    public required string ApplicationClientId { get; set; }

    public required string ClientSecret { get; set; }
    public required string WorkFlow_GrantType { get; set; }
}