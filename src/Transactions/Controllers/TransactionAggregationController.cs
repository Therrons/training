using docke_web_Api.Transactions.Dto;
using docke_web_Api.Transactions.Mapping;
using docke_web_Api.Transactions.Models;
using docke_web_Api.Transactions.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace docke_web_Api.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionAggregationController : ControllerBase
    {
        private readonly ITransactionAggregator _aggregator;

        public TransactionAggregationController(ITransactionAggregator aggregator)
        {
            _aggregator = aggregator;
        }

        [HttpGet("all")]
        public ActionResult<IEnumerable<TransactionDto>> GetAllTransactions()
        {
            return Ok(_aggregator.GetAllTransactions().ToDto());
        }

        [HttpGet]
        public ActionResult<IEnumerable<TransactionDto>> QueryTransactions([FromQuery] TransactionQueryDto query)
        {
            if (query == null)
            {
                return BadRequest("Query parameters are required.");
            }

            var model = query.ToModel();
            return Ok(_aggregator.GetTransactions(model).ToDto());
        }

        [HttpGet("summary/categories")]
        public ActionResult<IEnumerable<TransactionCategorySummaryDto>> GetCategorySummaries([FromQuery] TransactionQueryDto? query)
        {
            var model = query?.ToModel();
            return Ok(_aggregator.GetCategorySummaries(model).ToDto());
        }

        [HttpGet("summary/customers")]
        public ActionResult<IEnumerable<TransactionCustomerSummaryDto>> GetCustomerSummaries([FromQuery] TransactionQueryDto? query)
        {
            var model = query?.ToModel();
            return Ok(_aggregator.GetCustomerSummaries(model).ToDto());
        }

        [HttpGet("summary/sources")]
        public ActionResult<IEnumerable<TransactionSourceSummaryDto>> GetSourceSummaries([FromQuery] TransactionQueryDto? query)
        {
            var model = query?.ToModel();
            return Ok(_aggregator.GetSourceSummaries(model).ToDto());
        }

        [HttpGet("summary/daily")]
        public ActionResult<IEnumerable<TransactionDailySummaryDto>> GetDailySummaries([FromQuery] TransactionQueryDto? query)
        {
            var model = query?.ToModel();
            return Ok(_aggregator.GetDailySummaries(model).ToDto());
        }
    }
}
