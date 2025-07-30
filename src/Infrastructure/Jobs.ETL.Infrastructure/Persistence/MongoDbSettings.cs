using System;

namespace Jobs.ETL.Infrastructure.Persistence;

public class MongoDbSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public CollectionNameSettings CollectionNames { get; set; } = new();
}


public class CollectionNameSettings
{
    public string Jobs { get; set; } = string.Empty;
}
