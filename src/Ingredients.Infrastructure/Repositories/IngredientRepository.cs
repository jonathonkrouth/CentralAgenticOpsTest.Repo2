using System.Data;
using System.Globalization;
using Dapper;
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

        var rows = await connection.QueryAsync<IngredientRow>(
            new CommandDefinition(SelectSql + " ORDER BY Name;", cancellationToken: cancellationToken));

        return rows.Select(row => row.ToIngredient()).ToList();
    }

    public async Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Int32);

        var row = await connection.QueryFirstOrDefaultAsync<IngredientRow>(
            new CommandDefinition(SelectSql + " WHERE Id = @Id;", parameters, cancellationToken: cancellationToken));

        return row?.ToIngredient();
    }

    public async Task<int> CreateAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = CreateWritableParameters(ingredient);
        parameters.Add("@CreatedAt", ingredient.CreatedAt, DbType.DateTimeOffset);

        var newId = await connection.ExecuteScalarAsync<object>(
            new CommandDefinition(InsertSql, parameters, cancellationToken: cancellationToken));
        return Convert.ToInt32(newId, CultureInfo.InvariantCulture);
    }

    public async Task<bool> UpdateAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", ingredient.Id, DbType.Int32);
        AddWritableParameters(parameters, ingredient);

        return await connection.ExecuteAsync(
            new CommandDefinition(UpdateSql, parameters, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var parameters = new DynamicParameters();
        parameters.Add("@Id", id, DbType.Int32);

        return await connection.ExecuteAsync(
            new CommandDefinition(DeleteSql, parameters, cancellationToken: cancellationToken)) > 0;
    }

    private static DynamicParameters CreateWritableParameters(Ingredient ingredient)
    {
        var parameters = new DynamicParameters();
        AddWritableParameters(parameters, ingredient);
        return parameters;
    }

    private static void AddWritableParameters(DynamicParameters parameters, Ingredient ingredient)
    {
        parameters.Add("@Name", ingredient.Name, DbType.String);
        parameters.Add("@Description", ingredient.Description, DbType.String);
        parameters.Add("@UnitOfMeasure", ingredient.UnitOfMeasure.ToString(), DbType.String);
        parameters.Add("@CaloriesPerUnit", ingredient.CaloriesPerUnit, DbType.Decimal);
        parameters.Add("@IsAllergen", ingredient.IsAllergen, DbType.Boolean);
        parameters.Add("@UpdatedAt", ingredient.UpdatedAt, DbType.DateTimeOffset);
    }

    private sealed class IngredientRow
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string UnitOfMeasure { get; set; } = null!;

        public decimal CaloriesPerUnit { get; set; }

        public bool IsAllergen { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Ingredient ToIngredient() => new()
        {
            Id = Id,
            Name = Name,
            Description = Description,
            UnitOfMeasure = Enum.Parse<UnitOfMeasure>(UnitOfMeasure),
            CaloriesPerUnit = CaloriesPerUnit,
            IsAllergen = IsAllergen,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }
}
