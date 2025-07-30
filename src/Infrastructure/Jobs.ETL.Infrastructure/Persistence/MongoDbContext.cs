using System;
using Jobs.ETL.Application.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Jobs.ETL.Infrastructure.Persistence;

public class MongoDbContext
{
    public IMongoCollection<Job> Jobs { get; }

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);

        Jobs = database.GetCollection<Job>(settings.Value.CollectionNames.Jobs);
    }
}

