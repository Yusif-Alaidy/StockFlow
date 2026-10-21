using System;
using stockFlow.Domain.Common;

namespace stockFlow.Domain.Entities;

public sealed class Product : BaseEntity
{
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public Guid UnitId { get; private set; }

    private Product() { }

    public Product(string sku, string name, Guid categoryId, Guid unitId, DateTime utcNow)
        : base(utcNow)
    {
        Sku = Guard.Code(sku, FieldLimits.Sku, nameof(sku));
        Name = Guard.RequiredText(name, FieldLimits.ProductName, nameof(name));
        CategoryId = Guard.RequiredId(categoryId, nameof(categoryId));
        UnitId = Guard.RequiredId(unitId, nameof(unitId));
    }

    public void UpdateDetails(string sku, string name, Guid categoryId, DateTime utcNow)
    {
        var validSku = Guard.Code(sku, FieldLimits.Sku, nameof(sku));
        var validName = Guard.RequiredText(name, FieldLimits.ProductName, nameof(name));
        var validCategory = Guard.RequiredId(categoryId, nameof(categoryId));

        Touch(utcNow);
        Sku = validSku;
        Name = validName;
        CategoryId = validCategory;
    }

    // Application MUST verify that the product has no document/movement usage
    // before calling this method. Domain cannot query historical database usage.
    public void ChangeBaseUnit(Guid unitId, DateTime utcNow)
    {
        var validUnit = Guard.RequiredId(unitId, nameof(unitId));
        if (validUnit == UnitId)
            return;

        Touch(utcNow);
        UnitId = validUnit;
    }
}
