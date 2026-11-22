namespace MYAcademy_IdentitiyEmailProject.Services
{
    public interface IMailService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody);
    }
}