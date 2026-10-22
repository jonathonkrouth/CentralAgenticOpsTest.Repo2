using Ingredients.Application.Domain;

namespace Ingredients.Application.Contracts;

public sealed record IngredientRequest(
    string Name,
    string? Description,
    UnitOfMeasure UnitOfMeasure,
    decimal CaloriesPerUnit,
    bool IsAllergen);
