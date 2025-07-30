using System;
using Jobs.ETL.Domain.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace Jobs.ETL.Application.Models;


[BsonIgnoreExtraElements]
public class Job
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("JobType")]
    public string JobType { get; set; } = string.Empty;

    [BsonElement("Payload")]
    [BsonIgnoreIfNull]
    public string? Payload { get; set; }

    [BsonElement("Status")]
    [BsonRepresentation(BsonType.String)]
    public JobStatus Status { get; set; }

    [BsonElement("CreatedAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("ScheduledAt")]
    public DateTime ScheduledAt { get; set; }

    [BsonElement("ProcessingStartedAt")]
    [BsonIgnoreIfNull]
    public DateTime? ProcessingStartedAt { get; set; }

    [BsonElement("CompletedAt")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAt { get; set; }

    [BsonElement("FailedAt")]
    [BsonIgnoreIfNull]
    public DateTime? FailedAt { get; set; }

    [BsonElement("RetryCount")]
    public int RetryCount { get; set; }

    [BsonElement("ErrorMessage")]
    [BsonIgnoreIfNull]
    public string? ErrorMessage { get; set; }
}
