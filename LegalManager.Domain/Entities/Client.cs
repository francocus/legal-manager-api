namespace LegalManager.Domain.Entities
{
    public class Client : User
    {
        public string? Phone { get; private set; }
        public string? Address { get; private set; }

        private Client()
        {
        }

        public Client(string firstName, string lastName, string dni, string email, string password, string? phone, string? address)
            : base(firstName, lastName, dni, email, password)
        {
            Phone = phone;
            Address = address;
        }

        public void UpdatePhone(string? phone)
        {
            Phone = phone;
        }

        public void UpdateAddress(string? address)
        {
            Address = address;
        }
    }
}