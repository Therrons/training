using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;

namespace fraud_poc_project_buss.Service
{
    public class FraudQueryService : IFraudQueryService
    {
        private readonly IFraudRepository _repository;

        public FraudQueryService(IFraudRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<FraudEventRecord>> QueryEventsAsync(FraudQueryDto query)
            => await _repository.QueryFraudEventsAsync(query, false).ConfigureAwait(false);

        public async Task<IReadOnlyList<FraudRuleSetRecord>?> GetRuleResultsAsync(long fraudEventId)
        {
            // Every stored fraud event has one result per rule, so an empty list means the event does not exist
            var records = (await _repository.GetRuleResultsForEventAsync(fraudEventId).ConfigureAwait(false)).ToList();
            if (records.Count == 0)
            {
                return null;
            }

            return records;
        }
    }
}
