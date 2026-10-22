using Ingredients.Api.Controllers;
using Ingredients.Application.Contracts;
using Ingredients.Application.Domain;
using Ingredients.Application.Services;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace Ingredients.Api.Tests.Controllers;

public sealed class IngredientsControllerTests
{
    private readonly IIngredientService _ingredientService = Substitute.For<IIngredientService>();
    private readonly IngredientsController _controller;

    public IngredientsControllerTests()
    {
        _controller = new IngredientsController(_ingredientService);
    }

    [Fact]
    public async Task GetAll_ReturnsOkWithIngredients()
    {
        IReadOnlyList<IngredientDto> ingredients = [CreateDto(1), CreateDto(2)];
        _ingredientService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(ingredients);

        var result = await _controller.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(ingredients, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenIngredientExists_ReturnsOk()
    {
        var ingredient = CreateDto(3);
        _ingredientService.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns(ingredient);

        var result = await _controller.GetById(3, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(ingredient, ok.Value);
    }

    [Fact]
    public async Task GetById_WhenIngredientDoesNotExist_ReturnsNotFound()
    {
        _ingredientService.GetByIdAsync(3, Arg.Any<CancellationToken>()).Returns((IngredientDto?)null);

        var result = await _controller.GetById(3, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtGetById()
    {
        var request = CreateRequest();
        var created = CreateDto(12);
        _ingredientService.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(created);

        var result = await _controller.Create(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(IngredientsController.GetById), createdResult.ActionName);
        Assert.Equal(12, createdResult.RouteValues?["id"]);
        Assert.Same(created, createdResult.Value);
    }

    [Fact]
    public async Task Update_WhenIngredientExists_ReturnsOk()
    {
        var request = CreateRequest();
        var updated = CreateDto(4);
        _ingredientService.UpdateAsync(4, request, Arg.Any<CancellationToken>()).Returns(updated);

        var result = await _controller.Update(4, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(updated, ok.Value);
    }

    [Fact]
    public async Task Update_WhenIngredientDoesNotExist_ReturnsNotFound()
    {
        var request = CreateRequest();
        _ingredientService.UpdateAsync(4, request, Arg.Any<CancellationToken>()).Returns((IngredientDto?)null);

        var result = await _controller.Update(4, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData(true, typeof(NoContentResult))]
    [InlineData(false, typeof(NotFoundResult))]
    public async Task Delete_ReturnsExpectedResult(bool deleted, Type expectedResultType)
    {
        _ingredientService.DeleteAsync(6, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _controller.Delete(6, CancellationToken.None);

        Assert.IsType(expectedResultType, result);
    }

    private static IngredientRequest CreateRequest() => new("All-Purpose Flour", "Unbleached", UnitOfMeasure.Gram, 3.64m, true);

    private static IngredientDto CreateDto(int id) => new(
        id,
        "All-Purpose Flour",
        "Unbleached",
        UnitOfMeasure.Gram,
        3.64m,
        true,
        DateTimeOffset.UnixEpoch,
        DateTimeOffset.UnixEpoch);
}
