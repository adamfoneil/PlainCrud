using Dapper;
using Microsoft.EntityFrameworkCore;

namespace PlainCrud.Abstractions;

public static class DbContextExtensions
{
    public static async Task<TKey> InsertAsync<TEntity, TKey>(this DbContext dbContext, TEntity entity, ISqlProvider sqlBuilder)
    {
        var cn = dbContext.Database.GetDbConnection();

        var (table, idCol, properties) = ParseSqlElements(entity, dbContext);

        var cmd = sqlBuilder.Insert(table, idCol, properties);

        return await cn.QuerySingleAsync<TKey>(cmd);
    }

    public static async Task UpdateAsync<TEntity, TKey>(this DbContext dbContext, TEntity entity, ISqlProvider sqlBuilder)
    {
        var cn = dbContext.Database.GetDbConnection();

        var (table, idCol, properties) = ParseSqlElements(entity, dbContext);

        var cmd = sqlBuilder.Update(table, idCol, properties);

        await cn.ExecuteAsync(cmd);        
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
