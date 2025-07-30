using Bussola.ETL.Application.Jobs.DTOs;
using Jobs.ETL.Application.Interfaces;
using Jobs.ETL.Application.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace Bussola.ETL.WebAPI.Controllers.v1;


public class EmailController(IJobEnqueuer jobEnqueuer) : ApiControllerBase(jobEnqueuer)
{

    [HttpPost("send-welcome-email")]
    public async Task<IActionResult> SendWelcomeEmail(string email, string name)
    {
        var payload = new SendWelcomeEmailPayload(email, name);
        await JobEnqueuer.EnqueueAsync<SendWelcomeEmailJob>(payload);
        return Accepted(value: $"Job para enviar e-mail para '{email}' foi enfileirado.");
    }
}
