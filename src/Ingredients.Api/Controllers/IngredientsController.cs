using Ingredients.Application.Contracts;
using Ingredients.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ingredients.Api.Controllers;

[ApiController]
[Route("api/ingredients")]
public sealed class IngredientsController(IIngredientService ingredientService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<IngredientDto>>> GetAll(CancellationToken cancellationToken)
    {
        var ingredients = await ingredientService.GetAllAsync(cancellationToken);
        return Ok(ingredients);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IngredientDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var ingredient = await ingredientService.GetByIdAsync(id, cancellationToken);
        return ingredient is null ? NotFound() : Ok(ingredient);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IngredientDto>> Create(IngredientRequest request, CancellationToken cancellationToken)
    {
        var ingredient = await ingredientService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = ingredient.Id }, ingredient);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IngredientDto>> Update(int id, IngredientRequest request, CancellationToken cancellationToken)
    {
        var ingredient = await ingredientService.UpdateAsync(id, request, cancellationToken);
        return ingredient is null ? NotFound() : Ok(ingredient);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await ingredientService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
