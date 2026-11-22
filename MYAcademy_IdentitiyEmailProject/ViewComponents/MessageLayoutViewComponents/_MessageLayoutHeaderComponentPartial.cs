using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentitiyEmailProject.Entities;

namespace MYAcademy_IdentitiyEmailProject.ViewComponents.MessageLayoutViewComponents
{
    public class _MessageLayoutHeaderComponentPartial : ViewComponent
    {
        private readonly UserManager<AppUser> _userManager;

        public _MessageLayoutHeaderComponentPartial(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Giriş yapan kullanıcıyı bul ve sayfaya gönder
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            return View(user);
        }
    }
}