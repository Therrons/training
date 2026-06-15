using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;

namespace docke_web_Api.Transactions.Swagger
{
    public class TransactionExamplesOperationFilter : IOperationFilter
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

            if (path == "api/transactions/all" && method == "GET")
            {
                SetResponseExample(operation, GetTransactionListExample());
            }
            else if (path == "api/transactions" && method == "GET")
            {
                SetResponseExample(operation, GetTransactionListExample());
            }
            else if (path == "api/transactions/summary/categories" && method == "GET")
            {
                SetResponseExample(operation, GetCategorySummaryExample());
            }
            else if (path == "api/transactions/summary/customers" && method == "GET")
            {
                SetResponseExample(operation, GetCustomerSummaryExample());
            }
            else if (path == "api/transactions/summary/sources" && method == "GET")
            {
                SetResponseExample(operation, GetSourceSummaryExample());
            }
            else if (path == "api/transactions/summary/daily" && method == "GET")
            {
                SetResponseExample(operation, GetDailySummaryExample());
            }
        }

        private static void SetQueryParameterExamples(OpenApiOperation operation)
        {
            if (operation.Parameters == null)
                return;

            foreach (var parameter in operation.Parameters)
            {
                switch (parameter.Name?.ToLowerInvariant())
                {
                    case "customerid":
                        parameter.Example = new OpenApiString("cust-001");
                        parameter.Description = "Filter by customer identifier.";
                        break;
                    case "category":
                        parameter.Example = new OpenApiString("Groceries");
                        parameter.Description = "Optional transaction category filter.";
                        break;
                    case "source":
                        parameter.Example = new OpenApiString("CreditCard");
                        parameter.Description = "Optional transaction source type filter.";
                        break;
                    case "frompostedat":
                        parameter.Example = new OpenApiString(DateTime.UtcNow.AddDays(-10).ToString("yyyy-MM-dd"));
                        parameter.Description = "Inclusive start date for transaction posting date.";
                        break;
                    case "topostedat":
                        parameter.Example = new OpenApiString(DateTime.UtcNow.ToString("yyyy-MM-dd"));
                        parameter.Description = "Inclusive end date for transaction posting date.";
                        break;
                    case "minimumamount":
                        parameter.Example = new OpenApiString("-500");
                        parameter.Description = "Optional minimum transaction amount.";
                        break;
                    case "maximumamount":
                        parameter.Example = new OpenApiString("1000");
                        parameter.Description = "Optional maximum transaction amount.";
                        break;
                }
            }
        }

        private static void SetResponseExample(OpenApiOperation operation, OpenApiArray exampleArray)
        {
            if (operation.Responses.TryGetValue("200", out var response) && response.Content != null)
            {
                if (response.Content.TryGetValue("application/json", out var mediaType))
                {
                    mediaType.Example = exampleArray;
                }
            }
        }

        private static OpenApiArray GetTransactionListExample()
        {
            return new OpenApiArray
            {
                new OpenApiObject
                {
                    ["id"] = new OpenApiString(Guid.NewGuid().ToString()),
                    ["customerId"] = new OpenApiString("cust-001"),
                    ["source"] = new OpenApiString("Bank"),
                    ["postedAt"] = new OpenApiString(DateTime.UtcNow.AddDays(-1).ToString("o")),
                    ["amount"] = new OpenApiDouble(-220.45),
                    ["currency"] = new OpenApiString("ZAR"),
                    ["merchant"] = new OpenApiString("Spar Supermarket"),
                    ["description"] = new OpenApiString("Groceries purchase"),
                    ["category"] = new OpenApiString("Groceries"),
                    ["rawType"] = new OpenApiString("POS")
                },
                new OpenApiObject
                {
                    ["id"] = new OpenApiString(Guid.NewGuid().ToString()),
                    ["customerId"] = new OpenApiString("cust-002"),
                    ["source"] = new OpenApiString("DigitalWallet"),
                    ["postedAt"] = new OpenApiString(DateTime.UtcNow.AddDays(-4).ToString("o")),
                    ["amount"] = new OpenApiDouble(-140.00),
                    ["currency"] = new OpenApiString("ZAR"),
                    ["merchant"] = new OpenApiString("Netflix"),
                    ["description"] = new OpenApiString("Streaming subscription"),
                    ["category"] = new OpenApiString("Entertainment"),
                    ["rawType"] = new OpenApiString("WalletPayment")
                }
            };
        }

        private static OpenApiArray GetCategorySummaryExample()
        {
            return new OpenApiArray
            {
                new OpenApiObject
                {
                    ["category"] = new OpenApiString("Income"),
                    ["transactionCount"] = new OpenApiInteger(1),
                    ["totalAmount"] = new OpenApiDouble(15000.00)
                },
                new OpenApiObject
                {
                    ["category"] = new OpenApiString("Groceries"),
                    ["transactionCount"] = new OpenApiInteger(2),
                    ["totalAmount"] = new OpenApiDouble(-1200.45)
                }
            };
        }

        private static OpenApiArray GetCustomerSummaryExample()
        {
            return new OpenApiArray
            {
                new OpenApiObject
                {
                    ["customerId"] = new OpenApiString("cust-001"),
                    ["transactionCount"] = new OpenApiInteger(3),
                    ["totalAmount"] = new OpenApiDouble(-1870.45)
                },
                new OpenApiObject
                {
                    ["customerId"] = new OpenApiString("cust-002"),
                    ["transactionCount"] = new OpenApiInteger(2),
                    ["totalAmount"] = new OpenApiDouble(14860.00)
                }
            };
        }

        private static OpenApiArray GetSourceSummaryExample()
        {
            return new OpenApiArray
            {
                new OpenApiObject
                {
                    ["source"] = new OpenApiString("Bank"),
                    ["transactionCount"] = new OpenApiInteger(3),
                    ["totalAmount"] = new OpenApiDouble(1379.55)
                },
                new OpenApiObject
                {
                    ["source"] = new OpenApiString("DigitalWallet"),
                    ["transactionCount"] = new OpenApiInteger(2),
                    ["totalAmount"] = new OpenApiDouble(-440.00)
                }
            };
        }

        private static OpenApiArray GetDailySummaryExample()
        {
            return new OpenApiArray
            {
                new OpenApiObject
                {
                    ["day"] = new OpenApiString(DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")),
                    ["transactionCount"] = new OpenApiInteger(1),
                    ["totalAmount"] = new OpenApiDouble(-220.45)
                },
                new OpenApiObject
                {
                    ["day"] = new OpenApiString(DateTime.UtcNow.AddDays(-4).ToString("yyyy-MM-dd")),
                    ["transactionCount"] = new OpenApiInteger(1),
                    ["totalAmount"] = new OpenApiDouble(-140.00)
                }
            };
        }
    }
}
