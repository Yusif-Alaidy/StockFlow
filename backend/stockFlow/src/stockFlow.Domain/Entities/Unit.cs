using System;
using stockFlow.Domain.Common;

namespace stockFlow.Domain.Entities;

public sealed class Unit : BaseEntity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    private Unit() { }

    public Unit(string code, string name, DateTime utcNow) : base(utcNow)
    {
        Code = Guard.Code(code, FieldLimits.Code, nameof(code));
        Name = Guard.RequiredText(name, FieldLimits.Name, nameof(name));
    }

    public void UpdateDetails(string code, string name, DateTime utcNow)
    {
        // Validate every field before changing the object.
        var validCode = Guard.Code(code, FieldLimits.Code, nameof(code));
        var validName = Guard.RequiredText(name, FieldLimits.Name, nameof(name));

        Touch(utcNow);
        Code = validCode;
        Name = validName;
    }
}
