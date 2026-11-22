using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;

namespace MYAcademy_IdentitiyEmailProject.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IWebHostEnvironment _env;

        private const long MaxPhotoBytes = 2 * 1024 * 1024;   // 2 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public ProfileController(UserManager<AppUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _env = env;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            return View(new ProfileViewModel
            {
                Name = user.Name,
                Surname = user.Surname,
                Description = user.Description,
                UserName = user.UserName,
                Email = user.Email,
                ImageUrl = user.ImageUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ---- 1) Metin alanları ----
            model.Name = model.Name?.Trim();
            model.Surname = model.Surname?.Trim();
            model.Description = model.Description?.Trim();

            if (string.IsNullOrEmpty(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Ad boş olamaz.");
            else if (model.Name.Length > 50)
                ModelState.AddModelError(nameof(model.Name), "Ad en fazla 50 karakter olabilir.");

            if (string.IsNullOrEmpty(model.Surname))
                ModelState.AddModelError(nameof(model.Surname), "Soyad boş olamaz.");
            else if (model.Surname.Length > 50)
                ModelState.AddModelError(nameof(model.Surname), "Soyad en fazla 50 karakter olabilir.");

            if (model.Description?.Length > 300)
                ModelState.AddModelError(nameof(model.Description), "Açıklama en fazla 300 karakter olabilir.");

            // ---- 2) Fotoğraf kontrolleri ----
            if (model.Photo != null)
            {
                var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();

                if (model.Photo.Length == 0)
                    ModelState.AddModelError(nameof(model.Photo), "Seçilen dosya boş.");
                else if (model.Photo.Length > MaxPhotoBytes)
                    ModelState.AddModelError(nameof(model.Photo), "Fotoğraf en fazla 2 MB olabilir.");
                else if (!AllowedExtensions.Contains(ext))
                    ModelState.AddModelError(nameof(model.Photo), "Sadece JPG, PNG veya WEBP yükleyebilirsin.");
                else if (!await IsRealImage(model.Photo))
                    ModelState.AddModelError(nameof(model.Photo), "Bu dosya geçerli bir resim değil.");
            }

            if (!ModelState.IsValid)
            {
                // Sadece gösterilen alanları tekrar doldur
                model.UserName = user.UserName;
                model.Email = user.Email;
                model.ImageUrl = user.ImageUrl;
                return View(model);
            }

            // ---- 3) Kaydet ----
            user.Name = model.Name;
            user.Surname = model.Surname;
            user.Description = model.Description;

            if (model.Photo != null)
            {
                var newUrl = await SavePhoto(model.Photo);
                DeletePhoto(user.ImageUrl);          // Eskisi diskte boşuna yer kaplamasın
                user.ImageUrl = newUrl;
            }
            else if (model.RemovePhoto)
            {
                DeletePhoto(user.ImageUrl);
                user.ImageUrl = null;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                model.UserName = user.UserName;
                model.Email = user.Email;
                model.ImageUrl = user.ImageUrl;
                return View(model);
            }

            TempData["Toast"] = "Profilin güncellendi.";
            return RedirectToAction("Index");
        }

        // Dosyanın ilk byte'larına bakarak gerçekten resim mi kontrol eder
        private static async Task<bool> IsRealImage(IFormFile file)
        {
            var header = new byte[12];
            await using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header, 0, header.Length);
            if (read < 4) return false;

            var isJpg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
            var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            var isWebp = read >= 12
                && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P';

            return isJpg || isPng || isWebp;
        }

        // wwwroot/uploads/profiles/ içine rastgele adla kaydeder
        private async Task<string> SavePhoto(IFormFile file)
        {
            var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";

            await using var fs = new FileStream(Path.Combine(folder, fileName), FileMode.CreateNew);
            await file.CopyToAsync(fs);

            return $"/uploads/profiles/{fileName}";
        }

        // Sadece bizim klasörümüzdeki dosyayı siler
        private void DeletePhoto(string? url)
        {
            if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/profiles/")) return;

            var path = Path.Combine(_env.WebRootPath, "uploads", "profiles", Path.GetFileName(url));
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}