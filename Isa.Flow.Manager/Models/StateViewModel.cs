using Isa.Flow.Interact.Extractor.Rpc;

namespace Isa.Flow.Manager.Models
{
    public class StateViewModel
    {
        public string NewAndUpdatedQueueName { get; set; }

        public string DeletedQueueName { get; set; }

        public int? NewCount { get; set; }

        public int? DeletedCount { get; set; }

        public FuncStateResponse? ExtractorStarted { get; set; }

        public bool? TgCollectorStarted { get; set; }

        public bool? VkCollectorStarted { get; set; }

        public bool? IndexerStarted { get; set; }
    }
}
