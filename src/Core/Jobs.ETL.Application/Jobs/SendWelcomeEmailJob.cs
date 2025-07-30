using System;
using System.Text.Json;
using Jobs.ETL.Application.Interfaces;
using Jobs.ETL.Application.Jobs.DTOs;
using Microsoft.Extensions.Logging;

namespace Jobs.ETL.Application.Jobs;

public class SendWelcomeEmailJob(ILogger<SendWelcomeEmailJob> logger) : IJob
{
    private readonly ILogger<SendWelcomeEmailJob> _logger = logger;

    public async Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(payload))
        {
            throw new ArgumentNullException(nameof(payload), "Payload é necessário para este job.");
        }

        var data = JsonSerializer.Deserialize<SendWelcomeEmailPayload>(payload);
        if (data is null)
        {
            throw new InvalidOperationException("Payload inválido para SendWelcomeEmailJob.");
        }

        _logger.LogInformation("Simulando envio de e-mail de boas-vindas para {Email}...", data.Email);
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        // Para testar a lógica de falha, descomente a linha abaixo
        // throw new HttpRequestException("Servidor de e-mail indisponível.");

        _logger.LogInformation("E-mail para {Email} enviado com sucesso!", data.Email);
    }
}
