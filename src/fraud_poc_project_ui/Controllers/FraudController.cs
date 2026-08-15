using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fraud_poc_project.Controllers
{
    // The read-only API for looking up fraud results that have already been saved to
    // the database. This doesn't evaluate anything itself - that happens automatically
    // in the background via FraudConsumer/FraudBatchConsumerWorker.
    [ApiController]
    [Route("api/fraud")]
    public class FraudController : ControllerBase
    {
        private readonly IFraudRepository _repository;

        public FraudController(IFraudRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Query fraud-evaluated transaction events by date range and optional filters.
        /// </summary>
        [HttpGet("events")]
        public async Task<ActionResult<IEnumerable<FraudEventRecord>>> QueryEvents([FromQuery] FraudQueryDto query)
        {
            var records = await _repository.QueryFraudEventsAsync(query);
            return Ok(records);
        }

        /// <summary>
        /// Query only flagged transaction events by date range and optional filters.
        /// </summary>
        [HttpGet("events/flagged")]
        public async Task<ActionResult<IEnumerable<FraudEventRecord>>> QueryFlaggedEvents([FromQuery] FraudQueryDto query)
        {
            var records = await _repository.QueryFlaggedOnlyFraudEventsAsync(query);
            return Ok(records);
        }

        /// <summary>
        /// Retrieve all fraud rule results for a specific fraud event.
        /// </summary>
        [HttpGet("events/{fraudEventId:long}/rules")]
        public async Task<ActionResult<IEnumerable<FraudRuleSetRecord>>> GetRuleResults(long fraudEventId)
        {
            var records = await _repository.GetRuleResultsForEventAsync(fraudEventId);
            return Ok(records);
        }
    }
}
