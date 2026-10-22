using Ingredients.Application.Contracts;

namespace Ingredients.Application.Services;

public interface IIngredientService
{
    Task<IReadOnlyList<IngredientDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IngredientDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IngredientDto> CreateAsync(IngredientRequest request, CancellationToken cancellationToken = default);

    Task<IngredientDto?> UpdateAsync(int id, IngredientRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
