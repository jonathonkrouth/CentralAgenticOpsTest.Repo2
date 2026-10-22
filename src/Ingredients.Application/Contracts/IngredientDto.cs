using Ingredients.Application.Domain;

namespace Ingredients.Application.Contracts;

public sealed record IngredientDto(
    int Id,
    string Name,
    string? Description,
    UnitOfMeasure UnitOfMeasure,
    decimal CaloriesPerUnit,
    bool IsAllergen,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static IngredientDto FromEntity(Ingredient ingredient) => new(
        ingredient.Id,
        ingredient.Name,
        ingredient.Description,
        ingredient.UnitOfMeasure,
        ingredient.CaloriesPerUnit,
        ingredient.IsAllergen,
        ingredient.CreatedAt,
        ingredient.UpdatedAt);
}
