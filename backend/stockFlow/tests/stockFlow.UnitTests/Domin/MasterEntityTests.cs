using System;
using Xunit;
using stockFlow.Domain.Common;
using stockFlow.Domain.Entities;

namespace stockFlow.UnitTests.Domain;

public sealed class MasterEntityTests
{
    private static readonly DateTime Created = new(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Branch_normalizes_code_and_trims_name()
    {
        var branch = new Branch(" cai ", " Cairo ", Created);
        Assert.Equal("CAI", branch.Code);
        Assert.Equal("Cairo", branch.Name);
        Assert.NotEqual(Guid.Empty, branch.Id);
        Assert.True(branch.IsActive);
        Assert.Equal(Created, branch.CreatedAt);
        Assert.Null(branch.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Branch_rejects_blank_names(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Branch("CAI", name!, Created));
    }

    [Fact]
    public void Unit_rejects_names_exceeding_the_limit()
    {
        Assert.Throws<ArgumentException>(() =>
            new Unit("PCS", new string('x', FieldLimits.Name + 1), Created));
    }

    [Fact]
    public void Failed_update_does_not_partially_modify_entity()
    {
        var branch = new Branch("CAI", "Cairo", Created);
        Assert.Throws<ArgumentException>(() =>
            branch.UpdateDetails("GIZ", " ", Created.AddMinutes(1)));
        Assert.Equal("CAI", branch.Code);
        Assert.Equal("Cairo", branch.Name);
        Assert.Null(branch.UpdatedAt);
    }

    [Fact]
    public void Valid_update_preserves_identity_and_creation_time()
    {
        var branch = new Branch("CAI", "Cairo", Created);
        var id = branch.Id;
        var changed = Created.AddMinutes(1);
        branch.UpdateDetails(" cai-01 ", "Cairo Main", changed);
        Assert.Equal(id, branch.Id);
        Assert.Equal(Created, branch.CreatedAt);
        Assert.Equal(changed, branch.UpdatedAt);
        Assert.Equal("CAI-01", branch.Code);
    }

    [Fact]
    public void Deactivation_preserves_entity_and_reactivation_is_possible()
    {
        var category = new Category("Accessories", Created);
        var id = category.Id;
        category.Deactivate(Created.AddMinutes(1));
        Assert.False(category.IsActive);
        Assert.Equal(id, category.Id);
        category.Activate(Created.AddMinutes(2));
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Warehouse_rejects_empty_branch_id()
    {
        Assert.Throws<ArgumentException>(() =>
            new Warehouse(Guid.Empty, "WH-01", "Main", Created));
    }

    [Fact]
    public void Product_rejects_empty_category_or_unit()
    {
        Assert.Throws<ArgumentException>(() =>
            new Product("SKU-1", "Keyboard", Guid.Empty, Guid.NewGuid(), Created));
        Assert.Throws<ArgumentException>(() =>
            new Product("SKU-1", "Keyboard", Guid.NewGuid(), Guid.Empty, Created));
    }

    [Fact]
    public void Product_normalizes_sku_and_requires_valid_new_unit()
    {
        var product = new Product(" acc-001 ", "Keyboard",
            Guid.NewGuid(), Guid.NewGuid(), Created);
        var originalUnit = product.UnitId;
        Assert.Equal("ACC-001", product.Sku);
        Assert.Throws<ArgumentException>(() =>
            product.ChangeBaseUnit(Guid.Empty, Created.AddMinutes(1)));
        Assert.Equal(originalUnit, product.UnitId);
    }

    [Fact]
    public void Supplier_optional_whitespace_becomes_null_and_phone_stays_text()
    {
        var supplier = new Supplier("sup-1", "Supplier", Created,
            email: "  ", phone: " +20 01012345678 ", address: null);
        Assert.Null(supplier.Email);
        Assert.Null(supplier.Address);
        Assert.Equal("+20 01012345678", supplier.Phone);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("Person <sales@example.com>")]
    public void Supplier_rejects_invalid_or_display_name_email(string email)
    {
        Assert.Throws<ArgumentException>(() =>
            new Supplier("SUP-1", "Supplier", Created, email: email));
    }

    [Fact]
    public void Supplier_accepts_plain_email()
    {
        var supplier = new Supplier("SUP-1", "Supplier", Created,
            email: " sales@example.com ");
        Assert.Equal("sales@example.com", supplier.Email);
    }

    [Fact]
    public void Failed_supplier_update_preserves_old_values()
    {
        var supplier = new Supplier("SUP-1", "Old Name", Created);
        Assert.Throws<ArgumentException>(() =>
            supplier.UpdateDetails("SUP-2", "New Name", Created.AddMinutes(1),
                email: "invalid"));
        Assert.Equal("SUP-1", supplier.Code);
        Assert.Equal("Old Name", supplier.Name);
        Assert.Null(supplier.UpdatedAt);
    }

    [Fact]
    public void Timestamps_must_be_utc_and_updates_cannot_move_backwards()
    {
        Assert.Throws<ArgumentException>(() =>
            new Category("Accessories", DateTime.SpecifyKind(Created, DateTimeKind.Unspecified)));

        var category = new Category("Accessories", Created);
        category.Rename("Computers", Created.AddMinutes(2));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            category.Rename("Wrong Time", Created.AddMinutes(1)));
        Assert.Equal("Computers", category.Name);
    }
}
