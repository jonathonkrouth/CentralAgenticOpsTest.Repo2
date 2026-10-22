using System.Data;
using Ingredients.Application.Domain;
using Ingredients.Infrastructure.Data;
using Ingredients.Infrastructure.Repositories;
using Ingredients.Infrastructure.Tests.Fakes;
using NSubstitute;

namespace Ingredients.Infrastructure.Tests.Repositories;

public sealed class IngredientRepositoryTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 2, 3, 9, 15, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset UpdatedAt = new(2026, 2, 4, 14, 0, 0, TimeSpan.Zero);

    private readonly FakeDbCommand _command = new();
    private readonly IngredientRepository _repository;

    public IngredientRepositoryTests()
    {
        var connectionFactory = Substitute.For<IDbConnectionFactory>();
        connectionFactory.CreateConnection().Returns(_ => new FakeDbConnection(_command));
        _repository = new IngredientRepository(connectionFactory);
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryRow()
    {
        _command.ReaderResult = CreateResultTable(
            CreateIngredient(1, "Butter", UnitOfMeasure.Tablespoon, description: null),
            CreateIngredient(2, "Whole Milk", UnitOfMeasure.Cup, description: "Pasteurized"));

        var ingredients = await _repository.GetAllAsync();

        Assert.Contains("FROM dbo.Ingredients", _command.CommandText);
        Assert.Contains("ORDER BY Name", _command.CommandText);
        Assert.Collection(
            ingredients,
            first =>
            {
                Assert.Equal(1, first.Id);
                Assert.Equal("Butter", first.Name);
                Assert.Equal(UnitOfMeasure.Tablespoon, first.UnitOfMeasure);
                Assert.Null(first.Description);
            },
            second =>
            {
                Assert.Equal(2, second.Id);
                Assert.Equal(UnitOfMeasure.Cup, second.UnitOfMeasure);
                Assert.Equal("Pasteurized", second.Description);
            });
    }

    [Fact]
    public async Task GetByIdAsync_WhenRowExists_ReturnsMappedIngredient()
    {
        _command.ReaderResult = CreateResultTable(CreateIngredient(7, "Almonds", UnitOfMeasure.Gram, description: "Raw, unsalted"));

        var ingredient = await _repository.GetByIdAsync(7);

        Assert.NotNull(ingredient);
        Assert.Equal(7, ingredient.Id);
        Assert.Equal("Almonds", ingredient.Name);
        Assert.Equal("Raw, unsalted", ingredient.Description);
        Assert.Equal(UnitOfMeasure.Gram, ingredient.UnitOfMeasure);
        Assert.Equal(5.79m, ingredient.CaloriesPerUnit);
        Assert.True(ingredient.IsAllergen);
        Assert.Equal(CreatedAt, ingredient.CreatedAt);
        Assert.Equal(UpdatedAt, ingredient.UpdatedAt);
        Assert.Contains("WHERE Id = @Id", _command.CommandText);
        Assert.Equal(7, _command.Parameters["@Id"].Value);
    }

    [Fact]
    public async Task GetByIdAsync_WhenNoRowExists_ReturnsNull()
    {
        _command.ReaderResult = CreateResultTable();

        var ingredient = await _repository.GetByIdAsync(99);

        Assert.Null(ingredient);
    }

    [Fact]
    public async Task CreateAsync_SendsParametersAndReturnsNewId()
    {
        _command.ScalarResult = 42;

        var id = await _repository.CreateAsync(CreateIngredient(0, "Sea Salt", UnitOfMeasure.Teaspoon, description: null));

        Assert.Equal(42, id);
        Assert.Contains("INSERT INTO dbo.Ingredients", _command.CommandText);
        Assert.Equal("Sea Salt", _command.Parameters["@Name"].Value);
        Assert.Equal(DBNull.Value, _command.Parameters["@Description"].Value);
        Assert.Equal("Teaspoon", _command.Parameters["@UnitOfMeasure"].Value);
        Assert.Equal(5.79m, _command.Parameters["@CaloriesPerUnit"].Value);
        Assert.True(Assert.IsType<bool>(_command.Parameters["@IsAllergen"].Value));
        Assert.Equal(CreatedAt, _command.Parameters["@CreatedAt"].Value);
        Assert.Equal(UpdatedAt, _command.Parameters["@UpdatedAt"].Value);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task UpdateAsync_ReturnsWhetherARowWasUpdated(int rowsAffected, bool expected)
    {
        _command.NonQueryResult = rowsAffected;

        var updated = await _repository.UpdateAsync(CreateIngredient(5, "Honey", UnitOfMeasure.Tablespoon, description: "Wildflower"));

        Assert.Equal(expected, updated);
        Assert.Contains("UPDATE dbo.Ingredients", _command.CommandText);
        Assert.Equal(5, _command.Parameters["@Id"].Value);
        Assert.Equal("Honey", _command.Parameters["@Name"].Value);
        Assert.Equal("Tablespoon", _command.Parameters["@UnitOfMeasure"].Value);
        Assert.False(_command.Parameters.Contains("@CreatedAt"));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public async Task DeleteAsync_ReturnsWhetherARowWasDeleted(int rowsAffected, bool expected)
    {
        _command.NonQueryResult = rowsAffected;

        var deleted = await _repository.DeleteAsync(3);

        Assert.Equal(expected, deleted);
        Assert.Contains("DELETE FROM dbo.Ingredients", _command.CommandText);
        Assert.Equal(3, _command.Parameters["@Id"].Value);
    }

    private static Ingredient CreateIngredient(int id, string name, UnitOfMeasure unitOfMeasure, string? description) => new()
    {
        Id = id,
        Name = name,
        Description = description,
        UnitOfMeasure = unitOfMeasure,
        CaloriesPerUnit = 5.79m,
        IsAllergen = true,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
    };

    private static DataTable CreateResultTable(params Ingredient[] ingredients)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        table.Columns.Add("Name", typeof(string));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("UnitOfMeasure", typeof(string));
        table.Columns.Add("CaloriesPerUnit", typeof(decimal));
        table.Columns.Add("IsAllergen", typeof(bool));
        table.Columns.Add("CreatedAt", typeof(DateTimeOffset));
        table.Columns.Add("UpdatedAt", typeof(DateTimeOffset));

        foreach (var ingredient in ingredients)
        {
            table.Rows.Add(
                ingredient.Id,
                ingredient.Name,
                (object?)ingredient.Description ?? DBNull.Value,
                ingredient.UnitOfMeasure.ToString(),
                ingredient.CaloriesPerUnit,
                ingredient.IsAllergen,
                ingredient.CreatedAt,
                ingredient.UpdatedAt);
        }

        return table;
    }
}
