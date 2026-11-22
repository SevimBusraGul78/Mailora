using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentitiyEmailProject.Context;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;

namespace MYAcademy_IdentitiyEmailProject.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardController(EmailContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();
            var email = user.Email!;

            // ---------- 1) KARTLAR ----------
            // Gelen kutusu sorgusu: henüz çalışmadı, sadece hazırlandı
            var inbox = _context.Messages.Where(x => x.ReceiverEmail == email && !x.IsDeleted && !x.IsPurged && !x.IsDraft);

            var model = new DashboardViewModel
            {
                FullName = $"{user.Name} {user.Surname}".Trim(),
                InboxCount = await inbox.CountAsync(),
                UnreadCount = await inbox.CountAsync(x => !x.IsRead),
                StarredCount = await inbox.CountAsync(x => x.IsFavorite),
                SentCount = await _context.Messages.CountAsync(x => x.SenderEmail == email && !x.IsDraft),
                DraftCount = await _context.Messages.CountAsync(x => x.SenderEmail == email && x.IsDraft),
                TrashCount = await _context.Messages.CountAsync(x => x.ReceiverEmail == email && x.IsDeleted && !x.IsPurged),

                RecentMessages = await inbox.Include(x => x.Category)
                                            .OrderByDescending(x => x.SendDate)
                                            .Take(5)
                                            .ToListAsync()
            };

            // ---------- 2) BENİM YAZIŞMALARIM ----------
            // Bana gelen + benim gönderdiğim tüm mesajların sadece 3 bilgisini çekiyoruz
            var myMessages = await _context.Messages
                .Where(x => !x.IsDraft &&
                            ((x.ReceiverEmail == email && !x.IsPurged) || x.SenderEmail == email))
                .Select(x => new { x.SenderEmail, x.ReceiverEmail, x.SendDate })
                .ToListAsync();

            // ---------- 3) AKTİVİTE HARİTASI (son 91 gün) ----------
            model.HeatmapStart = DateTime.Today.AddDays(-90);
            for (int i = 0; i < 91; i++)
            {
                var day = model.HeatmapStart.AddDays(i);
                model.HeatmapCounts.Add(myMessages.Count(m => m.SendDate.Date == day));
            }

            // ---------- 4) EN ÇOK YAZIŞTIKLARIN ----------
            // Mesajı ben attıysam karşı taraf alıcıdır, bana geldiyse gönderendir
            model.TopContacts = myMessages
                .Select(m => m.SenderEmail == email ? m.ReceiverEmail : m.SenderEmail)
                .GroupBy(contact => contact)
                .Select(g => new ContactStat { Email = g.Key, Count = g.Count() })
                .OrderByDescending(c => c.Count)
                .Take(5)
                .ToList();

            // ---------- 5) YOĞUN SAATLER (8 dilim × 3 saat) ----------
            for (int slot = 0; slot < 8; slot++)
                model.HourlyCounts.Add(myMessages.Count(m => m.SendDate.Hour / 3 == slot));

            // ---------- 6) KATEGORİLER ----------
            var byCategory = await inbox.Where(x => x.Category != null)
                                        .GroupBy(x => x.Category!.CategoryName)
                                        .Select(g => new { Name = g.Key, Count = g.Count() })
                                        .OrderByDescending(x => x.Count)
                                        .ToListAsync();

            model.CategoryNames = byCategory.Select(x => x.Name).ToList();
            model.CategoryCounts = byCategory.Select(x => x.Count).ToList();

            return View(model);
        }
    }
}