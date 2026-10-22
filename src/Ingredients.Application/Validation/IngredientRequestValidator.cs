using Ingredients.Application.Contracts;
using Ingredients.Application.Domain;
using Ingredients.Application.Exceptions;

namespace Ingredients.Application.Validation;

internal static class IngredientRequestValidator
{
    private const int NameMaxLength = 150;
    private const int DescriptionMaxLength = 500;

    public static void ValidateAndThrow(IngredientRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }
        else if (request.Name.Trim().Length > NameMaxLength)
        {
            errors[nameof(request.Name)] = [$"Name must be {NameMaxLength} characters or fewer."];
        }

        if (request.Description?.Trim().Length > DescriptionMaxLength)
        {
            errors[nameof(request.Description)] = [$"Description must be {DescriptionMaxLength} characters or fewer."];
        }

        if (!Enum.IsDefined(request.UnitOfMeasure))
        {
            errors[nameof(request.UnitOfMeasure)] =
                [$"Unit of measure must be one of: {string.Join(", ", Enum.GetNames<UnitOfMeasure>())}."];
        }

        if (request.CaloriesPerUnit < 0)
        {
            errors[nameof(request.CaloriesPerUnit)] = ["Calories per unit cannot be negative."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }
    }
}
