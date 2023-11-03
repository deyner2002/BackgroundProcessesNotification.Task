using APIEmisorKafka.Enum;

namespace APIEmisorKafka.Models
{
    public class ProgrammingInfo
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool Active { get; set; }
        public DateTime ActivationTime { get; set; }
        public bool IsRecurring { get; set; }
        public Recurrence Recurrence { get; set; }
    }
}
