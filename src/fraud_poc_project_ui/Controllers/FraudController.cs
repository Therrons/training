using fraud_poc_project_models.Dto;
using fraud_poc_project_models.Mapping;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fraud_poc_project.Controllers
{
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
        public async Task<ActionResult<IEnumerable<FraudEventDto>>> QueryEvents([FromQuery] FraudQueryDto query)
        {
            var model = query.ToModel();
            var records = await _repository.QueryFraudEventsAsync(model);
            return Ok(records.ToDto());
        }

        /// <summary>
        /// Query only flagged transaction events by date range and optional filters.
        /// </summary>
        [HttpGet("events/flagged")]
        public async Task<ActionResult<IEnumerable<FraudEventDto>>> QueryFlaggedEvents([FromQuery] FraudQueryDto query)
        {
            var model = query.ToModel();
            model.IsFlaggedOnly = true;
            var records = await _repository.QueryFraudEventsAsync(model);
            return Ok(records.ToDto());
        }

        /// <summary>
        /// Retrieve all fraud rule results for a specific fraud event.
        /// </summary>
        [HttpGet("events/{fraudEventId:long}/rules")]
        public async Task<ActionResult<IEnumerable<FraudRuleResultDto>>> GetRuleResults(long fraudEventId)
        {
            var records = await _repository.GetRuleResultsForEventAsync(fraudEventId);
            return Ok(records.ToDto());
        }
    }
}
