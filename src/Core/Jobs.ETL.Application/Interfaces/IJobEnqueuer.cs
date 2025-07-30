using System;

namespace Jobs.ETL.Application.Interfaces;

public interface IJobEnqueuer
{
    Task EnqueueAsync<TJob>(object payload) where TJob : IJob;
    Task EnqueueAsync<TJob>() where TJob : IJob;
}
