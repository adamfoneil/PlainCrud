using Dapper;
using Microsoft.EntityFrameworkCore;

namespace PlainCrud.Abstractions;

public static class DbContextExtensions
{
    public static async Task<TKey> InsertAsync<TEntity, TKey>(this DbContext dbContext, TEntity entity, ISqlBuilder sqlBuilder)
    {
        var cn = dbContext.Database.GetDbConnection();

        Dictionary<string, object> entityValues = ParseDictionary(entity, dbContext);

        var cmd = sqlBuilder.Insert(entityValues);

        return await cn.QuerySingleAsync<TKey>(cmd);
    }

    public static async Task UpdateAsync<TEntity, TKey>(this DbContext dbContext, TEntity entity, ISqlBuilder sqlBuilder)
    {
        var cn = dbContext.Database.GetDbConnection();

        Dictionary<string, object> entityValues = ParseDictionary(entity, dbContext);

        var cmd = sqlBuilder.Update(entityValues);

        await cn.ExecuteAsync(cmd);        
    }

    private static Dictionary<string, object> ParseDictionary<TEntity>(TEntity? entity, DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(entity, nameof(entity));

        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"Entity type '{typeof(TEntity).Name}' is not registered in the DbContext model.");

        var result = new Dictionary<string, object>();

        foreach (var property in entityType.GetProperties())
        {
            var columnName = property.GetColumnName();
            var value = property.PropertyInfo?.GetValue(entity);
            result[columnName] = value ?? DBNull.Value;
        }

        return result;
    }
}
