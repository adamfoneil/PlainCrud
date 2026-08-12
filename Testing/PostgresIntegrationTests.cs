using Dapper;
using Npgsql;
using PlainCrud.Extensions;
using Testcontainers.PostgreSql;

namespace Testing;

[TestClass]
public sealed class PostgresIntegrationTests
{
    private static PostgreSqlContainer _container = null!;
    private static string _connectionString = null!;

    private const string Table = "products";
    private const string IdCol = "product_id";

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext _)
    {
        _container = new PostgreSqlBuilder().Build();
        await _container.StartAsync();

        _connectionString = _container.GetConnectionString();

        await using var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync();
        await cn.ExecuteAsync($"""
            CREATE TABLE "{Table}" (
                "{IdCol}"      SERIAL PRIMARY KEY,
                "product_name" TEXT           NOT NULL,
                "Price"        NUMERIC(10, 2) NOT NULL,
                "StockQuantity" INTEGER       NOT NULL
            );
            """);
    }

    [ClassCleanup]
    public static async Task ClassCleanup()
    {
        await _container.DisposeAsync();
    }

    private static async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var cn = new NpgsqlConnection(_connectionString);
        await cn.OpenAsync();
        return cn;
    }

    private static IDictionary<string, object> MakeEntity(int id = 0) =>
        new Dictionary<string, object>
        {
            [IdCol]           = id,
            ["product_name"]  = "Widget",
            ["Price"]         = 9.99m,
            ["StockQuantity"] = 42
        };

    // ── Insert ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Insert_ReturnsGeneratedId()
    {
        await using var cn = await OpenConnectionAsync();
        var provider = new PostgresSqlProvider();
        var cmd = provider.Insert(Table, IdCol, MakeEntity());

        var newId = await cn.QuerySingleAsync<int>(cmd);

        Assert.IsTrue(newId > 0, $"Expected a positive generated id, got {newId}");
    }

    [TestMethod]
    public async Task Insert_RowExistsAfterInsert()
    {
        await using var cn = await OpenConnectionAsync();
        var provider = new PostgresSqlProvider();
        var cmd = provider.Insert(Table, IdCol, MakeEntity());

        var newId = await cn.QuerySingleAsync<int>(cmd);

        var count = await cn.QuerySingleAsync<int>(
            $"SELECT COUNT(*) FROM \"{Table}\" WHERE \"{IdCol}\" = @id",
            new { id = newId });

        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public async Task Insert_StoredValuesMatchInput()
    {
        await using var cn = await OpenConnectionAsync();
        var provider = new PostgresSqlProvider();
        var entity = MakeEntity();
        var cmd = provider.Insert(Table, IdCol, entity);

        var newId = await cn.QuerySingleAsync<int>(cmd);

        var row = await cn.QuerySingleAsync(
            $"SELECT * FROM \"{Table}\" WHERE \"{IdCol}\" = @id",
            new { id = newId });

        Assert.AreEqual(entity["product_name"], row.product_name);
        Assert.AreEqual(entity["StockQuantity"], (int)row.StockQuantity);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task Update_ModifiesExistingRow()
    {
        await using var cn = await OpenConnectionAsync();
        var provider = new PostgresSqlProvider();
        var insertCmd = provider.Insert(Table, IdCol, MakeEntity());
        var newId = await cn.QuerySingleAsync<int>(insertCmd);

        // Update it
        var updated = new Dictionary<string, object>
        {
            [IdCol]           = newId,
            ["product_name"]  = "Gadget",
            ["Price"]         = 19.99m,
            ["StockQuantity"] = 10
        };
        var updateCmd = provider.Update(Table, IdCol, updated);
        await cn.ExecuteAsync(updateCmd);

        var row = await cn.QuerySingleAsync(
            $"SELECT * FROM \"{Table}\" WHERE \"{IdCol}\" = @id",
            new { id = newId });

        Assert.AreEqual("Gadget", (string)row.product_name);
        Assert.AreEqual(10, (int)row.StockQuantity);
    }

    [TestMethod]
    public async Task Update_DoesNotAffectOtherRows()
    {
        await using var cn = await OpenConnectionAsync();
        var provider = new PostgresSqlProvider();

        // Insert two rows
        var id1 = await cn.QuerySingleAsync<int>(provider.Insert(Table, IdCol, MakeEntity()));
        var id2 = await cn.QuerySingleAsync<int>(provider.Insert(Table, IdCol, MakeEntity()));

        // Update only id1
        var updated = new Dictionary<string, object>
        {
            [IdCol]           = id1,
            ["product_name"]  = "Changed",
            ["Price"]         = 1.00m,
            ["StockQuantity"] = 1
        };
        await cn.ExecuteAsync(provider.Update(Table, IdCol, updated));

        var row2 = await cn.QuerySingleAsync(
            $"SELECT * FROM \"{Table}\" WHERE \"{IdCol}\" = @id",
            new { id = id2 });

        Assert.AreEqual("Widget", (string)row2.product_name);
    }
}
