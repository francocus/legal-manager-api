namespace LegalManager.Application.Users.DTOs
{
    public class UpdateLawyerScheduleRequest
    {
        public IReadOnlyList<int>? Days { get; set; }
        public IReadOnlyList<string>? Slots { get; set; }
        public string? Schedule { get; set; }
    }
}
