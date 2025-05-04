namespace ChatSupportSystem.Domain.Common;

public abstract class BaseAuditableEntity<TId> : BaseEntity<TId> where TId : struct
{
    public DateTime Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}

public abstract class BaseAuditableEntity : BaseAuditableEntity<int>
{
}