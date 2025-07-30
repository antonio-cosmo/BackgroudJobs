using Jobs.ETL.Application.Interfaces;
using Jobs.ETL.Application.Jobs;
using Jobs.ETL.Infrastructure.BackgroundJobs;
using Jobs.ETL.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddHostedService<JobProcessorService>();
builder.Services.AddHostedService<RecurringJobSchedulerService>();

builder.Services.AddScoped<MongoDbContext>();
builder.Services.AddScoped<IJobEnqueuer, JobEnqueuer>();
builder.Services.AddTransient<SendWelcomeEmailJob>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
