using System;
using stockFlow.Domain.Common;

namespace stockFlow.Domain.Entities;

public sealed class Warehouse : BaseEntity
{
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    private Warehouse() { }

    public Warehouse(Guid branchId, string code, string name, DateTime utcNow) : base(utcNow)
    {
        BranchId = Guard.RequiredId(branchId, nameof(branchId));
        Code = Guard.Code(code, FieldLimits.Code, nameof(code));
        Name = Guard.RequiredText(name, FieldLimits.Name, nameof(name));
    }

    // Reparenting is intentionally not exposed by this starter.
    public void UpdateDetails(string code, string name, DateTime utcNow)
    {
        var validCode = Guard.Code(code, FieldLimits.Code, nameof(code));
        var validName = Guard.RequiredText(name, FieldLimits.Name, nameof(name));

        Touch(utcNow);
        Code = validCode;
        Name = validName;
    }
}
