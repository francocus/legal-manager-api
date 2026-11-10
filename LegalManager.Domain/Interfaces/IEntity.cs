namespace LegalManager.Domain.Interfaces
{
    public interface IEntity
    {
        Guid Id { get; }
        bool Active { get; }
    }
}
