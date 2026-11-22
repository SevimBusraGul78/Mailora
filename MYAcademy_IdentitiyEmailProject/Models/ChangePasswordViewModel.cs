namespace MYAcademy_IdentitiyEmailProject.Models
{
    public class ChangePasswordViewModel
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string NewPasswordConfirm { get; set; } = "";
    }
}