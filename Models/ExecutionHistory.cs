using MongoDB.Bson;

public class ExecutionHistory
{
    public ObjectId Id { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}