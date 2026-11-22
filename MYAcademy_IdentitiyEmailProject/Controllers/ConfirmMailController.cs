using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;

namespace MYAcademy_IdentitiyEmailProject.Controllers
{
    public class ConfirmMailController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public ConfirmMailController(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index(string email)
        {
            var model=new ConfirmMailViewModel
            {
            Email = email };
            return View(model); 
        }
        [HttpPost]
        public async Task<IActionResult> Index(ConfirmMailViewModel model)
        {
           
            var user = await _userManager.FindByEmailAsync(model.Email);

           
            if (user == null || user.ValidCode != model.ConfirmCode)
            {
                ModelState.AddModelError("", "Doğrulama kodu hatalı.");
                return View(model);
            }

          
            user.EmailConfirmed = true;
            user.ValidCode = null;            
            await _userManager.UpdateAsync(user);

            
            return RedirectToAction("SignIn", "Login");
        }
    }
}
