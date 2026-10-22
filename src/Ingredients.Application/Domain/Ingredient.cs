namespace Ingredients.Application.Domain;

public sealed class Ingredient
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public UnitOfMeasure UnitOfMeasure { get; set; }

    public decimal CaloriesPerUnit { get; set; }

    public bool IsAllergen { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
