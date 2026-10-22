using Ingredients.Application.Abstractions;
using Ingredients.Application.Contracts;
using Ingredients.Application.Domain;
using Ingredients.Application.Exceptions;
using Ingredients.Application.Services;
using NSubstitute;

namespace Ingredients.Application.Tests.Services;

public sealed class IngredientServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 4, 2, 10, 30, 0, TimeSpan.Zero);

    private readonly IIngredientRepository _repository = Substitute.For<IIngredientRepository>();
    private readonly IngredientService _service;

    public IngredientServiceTests()
    {
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(Now);
        _service = new IngredientService(_repository, timeProvider);
    }

    public static TheoryData<IngredientRequest, string> InvalidRequests => new()
    {
        { ValidRequest() with { Name = "" }, nameof(IngredientRequest.Name) },
        { ValidRequest() with { Name = new string('a', 151) }, nameof(IngredientRequest.Name) },
        { ValidRequest() with { Description = new string('a', 501) }, nameof(IngredientRequest.Description) },
        { ValidRequest() with { UnitOfMeasure = default }, nameof(IngredientRequest.UnitOfMeasure) },
        { ValidRequest() with { UnitOfMeasure = (UnitOfMeasure)99 }, nameof(IngredientRequest.UnitOfMeasure) },
        { ValidRequest() with { CaloriesPerUnit = -0.5m }, nameof(IngredientRequest.CaloriesPerUnit) },
    };

    [Fact]
    public async Task GetAllAsync_ReturnsMappedIngredients()
    {
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([CreateIngredient(1, "Butter"), CreateIngredient(2, "Whole Milk")]);

        var ingredients = await _service.GetAllAsync();

        Assert.Collection(
            ingredients,
            first => Assert.Equal("Butter", first.Name),
            second => Assert.Equal("Whole Milk", second.Name));
    }

    [Fact]
    public async Task GetByIdAsync_WhenIngredientExists_ReturnsDto()
    {
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(CreateIngredient(3, "Almonds"));

        var ingredient = await _service.GetByIdAsync(3);

        Assert.NotNull(ingredient);
        Assert.Equal(3, ingredient.Id);
        Assert.Equal("Almonds", ingredient.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIngredientDoesNotExist_ReturnsNull()
    {
        _repository.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((Ingredient?)null);

        var ingredient = await _service.GetByIdAsync(3);

        Assert.Null(ingredient);
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_PersistsNormalizedIngredient()
    {
        _repository.CreateAsync(Arg.Any<Ingredient>(), Arg.Any<CancellationToken>()).Returns(10);

        var ingredient = await _service.CreateAsync(ValidRequest() with { Name = "  Sea Salt  ", Description = "" });

        Assert.Equal(10, ingredient.Id);
        Assert.Equal("Sea Salt", ingredient.Name);
        Assert.Null(ingredient.Description);
        Assert.Equal(UnitOfMeasure.Gram, ingredient.UnitOfMeasure);
        Assert.Equal(Now, ingredient.CreatedAt);
        Assert.Equal(Now, ingredient.UpdatedAt);
        await _repository.Received(1).CreateAsync(
            Arg.Is<Ingredient>(i => i.Name == "Sea Salt" && i.Description == null && i.CreatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task CreateAsync_WithInvalidRequest_ThrowsValidationException(IngredientRequest request, string expectedErrorKey)
    {
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(request));

        Assert.Contains(expectedErrorKey, exception.Errors.Keys);
        await _repository.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WhenIngredientExists_UpdatesFieldsAndPreservesCreatedAt()
    {
        var createdAt = Now.AddDays(-14);
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(CreateIngredient(5, "Old Name", createdAt));
        _repository.UpdateAsync(Arg.Any<Ingredient>(), Arg.Any<CancellationToken>()).Returns(true);

        var ingredient = await _service.UpdateAsync(
            5,
            ValidRequest() with { Name = "Brown Sugar", UnitOfMeasure = UnitOfMeasure.Cup, IsAllergen = false });

        Assert.NotNull(ingredient);
        Assert.Equal("Brown Sugar", ingredient.Name);
        Assert.Equal(UnitOfMeasure.Cup, ingredient.UnitOfMeasure);
        Assert.False(ingredient.IsAllergen);
        Assert.Equal(createdAt, ingredient.CreatedAt);
        Assert.Equal(Now, ingredient.UpdatedAt);
        await _repository.Received(1).UpdateAsync(
            Arg.Is<Ingredient>(i => i.Id == 5 && i.Name == "Brown Sugar" && i.UpdatedAt == Now),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenIngredientDoesNotExist_ReturnsNullWithoutUpdating()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Ingredient?)null);

        var ingredient = await _service.UpdateAsync(5, ValidRequest());

        Assert.Null(ingredient);
        await _repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ThrowsBeforeLoadingIngredient()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(5, ValidRequest() with { UnitOfMeasure = (UnitOfMeasure)42 }));

        await _repository.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAsync_ReturnsRepositoryResult(bool deleted)
    {
        _repository.DeleteAsync(8, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _service.DeleteAsync(8);

        Assert.Equal(deleted, result);
    }

    private static IngredientRequest ValidRequest() => new("All-Purpose Flour", "Unbleached", UnitOfMeasure.Gram, 3.64m, true);

    private static Ingredient CreateIngredient(int id, string name, DateTimeOffset? createdAt = null) => new()
    {
        Id = id,
        Name = name,
        UnitOfMeasure = UnitOfMeasure.Gram,
        CaloriesPerUnit = 3.64m,
        IsAllergen = true,
        CreatedAt = createdAt ?? Now,
        UpdatedAt = createdAt ?? Now,
    };
}
