using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;

namespace fraud_poc_project.Fraud.Swagger
{
    // Adds example values and descriptions to the Swagger/OpenAPI documentation page,
    // so anyone testing the API can see realistic sample data instead of blank fields.
    public class FraudExamplesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses == null || context.ApiDescription == null)
                return;

            var path = context.ApiDescription.RelativePath?.TrimEnd('/')?.ToLowerInvariant();
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();

            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(method))
                return;

            SetQueryParameterExamples(operation);

            if ((path == "api/fraud/events" || path == "api/fraud/events/flagged") && method == "GET")
                SetResponseExample(operation, GetFraudEventsExample());
            else if (method == "GET" && path.StartsWith("api/fraud/events/") && path.EndsWith("/rules"))
                SetResponseExample(operation, GetRuleResultsExample());
        }

        private static void SetQueryParameterExamples(OpenApiOperation operation)
        {
            if (operation.Parameters == null) return;

            foreach (var param in operation.Parameters)
            {
                switch (param.Name?.ToLowerInvariant())
                {
                    case "datefrom":
                        param.Example = new OpenApiString(DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-ddTHH:mm:ss"));
                        param.Description = "Inclusive start date/time for transaction_time.";
                        break;
                    case "dateto":
                        param.Example = new OpenApiString(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"));
                        param.Description = "Inclusive end date/time for transaction_time.";
                        break;
                    case "customerid":
                        param.Example = new OpenApiString("cust-001");
                        param.Description = "Optional customer identifier filter.";
                        break;
                    case "isflaggedonly":
                        param.Example = new OpenApiBoolean(true);
                        param.Description = "When true, returns only flagged events.";
                        break;
                    case "transactiontype":
                        param.Example = new OpenApiString("CNP");
                        param.Description = "Optional transaction type filter (e.g. POS, ATM, EFT, CNP).";
                        break;
                    case "minfraudscore":
                        param.Example = new OpenApiDouble(40);
                        param.Description = "Optional minimum fraud score filter (0-100).";
                        break;
                }
            }
        }

        private static void SetResponseExample(OpenApiOperation operation, OpenApiArray example)
        {
            if (operation.Responses.TryGetValue("200", out var response) && response.Content != null)
            {
                if (response.Content.TryGetValue("application/json", out var mediaType))
                    mediaType.Example = example;
            }
        }

        private static OpenApiArray GetFraudEventsExample() => new()
        {
            new OpenApiObject
            {
                ["id"] = new OpenApiLong(1),
                ["kafkaTopic"] = new OpenApiString("transaction-events"),
                ["kafkaPartition"] = new OpenApiInteger(0),
                ["kafkaOffset"] = new OpenApiLong(1024),
                ["transactionId"] = new OpenApiString(Guid.NewGuid().ToString()),
                ["customerId"] = new OpenApiString("cust-001"),
                ["accountId"] = new OpenApiString("acc-9876"),
                ["amount"] = new OpenApiDouble(75000.00),
                ["currency"] = new OpenApiString("ZAR"),
                ["merchantName"] = new OpenApiString("Unknown Merchant"),
                ["merchantCategory"] = new OpenApiString("gambling"),
                ["transactionType"] = new OpenApiString("CNP"),
                ["channel"] = new OpenApiString("Online"),
                ["countryCode"] = new OpenApiString("NG"),
                ["transactionTime"] = new OpenApiString(DateTime.UtcNow.AddMinutes(-10).ToString("o")),
                ["isFlagged"] = new OpenApiBoolean(true),
                ["fraudScore"] = new OpenApiDouble(75.0),
                ["flaggedReason"] = new OpenApiString("HIGH_AMOUNT, FOREIGN_CNP, HIGH_RISK_MERCHANT"),
                ["timeLogged"] = new OpenApiString(DateTime.UtcNow.AddMinutes(-5).ToString("o"))
            }
        };

        private static OpenApiArray GetRuleResultsExample() => new()
        {
            new OpenApiObject
            {
                ["id"] = new OpenApiLong(1),
                ["fraudEventId"] = new OpenApiLong(1),
                ["ruleCode"] = new OpenApiString("HIGH_AMOUNT"),
                ["ruleDescription"] = new OpenApiString("Transaction amount exceeds 50000.00"),
                ["isTriggered"] = new OpenApiBoolean(true),
                ["scoreContribution"] = new OpenApiDouble(40.0),
                ["evaluatedAt"] = new OpenApiString(DateTime.UtcNow.AddMinutes(-5).ToString("o"))
            },
            new OpenApiObject
            {
                ["id"] = new OpenApiLong(2),
                ["fraudEventId"] = new OpenApiLong(1),
                ["ruleCode"] = new OpenApiString("FOREIGN_CNP"),
                ["ruleDescription"] = new OpenApiString("Card-not-present transaction from a foreign country"),
                ["isTriggered"] = new OpenApiBoolean(true),
                ["scoreContribution"] = new OpenApiDouble(35.0),
                ["evaluatedAt"] = new OpenApiString(DateTime.UtcNow.AddMinutes(-5).ToString("o"))
            }
        };
    }
}
