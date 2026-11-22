using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;
using MYAcademy_IdentitiyEmailProject.Services;
using Microsoft.AspNetCore.Authorization;
namespace MYAcademy_IdentitiyEmailProject.Controllers
{
    public class PasswordController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IMailService _mailService;

        public PasswordController(UserManager<AppUser> userManager, IMailService mailService)
        {
            _userManager = userManager;
            _mailService = mailService;
        }

        // ---------- 1) Şifremi unuttum: e-posta iste ----------
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError("", "E-posta adresini yazmalısın.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Kullanıcı varsa bağlantı gönder
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var link = Url.Action("ResetPassword", "Password",
                    new { userId = user.Id, token = token }, Request.Scheme);

                await _mailService.SendAsync(
                    user.Email!,
                    "Mailora şifre yenileme",
                    $"<p>Merhaba {user.Name},</p>" +
                    $"<p>Şifreni yenilemek için aşağıdaki butona tıkla. Bağlantı <b>1 saat</b> geçerli ve tek kullanımlık.</p>" +
                    $"<p><a href='{link}' style='display:inline-block;padding:12px 22px;border-radius:12px;background:#7467C7;color:#fff;text-decoration:none;font-weight:bold'>Şifremi Yenile</a></p>" +
                    $"<p style='color:#85899B;font-size:12px'>Bu isteği sen yapmadıysan bu maili görmezden gelebilirsin.</p>");
            }

            // Kullanıcı olsa da olmasa da aynı ekranı gösteriyoruz
            TempData["SentTo"] = model.Email;
            return RedirectToAction("ForgotPasswordSent");
        }

        public IActionResult ForgotPasswordSent()
        {
            ViewBag.Email = TempData["SentTo"] as string;
            return View();
        }

        // ---------- 2) Maildeki bağlantı: yeni şifre iste ----------
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string userId, string token)
        {
            // Bağlantı geçerli mi? Değilse "geçersiz bağlantı" ekranı
            if (!await IsTokenValid(userId, token))
            {
                return View("ResetPasswordInvalid");
            }

            return View(new ResetPasswordViewModel { UserId = userId, Token = token });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (model.Password != model.PasswordConfirm)
            {
                ModelState.AddModelError("", "Şifreler birbirini tutmuyor.");
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return View("ResetPasswordInvalid");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                TempData["Success"] = "Şifren yenilendi. Yeni şifrenle giriş yapabilirsin.";
                return RedirectToAction("SignIn", "Login");
            }

            // Token bu arada geçersiz olduysa (süre doldu / zaten kullanıldı)
            if (result.Errors.Any(e => e.Code == "InvalidToken"))
            {
                return View("ResetPasswordInvalid");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        // Bağlantıdaki token hâlâ geçerli mi?
        // ---------- 3) Şifre değiştirme (giriş yapmış kullanıcı) ----------
        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (model.NewPassword != model.NewPasswordConfirm)
            {
                ModelState.AddModelError("", "Yeni şifreler birbirini tutmuyor.");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Şifre değişince güvenlik damgası değişir; oturumu tazeliyoruz ki kullanıcı dışarı atılmasın
                await _signInManager.RefreshSignInAsync(user);

                // Güvenlik bildirimi
                await _mailService.SendAsync(
                    user.Email!,
                    "Mailora şifren değiştirildi",
                    $"<p>Merhaba {user.Name},</p>" +
                    "<p>Hesabının şifresi az önce değiştirildi.</p>" +
                    "<p style='color:#C2476B'>Bu işlemi sen yapmadıysan hemen <b>Şifremi Unuttum</b> ile şifreni yenile.</p>");

                TempData["Success"] = "Şifren değiştirildi. Güvenlik için mailine de bilgi gönderdik.";
                return RedirectToAction("ChangePassword");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }
        private async Task<bool> IsTokenValid(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                return false;

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return false;

            return await _userManager.VerifyUserTokenAsync(
                user,
                _userManager.Options.Tokens.PasswordResetTokenProvider,
                UserManager<AppUser>.ResetPasswordTokenPurpose,
                token);
        }
    }
}