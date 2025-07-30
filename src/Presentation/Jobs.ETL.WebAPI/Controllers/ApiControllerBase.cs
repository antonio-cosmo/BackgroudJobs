using System;
using Jobs.ETL.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Bussola.ETL.WebAPI.Controllers;

/// <summary>
/// Controlador base da API
/// </summary>
// [Route("api/v{version:apiVersion}/[controller]")]
[Route("api/[controller]")]
[ApiController]
public class ApiControllerBase(IJobEnqueuer jobEnqueuer) : ControllerBase
{
    protected readonly IJobEnqueuer JobEnqueuer = jobEnqueuer ?? throw new ArgumentNullException(nameof(jobEnqueuer));
}
