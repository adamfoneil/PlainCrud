using Dapper;

namespace PlainCrud.Abstractions;

public interface ISqlProvider
{    
    CommandDefinition Insert(string tableName, string identityColumn, IDictionary<string, object> entity);
    CommandDefinition Update(string tableName, string identityColumn, IDictionary<string, object> entity);
}
