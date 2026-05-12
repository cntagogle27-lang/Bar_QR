using Bar.Eugenio.Domain.Menu;
using Bar.Eugenio.Domain.Orders;
using Bar.Eugenio.Domain.Roles;
using Bar.Eugenio.Domain.Staff;
using Bar.Eugenio.Domain.Tables;
using Microsoft.Data.Sqlite;
using System.Globalization;

namespace Bar.Eugenio.Infrastructure;

public sealed class SqliteRestaurantStore
{
    private readonly string _connectionString;
    private readonly string _contentRootPath;

    public SqliteRestaurantStore(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=bareugenio.dev.db";
        _contentRootPath = environment.ContentRootPath;
    }

    public void Initialize(InMemoryRestaurantState state)
    {
        EnsureDatabaseFile();
        using var connection = OpenConnection();
        CreateSchema(connection);
        SeedIfEmpty(connection);
        Load(connection, state);
    }

    public void Save(InMemoryRestaurantState state)
    {
        using var connection = OpenConnection();
        using var transaction = connection.BeginTransaction();

        Execute(connection, transaction, "DELETE FROM OrderItems");
        Execute(connection, transaction, "DELETE FROM Tables");

        foreach (var table in state.Tables)
        {
            Execute(
                connection,
                transaction,
                """
                INSERT INTO Tables (Number, Status, AccessCode, AccessCodeExpiresAt)
                VALUES ($number, $status, $accessCode, $expiresAt)
                """,
                ("$number", table.Number),
                ("$status", table.Status.ToString()),
                ("$accessCode", (object?)table.AccessCode ?? DBNull.Value),
                ("$expiresAt", table.AccessCodeExpiresAt?.ToString("O") ?? (object)DBNull.Value));

            foreach (var item in table.Items)
            {
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO OrderItems (Id, TableNumber, ProductId, Name, OriginalPrice, TablePrice, CreatedBy, CreatedAt)
                    VALUES ($id, $tableNumber, $productId, $name, $originalPrice, $tablePrice, $createdBy, $createdAt)
                    """,
                    ("$id", item.Id),
                    ("$tableNumber", table.Number),
                    ("$productId", item.ProductId),
                    ("$name", item.Name),
                    ("$originalPrice", item.OriginalPrice.ToString(CultureInfo.InvariantCulture)),
                    ("$tablePrice", item.TablePrice.ToString(CultureInfo.InvariantCulture)),
                    ("$createdBy", item.CreatedBy),
                    ("$createdAt", item.CreatedAt.ToString("O")));
            }
        }

        transaction.Commit();
    }

    private void EnsureDatabaseFile()
    {
        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var dataSource = builder.DataSource;

        if (string.IsNullOrWhiteSpace(dataSource) || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var targetPath = Path.GetFullPath(dataSource);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        var seedPath = Path.Combine(_contentRootPath, "Seed", "app.db");
        if (!File.Exists(targetPath) && File.Exists(seedPath))
        {
            File.Copy(seedPath, targetPath);
        }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static void CreateSchema(SqliteConnection connection)
    {
        Execute(
            connection,
            null,
            """
            CREATE TABLE IF NOT EXISTS Products (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Price TEXT NOT NULL,
                Category TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Staff (
                Id INTEGER PRIMARY KEY,
                Name TEXT NOT NULL,
                Role TEXT NOT NULL,
                Pin TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS Tables (
                Number INTEGER PRIMARY KEY,
                Status TEXT NOT NULL,
                AccessCode TEXT NULL,
                AccessCodeExpiresAt TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS OrderItems (
                Id INTEGER PRIMARY KEY,
                TableNumber INTEGER NOT NULL,
                ProductId INTEGER NOT NULL,
                Name TEXT NOT NULL,
                OriginalPrice TEXT NOT NULL,
                TablePrice TEXT NOT NULL,
                CreatedBy TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            );
            """);
    }

    private static void SeedIfEmpty(SqliteConnection connection)
    {
        if (ScalarLong(connection, "SELECT COUNT(*) FROM Products") == 0)
        {
            var products = new[]
            {
                new MenuProduct { Id = 1, Name = "Cafe espresso", Price = 1.50m, Category = "Bebidas" },
                new MenuProduct { Id = 2, Name = "Croissant", Price = 2.80m, Category = "Comida" },
                new MenuProduct { Id = 3, Name = "Hamburguesa", Price = 8.50m, Category = "Comida" },
                new MenuProduct { Id = 4, Name = "Cerveza", Price = 3.00m, Category = "Bebidas" },
                new MenuProduct { Id = 5, Name = "Tarta de queso", Price = 4.20m, Category = "Postres" }
            };

            foreach (var product in products)
            {
                Execute(
                    connection,
                    null,
                    "INSERT INTO Products (Id, Name, Price, Category) VALUES ($id, $name, $price, $category)",
                    ("$id", product.Id),
                    ("$name", product.Name),
                    ("$price", product.Price.ToString(CultureInfo.InvariantCulture)),
                    ("$category", product.Category));
            }
        }

        if (ScalarLong(connection, "SELECT COUNT(*) FROM Staff") == 0)
        {
            var staff = new[]
            {
                new StaffMember { Id = 1, Name = "Paco", Role = StaffRole.Empleado },
                new StaffMember { Id = 2, Name = "Luis", Role = StaffRole.Empleado },
                new StaffMember { Id = 3, Name = "Marta", Role = StaffRole.Encargado, Pin = "4321" },
                new StaffMember { Id = 4, Name = "Admin", Role = StaffRole.Admin, Pin = "1234" }
            };

            foreach (var member in staff)
            {
                Execute(
                    connection,
                    null,
                    "INSERT INTO Staff (Id, Name, Role, Pin) VALUES ($id, $name, $role, $pin)",
                    ("$id", member.Id),
                    ("$name", member.Name),
                    ("$role", member.Role.ToString()),
                    ("$pin", (object?)member.Pin ?? DBNull.Value));
            }
        }

        if (ScalarLong(connection, "SELECT COUNT(*) FROM Tables") == 0)
        {
            for (var number = 1; number <= 12; number++)
            {
                Execute(
                    connection,
                    null,
                    "INSERT INTO Tables (Number, Status) VALUES ($number, $status)",
                    ("$number", number),
                    ("$status", TableStatus.Libre.ToString()));
            }
        }
    }

    private static void Load(SqliteConnection connection, InMemoryRestaurantState state)
    {
        state.Products.Clear();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Id, Name, Price, Category FROM Products ORDER BY Id";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                state.Products.Add(new MenuProduct
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Price = decimal.Parse(reader.GetString(2), CultureInfo.InvariantCulture),
                    Category = reader.GetString(3)
                });
            }
        }

        state.Staff.Clear();
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Id, Name, Role, Pin FROM Staff ORDER BY Id";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                state.Staff.Add(new StaffMember
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Role = Enum.Parse<StaffRole>(reader.GetString(2)),
                    Pin = reader.IsDBNull(3) ? null : reader.GetString(3)
                });
            }
        }

        var itemsByTable = LoadItems(connection);
        state.Tables = [];
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT Number, Status, AccessCode, AccessCodeExpiresAt FROM Tables ORDER BY Number";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var number = reader.GetInt32(0);
                var table = new RestaurantTable(number);
                table.Restore(
                    Enum.Parse<TableStatus>(reader.GetString(1)),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.IsDBNull(3) ? null : DateTimeOffset.Parse(reader.GetString(3)),
                    itemsByTable.GetValueOrDefault(number, []));
                state.Tables.Add(table);
            }
        }

        state.NextOrderItemId = itemsByTable.Values.SelectMany(items => items).Select(item => item.Id).DefaultIfEmpty(0).Max() + 1;
    }

    private static Dictionary<int, List<OrderItem>> LoadItems(SqliteConnection connection)
    {
        var itemsByTable = new Dictionary<int, List<OrderItem>>();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, TableNumber, ProductId, Name, OriginalPrice, TablePrice, CreatedBy, CreatedAt
            FROM OrderItems
            ORDER BY Id
            """;
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var tableNumber = reader.GetInt32(1);
            if (!itemsByTable.TryGetValue(tableNumber, out var items))
            {
                items = [];
                itemsByTable[tableNumber] = items;
            }

            items.Add(new OrderItem(
                reader.GetInt32(0),
                reader.GetInt32(2),
                reader.GetString(3),
                decimal.Parse(reader.GetString(4), CultureInfo.InvariantCulture),
                decimal.Parse(reader.GetString(5), CultureInfo.InvariantCulture),
                reader.GetString(6),
                DateTimeOffset.Parse(reader.GetString(7))));
        }

        return itemsByTable;
    }

    private static long ScalarLong(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    private static void Execute(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value ?? DBNull.Value);
        }

        command.ExecuteNonQuery();
    }
}
