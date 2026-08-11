using Dapper;
using PlainCrud.Abstractions;

namespace Testing;

[TestClass]
public sealed class MySqlProviderTests
{
    private const string Table = "products";
    private const string IdCol = "product_id";

    private static IDictionary<string, object> MakeEntity(int id = 1) =>
        new Dictionary<string, object>
        {
            [IdCol]         = id,
            ["product_name"] = "Widget",
            ["Price"]        = 9.99m,
            ["StockQuantity"] = 42
        };

    private static DynamicParameters ParamsOf(CommandDefinition cmd) =>
        (DynamicParameters)cmd.Parameters!;

    // ── Insert ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Insert_SqlContainsTableName()
    {
        var provider = new MySqlProvider();
        var cmd = provider.Insert(Table, IdCol, MakeEntity());
        StringAssert.Contains(cmd.CommandText, $"`{Table}`");
    }

    [TestMethod]
    public void Insert_SqlExcludesIdentityColumnFromColumnList()
    {
        var provider = new MySqlProvider();
        var cmd = provider.Insert(Table, IdCol, MakeEntity());
        // The column list (before VALUES) must not contain the PK
        var columnList = cmd.CommandText[..cmd.CommandText.IndexOf(") VALUES")];
        Assert.IsFalse(columnList.Contains($"`{IdCol}`"),
            $"INSERT column list should not contain identity column `{IdCol}`");
    }

    [TestMethod]
    public void Insert_SqlContainsAllNonIdentityColumns()
    {
        var provider = new MySqlProvider();
        var entity = MakeEntity();
        var cmd = provider.Insert(Table, IdCol, entity);

        foreach (var col in entity.Keys.Where(k => k != IdCol))
            StringAssert.Contains(cmd.CommandText, $"`{col}`");
    }

    [TestMethod]
    public void Insert_SqlEndsWithLastInsertId()
    {
        var provider = new MySqlProvider();
        var cmd = provider.Insert(Table, IdCol, MakeEntity());
        StringAssert.EndsWith(cmd.CommandText.TrimEnd(), "SELECT LAST_INSERT_ID();");
    }

    [TestMethod]
    public void Insert_ParametersContainAllNonIdentityValues()
    {
        var provider = new MySqlProvider();
        var entity = MakeEntity();
        var cmd = provider.Insert(Table, IdCol, entity);
        var p = ParamsOf(cmd);

        foreach (var col in entity.Keys.Where(k => k != IdCol))
            Assert.AreEqual(entity[col], p.Get<object>(col));
    }

    [TestMethod]
    public void Insert_ParametersDoNotContainIdentityColumn()
    {
        var provider = new MySqlProvider();
        var cmd = provider.Insert(Table, IdCol, MakeEntity());
        var p = ParamsOf(cmd);
        var names = p.ParameterNames.ToList();
        CollectionAssert.DoesNotContain(names, IdCol);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Update_SqlContainsTableName()
    {
        var provider = new MySqlProvider();
        var cmd = provider.Update(Table, IdCol, MakeEntity());
        StringAssert.Contains(cmd.CommandText, $"`{Table}`");
    }

    [TestMethod]
    public void Update_SqlContainsSetClauseForNonIdentityColumns()
    {
        var provider = new MySqlProvider();
        var entity = MakeEntity();
        var cmd = provider.Update(Table, IdCol, entity);

        foreach (var col in entity.Keys.Where(k => k != IdCol))
            StringAssert.Contains(cmd.CommandText, $"`{col}` = @{col}");
    }

    [TestMethod]
    public void Update_SqlContainsWhereClauseWithIdentityColumn()
    {
        var provider = new MySqlProvider();
        var cmd = provider.Update(Table, IdCol, MakeEntity());
        StringAssert.Contains(cmd.CommandText, $"WHERE `{IdCol}` = @{IdCol}");
    }

    [TestMethod]
    public void Update_ParametersContainIdentityValue()
    {
        var provider = new MySqlProvider();
        var entity = MakeEntity(id: 7);
        var cmd = provider.Update(Table, IdCol, entity);
        Assert.AreEqual(7, ParamsOf(cmd).Get<object>(IdCol));
    }

    [TestMethod]
    public void Update_ParametersContainAllColumnValues()
    {
        var provider = new MySqlProvider();
        var entity = MakeEntity();
        var cmd = provider.Update(Table, IdCol, entity);
        var p = ParamsOf(cmd);

        foreach (var col in entity.Keys)
            Assert.AreEqual(entity[col], p.Get<object>(col));
    }
}

