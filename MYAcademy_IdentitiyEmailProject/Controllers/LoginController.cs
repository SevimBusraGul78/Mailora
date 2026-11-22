using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;

namespace MYAcademy_IdentitiyEmailProject.Controllers
{
    public class LoginController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;
        private readonly UserManager<AppUser> _userManager;

        public LoginController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public IActionResult SignIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SignIn(UserLoginViewModel userLoginViewModel)
        {
            var user = await _userManager.FindByNameAsync(userLoginViewModel.Username)
                       ?? await _userManager.FindByEmailAsync(userLoginViewModel.Username);

            if (user == null || !await _userManager.CheckPasswordAsync(user, userLoginViewModel.Password))
            {
                ModelState.AddModelError("", "Kullanıcı adı / e-posta veya şifre hatalı.");
                return View(userLoginViewModel);
            }

            if (!user.EmailConfirmed)
            {
                return RedirectToAction("Index", "ConfirmMail", new { email = user.Email });
            }

            await _signInManager.SignInAsync(user, false);
            return RedirectToAction("Inbox", "Message");
        }
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("SignIn", "Login");
        }
    }
}
