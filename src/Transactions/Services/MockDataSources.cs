using docke_web_Api.Transactions.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace docke_web_Api.Transactions.Services
{
    public interface ITransactionDataSource
    {
        IEnumerable<TransactionRecord> GetTransactions();
    }

    public class BankTransactionSource : ITransactionDataSource
    {
        public IEnumerable<TransactionRecord> GetTransactions()
        {
            return new[]
            {
                new TransactionRecord
                {
                    Id = Guid.NewGuid(),
                    CustomerId = "cust-001",
                    Source = TransactionSourceType.Bank,
                    PostedAt = DateTime.UtcNow.AddDays(-1),
                    Amount = -220.45m,
                    Currency = "ZAR",
                    Merchant = "Spar Supermarket",
                    Description = "Groceries purchase",
                    RawType = "POS",
                    Category = TransactionCategory.Groceries
                },
                new TransactionRecord
                {
                    Id = Guid.NewGuid(),
                    CustomerId = "cust-002",
                    Source = TransactionSourceType.Bank,
                    PostedAt = DateTime.UtcNow.AddDays(-3),
                    Amount = 15000.00m,
                    Currency = "ZAR",
                    Merchant = "Salary Credit",
                    Description = "Monthly salary",
                    RawType = "Credit",
                    Category = TransactionCategory.Income
                },
                new TransactionRecord
                {
                    Id = Guid.NewGuid(),
                    CustomerId = "cust-001",
                    Source = TransactionSourceType.Bank,
                    PostedAt = DateTime.UtcNow.AddDays(-7),
                    Amount = -1200.00m,
                    Currency = "ZAR",
                    Merchant = "Acme Utilities",
                    Description = "Electricity and water bill",
                    RawType = "Debit",
                    Category = TransactionCategory.Utilities
                }
            };
        }
    }

    public class CreditCardTransactionSource : ITransactionDataSource
    {
        public IEnumerable<TransactionRecord> GetTransactions()
        {
            return new[]
            {
                new TransactionRecord
                {
                    Id = Guid.NewGuid(),
                    CustomerId = "cust-001",
                    Source = TransactionSourceType.CreditCard,
                    PostedAt = DateTime.UtcNow.AddDays(-2),
                    Amount = -450.00m,
                    Currency = "ZAR",
                    Merchant = "Uber Eats",
                    Description = "Dinner order",
                    RawType = "Swipe",
                    Category = TransactionCategory.Dining
                },
                new TransactionRecord
                {
                    Id = Guid.NewGuid(),
                    CustomerId = "cust-003",
                    Source = TransactionSourceType.CreditCard,
                    PostedAt = DateTime.UtcNow.AddDays(-10),
                    Amount = -980.00m,
                    Currency = "ZAR",
                    Merchant = "FOODLION",
                    Description = "Grocery run",
                    RawType = "Contactless",
                    Category = TransactionCategory.Groceries
                }
            };
        }
    }

    public class DigitalWalletTransactionSource : ITransactionDataSource
    {
        public IEnumerable<TransactionRecord> GetTransactions()
        {
            return new[]
            {
                new TransactionRecord
                {
                    Id = Guid.NewGuid(),
                    CustomerId = "cust-002",
                    Source = TransactionSourceType.DigitalWallet,
                    PostedAt = DateTime.UtcNow.AddDays(-4),
                    Amount = -140.00m,
                    Currency = "ZAR",
                    Merchant = "Netflix",
                    Description = "Streaming subscription",
                    RawType = "WalletPayment",
                    Category = TransactionCategory.Entertainment
                },
                new TransactionRecord
                {
                    Id = Guid.NewGuid(),
                    CustomerId = "cust-003",
                    Source = TransactionSourceType.DigitalWallet,
                    PostedAt = DateTime.UtcNow.AddDays(-5),
                    Amount = -300.00m,
                    Currency = "ZAR",
                    Merchant = "Clicks Pharmacy",
                    Description = "Medicines and health products",
                    RawType = "WalletTap",
                    Category = TransactionCategory.Healthcare
                }
            };
        }
    }
}
