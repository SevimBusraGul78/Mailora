using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;
using MYAcademy_IdentitiyEmailProject.Services;

namespace MYAcademy_IdentitiyEmailProject.Controllers
{
    public class RegisterController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mailService;

        public RegisterController(UserManager<AppUser> userManager, IMailService mailService)
        {
            _userManager = userManager;
            _mailService = mailService;
        }

        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignUp(UserRegisterViewModel userRegisterViewModel)
        {
            int validCode = Random.Shared.Next(100000, 1000000);

            AppUser appuser = new AppUser()
            {
                Name = userRegisterViewModel.Name,
                Surname = userRegisterViewModel.Surname,
                Email = userRegisterViewModel.Email,
                UserName = userRegisterViewModel.UserName,
                ValidCode = validCode,
            };

            var result = await _userManager.CreateAsync(appuser, userRegisterViewModel.Password);

            if (result.Succeeded)
            {
                await _mailService.SendAsync(
                    userRegisterViewModel.Email,
                    "Mailora doğrulama kodun",
                    $"<p>Merhaba {userRegisterViewModel.Name},</p>" +
                    $"<p>Hesabını doğrulamak için kodun:</p>" +
                    $"<h2 style='letter-spacing:6px;color:#7467C7'>{validCode}</h2>");

                return RedirectToAction("Index", "ConfirmMail", new { email = userRegisterViewModel.Email });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(userRegisterViewModel);
        }
    }
}