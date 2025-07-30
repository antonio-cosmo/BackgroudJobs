using System;
using System.Text.Json;
using Jobs.ETL.Application.Interfaces;
using Jobs.ETL.Application.Models;
using Jobs.ETL.Domain.Enums;
using Jobs.ETL.Infrastructure.Persistence;

namespace Jobs.ETL.Infrastructure.BackgroundJobs;

public class JobEnqueuer(MongoDbContext context) : IJobEnqueuer
{
    private readonly MongoDbContext _context = context;

    public Task EnqueueAsync<TJob>(object payload) where TJob : IJob
    {
        var serializedPayload = JsonSerializer.Serialize(payload);
        return EnqueueInternalAsync<TJob>(serializedPayload);
    }

    public Task EnqueueAsync<TJob>() where TJob : IJob
    {
        return EnqueueInternalAsync<TJob>(null);
    }

    private async Task EnqueueInternalAsync<TJob>(string? payload) where TJob : IJob
    {
        var job = new Job
        {
            JobType = typeof(TJob).AssemblyQualifiedName!,
            Payload = payload,
            Status = JobStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ScheduledAt = DateTime.UtcNow
        };

        await _context.Jobs.InsertOneAsync(job);
    }
}
