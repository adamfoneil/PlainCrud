using System.Text;
using Dapper;

namespace PlainCrud.Extensions;

public class PostgresSqlProvider : ISqlProvider
{
    public CommandDefinition Insert(string tableName, string identityColumn, IDictionary<string, object> entity)
    {
        var columns = entity.Keys.Where(k => k != identityColumn).ToList();

        var sql = new StringBuilder();
        sql.Append($"INSERT INTO \"{tableName}\" (");
        sql.Append(string.Join(", ", columns.Select(c => $"\"{c}\"")));
        sql.Append(") VALUES (");
        sql.Append(string.Join(", ", columns.Select(c => $"@{c}")));
        sql.Append($") RETURNING \"{identityColumn}\";");

        var parameters = new DynamicParameters();
        foreach (var col in columns) parameters.Add(col, entity[col]);

        return new CommandDefinition(sql.ToString(), parameters);
    }

    public CommandDefinition Update(string tableName, string identityColumn, IDictionary<string, object> entity)
    {
        var columns = entity.Keys.Where(k => k != identityColumn).ToList();

        var sql = new StringBuilder();
        sql.Append($"UPDATE \"{tableName}\" SET ");
        sql.Append(string.Join(", ", columns.Select(c => $"\"{c}\" = @{c}")));
        sql.Append($" WHERE \"{identityColumn}\" = @{identityColumn};");

        var parameters = new DynamicParameters();
        foreach (var col in columns) parameters.Add(col, entity[col]);
        parameters.Add(identityColumn, entity[identityColumn]);

        return new CommandDefinition(sql.ToString(), parameters);
    }
}
