using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using docke_web_Api_models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace docke_web_Api.Configuration
{
    public class SecretsConfiguration
    {
        //private IAmazonSecretsManager _secretsManager;
        //public SecretsConfiguration(AmazonSecretsManager secretsManager)
        //{
        //    _secretsManager = secretsManager;
        //}


        //public async Task<DB_Keys> GetSecretAsync(string secretName)
        //{
        //    try
        //    {
        //        _secretsManager.Config. = RegionEndpoint.USEast1; // Set your region
        //        var response = await _secretsManager.GetSecretValueAsync(
        //               new GetSecretValueRequest
        //               {
        //                   SecretId = secretName,

        //               });

        //        if (string.IsNullOrEmpty(response.SecretString))
        //            throw new InvalidOperationException("Secret is empty");
        //        return JsonConvert.DeserializeObject<DB_Keys>(response.SecretString);
        //    }
        //    catch (AmazonSecretsManagerException e)
        //    {
        //        // Handle common exceptions like ResourceNotFound or AccessDenied
        //        Console.WriteLine($"Error retrieving DB Keys: {e.Message}");
        //        throw;
        //    }
        //}
















        private IAmazonSecretsManager _secretsManager;
        public SecretsConfiguration(IAmazonSecretsManager secretsManager)
        {
            _secretsManager = secretsManager;
        }


        public async Task<DB_Keys> GetSecretAsync(string secretName)
        {
            try
            {
                var response = await _secretsManager.GetSecretValueAsync(
                       new GetSecretValueRequest
                       {
                           SecretId = secretName,

                       });

                if (string.IsNullOrEmpty(response.SecretString))
                    throw new InvalidOperationException("Secret is empty");
                return JsonConvert.DeserializeObject<DB_Keys>(response.SecretString);
            }
            catch (AmazonSecretsManagerException e)
            {
                // Handle common exceptions like ResourceNotFound or AccessDenied
                Console.WriteLine($"Error retrieving DB Keys: {e.Message}");
                throw;
            }
        }

        //public static void ConfigureSecrets(this IServiceCollection services, out string dbKeys, WebApplicationBuilder builder)
        //{
        //    dbKeys = string.Empty;

        //    if (builder.Environment.EnvironmentName.Equals("LOC", StringComparison.InvariantCultureIgnoreCase))
        //    {
        //        builder.Configuration.AddEnvironmentVariables();
        //    }

        //    dbKeys = GetSecret("docke_web_api_k8s", "us-east-1");

        //var folderPath = builder.Configuration.GetValue<string>("SecretPath")
        //    ?? throw new ApplicationException("Mandatory config for SecretPath is missing.");


        //Log.Information($"The secrets folder path is {folderPath}");

        //if (!Directory.Exists(folderPath))
        //{
        //    Log.Warning($"The directory {folderPath} does not exist");

        //    return;
        //}

        //foreach (var file in Directory.EnumerateFiles(folderPath))
        //{
        //    var contents = File.ReadAllText(file).Trim();
        //    var fileName = file.Split("/").Last().Trim();
        //    builder.Configuration[fileName] = contents;
        //}
        //}


        //private static string GetSecret(string secretName, string region)
        //{
        //    // The parameterless constructor uses the default credentials provider chain,
        //    // which will automatically detect and use your IAM role credentials.
        //    using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

        //    var request = new GetSecretValueRequest
        //    {
        //        SecretId = secretName,
        //        VersionStage = "AWSCURRENT" // Optional: Defaults to the latest version
        //    };

        //    try
        //    {
        //        var result = client.GetSecretValueAsync(request).Result;
        //        return result.SecretString;
        //    }
        //    catch (AmazonSecretsManagerException e)
        //    {
        //        // Handle common exceptions like ResourceNotFound or AccessDenied
        //        Console.WriteLine($"Error retrieving DB Keys: {e.Message}");
        //        throw;
        //    }
        //}
    }
}
