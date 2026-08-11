using Dapper;

namespace PlainCrud.Abstractions;

public interface ISqlBuilder
{    
    CommandDefinition Insert(IDictionary<string, object> entity);
    CommandDefinition Update(IDictionary<string, object> entity);
}
