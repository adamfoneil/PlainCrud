using Dapper;
using Microsoft.EntityFrameworkCore;

namespace PlainCrud.Extensions;

/// <summary>
/// this is because I was getting nonsensical concurrency exceptions using plain EF Core SaveChanges, so I switched to 
/// the "nuclear option" of Dapper with some niceties to make it align with an existing EF Core DbContext
/// </summary>
public static class DbContextExtensions
{
    public static async Task<TKey> SaveAsync<TEntity, TKey>(this DbContext dbContext, TEntity entity, Func<TEntity, bool> isInsert, ISqlProvider sqlProvider) where TKey : notnull
    {
        if (isInsert(entity))
        {
            return await InsertAsync<TEntity, TKey>(dbContext, entity, sqlProvider);
        }

        await UpdateAsync<TEntity, TKey>(dbContext, entity, sqlProvider);

        return GetKeyValue<TEntity, TKey>(entity, dbContext);
    }

    public static async Task<TKey> InsertAsync<TEntity, TKey>(this DbContext dbContext, TEntity entity, ISqlProvider sqlProvider) where TKey : notnull
    {
        var cn = dbContext.Database.GetDbConnection();

        var (table, idCol, properties) = ParseSqlElements(entity, dbContext);

        var cmd = sqlProvider.Insert(table, idCol, properties);

        return await cn.QuerySingleAsync<TKey>(cmd);
    }

    public static async Task UpdateAsync<TEntity, TKey>(this DbContext dbContext, TEntity entity, ISqlProvider sqlBuilder) where TKey : notnull
    {
        var cn = dbContext.Database.GetDbConnection();

        var (table, idCol, properties) = ParseSqlElements(entity, dbContext);

        var cmd = sqlBuilder.Update(table, idCol, properties);

        await cn.ExecuteAsync(cmd);        
    }

    private static TKey GetKeyValue<TEntity, TKey>(TEntity entity, DbContext dbContext) where TKey : notnull
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not registered in the DbContext model.");

        var keyProperty = entityType.FindPrimaryKey()?.Properties.SingleOrDefault()
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' must have a single-column primary key.");

        return keyProperty.PropertyInfo?.GetValue(entity) is TKey value
            ? value
            : throw new InvalidOperationException($"Key value of entity '{typeof(TEntity).Name}' is null or not of type '{typeof(TKey).Name}'.");
    }

    private static (string TableName, string IdentityColumn, Dictionary<string, object> Properties) ParseSqlElements<TEntity>(TEntity? entity, DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not registered in the DbContext model.");

        var keyProperty = entityType.FindPrimaryKey()?.Properties.SingleOrDefault()
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' must have a single-column primary key.");

        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not mapped to a table.");
        var identityColumn = keyProperty.GetColumnName();
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in entityType.GetProperties())
        {
            var columnName = property.GetColumnName();
            var value = property.PropertyInfo?.GetValue(entity);
            result[columnName] = value ?? DBNull.Value;
        }

        return (tableName, identityColumn, result);
    }
}
