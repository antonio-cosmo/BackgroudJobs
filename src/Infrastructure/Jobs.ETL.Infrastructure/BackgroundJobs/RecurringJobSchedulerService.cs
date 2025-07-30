using System;
using Cronos;
using Jobs.ETL.Application.Interfaces;
using Jobs.ETL.Application.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jobs.ETL.Infrastructure.BackgroundJobs;

public class RecurringJobSchedulerService : BackgroundService
{
    private readonly ILogger<RecurringJobSchedulerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<(CronExpression cronExpression, Type jobType)> _recurringJobs;

    public RecurringJobSchedulerService(ILogger<RecurringJobSchedulerService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _recurringJobs = new List<(CronExpression, Type)>();

        // --- DEFINA SEU CRONOGRAMA DE JOBS AQUI ---
        _recurringJobs.Add((CronExpression.Parse("0 3 * * *", CronFormat.Standard), typeof(SendWelcomeEmailJob)));

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agendador de jobs recorrentes está iniciando.");

        // Aguarda um pouco para garantir que a aplicação esteja totalmente iniciada
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var utcNow = DateTime.UtcNow;

            foreach (var (cronExpression, jobType) in _recurringJobs)
            {
                var nextOccurrence = cronExpression.GetNextOccurrence(utcNow, TimeZoneInfo.Utc);

                // Verifica se a próxima ocorrência está no minuto atual
                if (nextOccurrence.HasValue && nextOccurrence.Value.ToString("yyyy-MM-dd HH:mm") == utcNow.ToString("yyyy-MM-dd HH:mm"))
                {
                    await EnqueueJobAsync(jobType);
                }
            }

            // Espera um minuto antes de verificar o cronograma novamente
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task EnqueueJobAsync(Type jobType)
    {
        _logger.LogInformation("Agendador enfileirando job recorrente do tipo: {JobType}", jobType.Name);

        using var scope = _serviceProvider.CreateScope();
        var enqueuer = scope.ServiceProvider.GetRequiredService<IJobEnqueuer>();

        var method = typeof(IJobEnqueuer).GetMethod(nameof(IJobEnqueuer.EnqueueAsync), Type.EmptyTypes);
        if (method != null)
        {
            var genericMethod = method.MakeGenericMethod(jobType);

            if (genericMethod.Invoke(enqueuer, null) is Task task)
            {
                await task;
            }
            else
            {
                _logger.LogError("Falha ao invocar o método EnqueueAsync para o job {JobType}.", jobType.Name);
            }
        }
    }
}
