namespace LegalManager.Domain.Entities
{
    public class Admin : User
    {
        private Admin()
        {
        }

        public Admin(string firstName, string lastName, string dni, string email, string passwordHash)
            : base(firstName, lastName, dni, email, passwordHash)
        {
        }
    }
}