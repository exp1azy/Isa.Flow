using Isa.Flow.VkCollector.Data;

namespace Isa.Flow.VkCollector.Entities
{
    public class Channel
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public bool Enabled { get; set; }

        public string Site { get; set; }

        public int Count { get; set; }

        public string? Type { get; set; }

        public static Channel? Map(SourceDao source) => source == null ? null : new Channel()
        {
            Id = source.Id,
            Title = source.Title,
            Enabled = source.Enabled,
            Site = source.Site,
            Count = source.Count,
            Type = source.Type
        };
    }
}
