using System;
using Jobs.ETL.Application.Interfaces;
using Jobs.ETL.Application.Models;
using Jobs.ETL.Domain.Enums;
using Jobs.ETL.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Jobs.ETL.Infrastructure.BackgroundJobs;

public class JobProcessorService : BackgroundService
{
    private readonly ILogger<JobProcessorService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _maxRetries;
    private readonly int _staleJobTimeoutMinutes;
    private readonly TimeSpan _pollInterval;
    private readonly int _maxConcurrentJobs;
    private readonly List<Task> _runningTasks = new();

    public JobProcessorService(
       ILogger<JobProcessorService> logger,
       IServiceProvider serviceProvider,
       IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;

        var settings = configuration.GetSection("JobSettings");
        _maxRetries = settings.GetValue("MaxRetries", 5);
        _staleJobTimeoutMinutes = settings.GetValue("StaleJobTimeoutMinutes", 30);
        _pollInterval = TimeSpan.FromSeconds(settings.GetValue("PollIntervalSeconds", 2));
        _maxConcurrentJobs = settings.GetValue("MaxConcurrentJobs", 10);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Job Processor Service (MongoDB) iniciando com MaxConcurrentJobs = {MaxConcurrentJobs}", _maxConcurrentJobs);
        await ResetStaleJobsAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Dispatcher (MongoDB) está rodando.");
        while (!stoppingToken.IsCancellationRequested)
        {
            _runningTasks.RemoveAll(t => t.IsCompleted);
            int availableSlots = _maxConcurrentJobs - _runningTasks.Count;

            if (availableSlots > 0)
            {
                // Inicia workers para preencher os espaços disponíveis.
                // A lógica de pegar os jobs agora está dentro do worker para garantir atomicidade.
                for (int i = 0; i < availableSlots; i++)
                {
                    _runningTasks.Add(ProcessSingleJobAsync(stoppingToken));
                }
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
        await Task.WhenAll(_runningTasks);
    }

    private async Task ProcessSingleJobAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var jobsCollection = context.Jobs;

        // A MÁGICA ATÔMICA DO MONGODB ACONTECE AQUI
        var filter = Builders<Job>.Filter.And(
            Builders<Job>.Filter.Eq(j => j.Status, JobStatus.Pending),
            Builders<Job>.Filter.Lte(j => j.ScheduledAt, DateTime.UtcNow)
        );

        var update = Builders<Job>.Update
            .Set(j => j.Status, JobStatus.Processing)
            .Set(j => j.ProcessingStartedAt, DateTime.UtcNow);

        // Encontra um job pendente e o atualiza para processando em uma única operação atômica.
        var job = await jobsCollection.FindOneAndUpdateAsync(filter, update, null, stoppingToken);

        if (job == null)
        {
            // Nenhum job encontrado, o worker encerra silenciosamente.
            return;
        }

        _logger.LogInformation("Worker iniciando job {JobId} do tipo {JobType}", job.Id, job.JobType);

        try
        {
            var jobType = Type.GetType(job.JobType);
            if (jobType == null) throw new InvalidOperationException($"Tipo de job não encontrado: '{job.JobType}'.");

            var jobInstance = (IJob)scope.ServiceProvider.GetRequiredService(jobType);
            await jobInstance.ExecuteAsync(job.Payload, stoppingToken);

            job.Status = JobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation("Worker concluiu job {JobId} com sucesso.", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Worker falhou no job {JobId}. Retentativa {RetryCount}/{MaxRetries}", job.Id, job.RetryCount + 1, _maxRetries);
            job.ErrorMessage = ex.ToString();
            job.RetryCount++;

            if (job.RetryCount >= _maxRetries)
            {
                job.Status = JobStatus.Failed;
                job.FailedAt = DateTime.UtcNow;
            }
            else
            {
                var delayInSeconds = Math.Pow(2, job.RetryCount) * 30;
                job.Status = JobStatus.Pending;
                job.ScheduledAt = DateTime.UtcNow.AddSeconds(delayInSeconds);
            }
        }

        // Substitui o documento inteiro com seu estado final (concluído ou falho/reagendado)
        await jobsCollection.ReplaceOneAsync(Builders<Job>.Filter.Eq(j => j.Id, job.Id), job, cancellationToken: stoppingToken);
    }

    private async Task ResetStaleJobsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var jobsCollection = context.Jobs;

        var staleTime = DateTime.UtcNow.AddMinutes(-_staleJobTimeoutMinutes);

        var filter = Builders<Job>.Filter.And(
            Builders<Job>.Filter.Eq(j => j.Status, JobStatus.Processing),
            Builders<Job>.Filter.Lt(j => j.ProcessingStartedAt, staleTime)
        );

        var update = Builders<Job>.Update
            .Set(j => j.Status, JobStatus.Pending)
            .Set(j => j.ProcessingStartedAt, null);

        var result = await jobsCollection.UpdateManyAsync(filter, update, null, stoppingToken);

        if (result.ModifiedCount > 0)
        {
            _logger.LogWarning("Encontrados e resetados {Count} jobs travados (stale).", result.ModifiedCount);
        }
    }

}
