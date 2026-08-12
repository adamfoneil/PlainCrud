using Dapper;

namespace PlainCrud.Extensions;

public interface ISqlProvider
{    
    CommandDefinition Insert(string tableName, string identityColumn, IDictionary<string, object> entity);
    CommandDefinition Update(string tableName, string identityColumn, IDictionary<string, object> entity);
}
