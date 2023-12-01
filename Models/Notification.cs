using APIEmisorKafka.Enum;
using MongoDB.Bson;

namespace APIEmisorKafka.Models
{
    public class Notification
    {
        public ObjectId? Id { get; set; }
        public bool? IsProgrammed { get; set; }
        public ProgrammingInfo? ProgrammingInfo { get; set; }
        public List<Channel>? Channels { get; set; }
        public List<Contact>? Contacts { get; set; }
        public List<Template>? Templates { get; set; }
        public bool GetObject { get; set; }
        public string GetObjectUrl { get; set; }
        public string? Object { get; set; }
    }
}
