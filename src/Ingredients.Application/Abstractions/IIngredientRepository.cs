using Ingredients.Application.Domain;

namespace Ingredients.Application.Abstractions;

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(Ingredient ingredient, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Ingredient ingredient, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
