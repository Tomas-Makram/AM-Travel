namespace BusinessLayer.DTOs.Work
{
    public class WorkReportUserDTO
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
    }
}