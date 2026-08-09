using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using fraud_poc_project_buss.Models.Database;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace fraud_poc_project.Configuration
{
    public class AWSSecretsConfiguration
    {
        private readonly IAmazonSecretsManager _secretsManager;

        public AWSSecretsConfiguration(IAmazonSecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }

        public async Task<DB_Keys> GetAWSSecretAsync(string secretName)
        {
            try
            {
                var response = await _secretsManager.GetSecretValueAsync(
                    new GetSecretValueRequest { SecretId = secretName });

                if (string.IsNullOrEmpty(response.SecretString))
                    throw new InvalidOperationException($"Secret '{secretName}' is empty.");

                return JsonConvert.DeserializeObject<DB_Keys>(response.SecretString)
                    ?? throw new InvalidOperationException($"Failed to deserialise secret '{secretName}'.");
            }
            catch (AmazonSecretsManagerException e)
            {
                Console.WriteLine($"Error retrieving DB_Keys from Secrets Manager: {e.Message}");
                throw;
            }
        }
    }
}
