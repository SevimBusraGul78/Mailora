using Microsoft.AspNetCore.Mvc;
using MYAcademy_IdentitiyEmailProject.Context;
using System.Security.Claims;

namespace MYAcademy_IdentitiyEmailProject.ViewComponents.MessageLayoutViewComponents
{
    public class _MessageLayoutSidebarComponentPartial : ViewComponent
    {
        private readonly EmailContext _context;

        public _MessageLayoutSidebarComponentPartial(EmailContext context)
        {
            _context = context;
        }

        // Kenar çubuğundaki "Etiketler" artık veritabanındaki kategorilerden geliyor
        public IViewComponentResult Invoke()
        {
            var categories = _context.Categories
                .OrderBy(x => x.CategoryId)
                .ToList();

            // Giriş yapan kullanıcının taslak sayısı
            var email = UserClaimsPrincipal.FindFirstValue(ClaimTypes.Email);
            ViewBag.DraftCount = string.IsNullOrEmpty(email)
                ? 0
                : _context.Messages.Count(x => x.SenderEmail == email && x.IsDraft);

            return View(categories);
        }
    }
}

