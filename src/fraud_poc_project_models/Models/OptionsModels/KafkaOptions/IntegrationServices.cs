namespace OptionsModels.KafkaOptions;

public class IntegrationServices
{
    public ClientDomainConfig ClientDomain { get; set; }
    public ClientEngagementConfig ClientEngagement { get; set; }
}

public class ClientDomainConfig
{
    public const string Name = "ClientDomain";
    public string ClientDomainBaseURL { get; set; }
    public string ResourceEndpoint { get; set; }
}

public class ClientEngagementConfig
{
    public const string Name = "ClientEngagement";
    public string ClientEngagementBaseURL { get; set; }
    public string ResourceEndpoint { get; set; }
}