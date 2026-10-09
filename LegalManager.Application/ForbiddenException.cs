namespace LegalManager.Application
{
    public class ForbiddenException(string message) : Exception(message)
    {
    }
}