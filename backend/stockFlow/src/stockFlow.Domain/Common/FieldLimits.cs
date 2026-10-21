namespace stockFlow.Domain.Common;

// Agreed proposed limits. Keep future DTO validators and EF mappings in sync.
public static class FieldLimits
{
    public const int Code = 30;
    public const int Sku = 50;
    public const int Name = 100;
    public const int ProductName = 150;
    public const int SupplierName = 150;
    public const int Email = 254;
    public const int Phone = 30;
    public const int Address = 300;
}
