namespace LegalManager.Application.Common.Exceptions
{
    // The emailed link does not match any pending token (invalid, expired or already used).
    public class AccountLinkException(string message) : Exception(message)
    {
    }
}
