using System;

namespace stockFlow.Domain.Common;

// Shared CLR behavior; not a separate database table or EF inheritance hierarchy.
public abstract class BaseEntity
{
    public Guid Id { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // For EF materialization through derived private constructors.
    protected BaseEntity() { }

    protected BaseEntity(DateTime utcNow)
    {
        CreatedAt = Guard.Utc(utcNow, nameof(utcNow));
        Id = Guid.NewGuid();
    }

    protected void Touch(DateTime utcNow)
    {
        Guard.Utc(utcNow, nameof(utcNow));
        if (utcNow < (UpdatedAt ?? CreatedAt))
            throw new ArgumentOutOfRangeException(nameof(utcNow),
                "Update time cannot precede the latest entity timestamp.");

        UpdatedAt = utcNow;
    }

    public void Activate(DateTime utcNow) => SetActive(true, utcNow);
    public void Deactivate(DateTime utcNow) => SetActive(false, utcNow);

    private void SetActive(bool active, DateTime utcNow)
    {
        Guard.Utc(utcNow, nameof(utcNow));
        if (utcNow < (UpdatedAt ?? CreatedAt))
            throw new ArgumentOutOfRangeException(nameof(utcNow),
                "Update time cannot precede the latest entity timestamp.");

        if (IsActive == active)
            return;

        UpdatedAt = utcNow;
        IsActive = active;
    }
}
