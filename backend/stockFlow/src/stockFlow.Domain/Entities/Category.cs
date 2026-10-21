using System;
using stockFlow.Domain.Common;

namespace stockFlow.Domain.Entities;

public sealed class Category : BaseEntity
{
    public string Name { get; private set; } = null!;

    private Category() { }

    public Category(string name, DateTime utcNow) : base(utcNow)
    {
        Name = Guard.RequiredText(name, FieldLimits.Name, nameof(name));
    }

    public void Rename(string name, DateTime utcNow)
    {
        var validName = Guard.RequiredText(name, FieldLimits.Name, nameof(name));
        Touch(utcNow);
        Name = validName;
    }
}
