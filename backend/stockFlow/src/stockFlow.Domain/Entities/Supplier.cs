using System;
using stockFlow.Domain.Common;

namespace stockFlow.Domain.Entities;

public sealed class Supplier : BaseEntity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }

    private Supplier() { }

    public Supplier(string code, string name, DateTime utcNow,
        string? email = null, string? phone = null, string? address = null)
        : base(utcNow)
    {
        Code = Guard.Code(code, FieldLimits.Code, nameof(code));
        Name = Guard.RequiredText(name, FieldLimits.SupplierName, nameof(name));
        Email = Guard.Email(email);
        Phone = Guard.OptionalText(phone, FieldLimits.Phone, nameof(phone));
        Address = Guard.OptionalText(address, FieldLimits.Address, nameof(address));
    }

    public void UpdateDetails(string code, string name, DateTime utcNow,
        string? email = null, string? phone = null, string? address = null)
    {
        var validCode = Guard.Code(code, FieldLimits.Code, nameof(code));
        var validName = Guard.RequiredText(name, FieldLimits.SupplierName, nameof(name));
        var validEmail = Guard.Email(email);
        var validPhone = Guard.OptionalText(phone, FieldLimits.Phone, nameof(phone));
        var validAddress = Guard.OptionalText(address, FieldLimits.Address, nameof(address));

        Touch(utcNow);
        Code = validCode;
        Name = validName;
        Email = validEmail;
        Phone = validPhone;
        Address = validAddress;
    }
}
