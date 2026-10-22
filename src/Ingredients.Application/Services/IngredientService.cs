using Ingredients.Application.Abstractions;
using Ingredients.Application.Contracts;
using Ingredients.Application.Domain;
using Ingredients.Application.Validation;

namespace Ingredients.Application.Services;

public sealed class IngredientService(IIngredientRepository repository, TimeProvider timeProvider) : IIngredientService
{
    public async Task<IReadOnlyList<IngredientDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var ingredients = await repository.GetAllAsync(cancellationToken);
        return ingredients.Select(IngredientDto.FromEntity).ToList();
    }

    public async Task<IngredientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var ingredient = await repository.GetByIdAsync(id, cancellationToken);
        return ingredient is null ? null : IngredientDto.FromEntity(ingredient);
    }

    public async Task<IngredientDto> CreateAsync(IngredientRequest request, CancellationToken cancellationToken = default)
    {
        IngredientRequestValidator.ValidateAndThrow(request);

        var now = timeProvider.GetUtcNow();
        var ingredient = new Ingredient
        {
            Name = request.Name.Trim(),
            Description = NormalizeOptional(request.Description),
            UnitOfMeasure = request.UnitOfMeasure,
            CaloriesPerUnit = request.CaloriesPerUnit,
            IsAllergen = request.IsAllergen,
            CreatedAt = now,
            UpdatedAt = now,
        };

        ingredient.Id = await repository.CreateAsync(ingredient, cancellationToken);
        return IngredientDto.FromEntity(ingredient);
    }

    public async Task<IngredientDto?> UpdateAsync(int id, IngredientRequest request, CancellationToken cancellationToken = default)
    {
        IngredientRequestValidator.ValidateAndThrow(request);

        var ingredient = await repository.GetByIdAsync(id, cancellationToken);
        if (ingredient is null)
        {
            return null;
        }

        ingredient.Name = request.Name.Trim();
        ingredient.Description = NormalizeOptional(request.Description);
        ingredient.UnitOfMeasure = request.UnitOfMeasure;
        ingredient.CaloriesPerUnit = request.CaloriesPerUnit;
        ingredient.IsAllergen = request.IsAllergen;
        ingredient.UpdatedAt = timeProvider.GetUtcNow();

        var updated = await repository.UpdateAsync(ingredient, cancellationToken);
        return updated ? IngredientDto.FromEntity(ingredient) : null;
    }

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(id, cancellationToken);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
