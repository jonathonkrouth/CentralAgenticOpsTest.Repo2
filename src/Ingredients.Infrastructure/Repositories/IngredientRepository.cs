using System.Data;
using System.Data.Common;
using System.Globalization;
using Ingredients.Application.Abstractions;
using Ingredients.Application.Domain;
using Ingredients.Infrastructure.Data;

namespace Ingredients.Infrastructure.Repositories;

public sealed class IngredientRepository(IDbConnectionFactory connectionFactory) : IIngredientRepository
{
    private const string SelectSql = """
        SELECT Id, Name, Description, UnitOfMeasure, CaloriesPerUnit, IsAllergen, CreatedAt, UpdatedAt
        FROM dbo.Ingredients
        """;

    private const string InsertSql = """
        INSERT INTO dbo.Ingredients (Name, Description, UnitOfMeasure, CaloriesPerUnit, IsAllergen, CreatedAt, UpdatedAt)
        OUTPUT INSERTED.Id
        VALUES (@Name, @Description, @UnitOfMeasure, @CaloriesPerUnit, @IsAllergen, @CreatedAt, @UpdatedAt);
        """;

    private const string UpdateSql = """
        UPDATE dbo.Ingredients
        SET Name = @Name,
            Description = @Description,
            UnitOfMeasure = @UnitOfMeasure,
            CaloriesPerUnit = @CaloriesPerUnit,
            IsAllergen = @IsAllergen,
            UpdatedAt = @UpdatedAt
        WHERE Id = @Id;
        """;

    private const string DeleteSql = "DELETE FROM dbo.Ingredients WHERE Id = @Id;";

    public async Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " ORDER BY Name;";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var ingredients = new List<Ingredient>();
        while (await reader.ReadAsync(cancellationToken))
        {
            ingredients.Add(Map(reader));
        }

        return ingredients;
    }

    public async Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectSql + " WHERE Id = @Id;";
        AddParameter(command, "@Id", DbType.Int32, id);

        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = InsertSql;
        AddWritableParameters(command, ingredient);
        AddParameter(command, "@CreatedAt", DbType.DateTimeOffset, ingredient.CreatedAt);

        var newId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(newId, CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = UpdateSql;
        AddParameter(command, "@Id", DbType.Int32, ingredient.Id);
        AddWritableParameters(command, ingredient);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = DeleteSql;
        AddParameter(command, "@Id", DbType.Int32, id);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static void AddWritableParameters(DbCommand command, Ingredient ingredient)
    {
        AddParameter(command, "@Name", DbType.String, ingredient.Name);
        AddParameter(command, "@Description", DbType.String, ingredient.Description);
        AddParameter(command, "@UnitOfMeasure", DbType.String, ingredient.UnitOfMeasure.ToString());
        AddParameter(command, "@CaloriesPerUnit", DbType.Decimal, ingredient.CaloriesPerUnit);
        AddParameter(command, "@IsAllergen", DbType.Boolean, ingredient.IsAllergen);
        AddParameter(command, "@UpdatedAt", DbType.DateTimeOffset, ingredient.UpdatedAt);
    }

    private static void AddParameter(DbCommand command, string name, DbType dbType, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static Ingredient Map(DbDataReader reader)
    {
        var descriptionOrdinal = reader.GetOrdinal("Description");

        return new Ingredient
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            Name = reader.GetString(reader.GetOrdinal("Name")),
            Description = reader.IsDBNull(descriptionOrdinal) ? null : reader.GetString(descriptionOrdinal),
            UnitOfMeasure = Enum.Parse<UnitOfMeasure>(reader.GetString(reader.GetOrdinal("UnitOfMeasure"))),
            CaloriesPerUnit = reader.GetDecimal(reader.GetOrdinal("CaloriesPerUnit")),
            IsAllergen = reader.GetBoolean(reader.GetOrdinal("IsAllergen")),
            CreatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("CreatedAt")),
            UpdatedAt = reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("UpdatedAt")),
        };
    }
}
