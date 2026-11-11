namespace LegalManager.Application.Common.Interfaces
{
    public interface ICurrentUser
    {
        Guid Id { get; }

        string Role { get; }

        bool IsAdmin { get; }

        bool IsLawyer { get; }

        bool IsClient { get; }
    }
}