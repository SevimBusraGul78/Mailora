namespace MYAcademy_IdentitiyEmailProject.Models
{
    public class ProfileViewModel
    {
        public string? Name { get; set; }
        public string? Surname { get; set; }
        public string? Description { get; set; }

        // Fotoğraf
        public IFormFile? Photo { get; set; }      // Yeni yüklenen dosya
        public bool RemovePhoto { get; set; }      // "Fotoğrafı kaldır" seçildi mi

        // Sadece gösterim için (formdan değiştirilemez)
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? ImageUrl { get; set; }
    }
}

