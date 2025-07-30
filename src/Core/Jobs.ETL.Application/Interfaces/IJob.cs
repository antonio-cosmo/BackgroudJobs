using System;

namespace Jobs.ETL.Application.Interfaces;

public interface IJob
{
    Task ExecuteAsync(string? payload, CancellationToken cancellationToken);
}
