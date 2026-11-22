using Ganss.Xss;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MYAcademy_IdentitiyEmailProject.Context;
using MYAcademy_IdentitiyEmailProject.Entities;
using MYAcademy_IdentitiyEmailProject.Models;
using MYAcademy_IdentitiyEmailProject.Services;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace MYAcademy_IdentitiyEmailProject.Controllers
{
    [Authorize]
    public class MessageController : Controller
    {
        private readonly EmailContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IMailService _mailService;

        public MessageController(EmailContext context, UserManager<AppUser> userManager, IMailService mailService)
        {
            _context = context;
            _userManager = userManager;
            _mailService = mailService;
        }

        // Gelen kutusunda görünecek mesajlar: bana gelen, çöpte olmayan, taslak olmayan
        private IQueryable<Message> InboxQuery(string email) =>
            _context.Messages
                .Include(x => x.Category)
                .Where(x => x.ReceiverEmail == email && !x.IsDeleted && !x.IsPurged && !x.IsDraft);

        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            var values = InboxQuery(user!.Email!)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            return View(values);
        }

        public async Task<IActionResult> Starred()
        {
            var user = await _userManager.GetUserAsync(User);
            var values = InboxQuery(user!.Email!)
                .Where(x => x.IsFavorite)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            ViewBag.BoxTitle = "Yıldızlı Mesajlar";
            ViewBag.ActiveMenu = "starred";
            return View("Inbox", values);
        }
        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> CreateMessage(int? draftId, int? replyTo, int? forwardOf)
        {
            LoadCategories();

            // Yanıtla / İlet: eski mesajın bilgileriyle formu doldur
            if (replyTo != null || forwardOf != null)
            {
                var me = await _userManager.GetUserAsync(User);
                var original = await FindOwnMessage((replyTo ?? forwardOf)!.Value, me!.Email!);
                if (original == null) return NotFound();

                var tr = new CultureInfo("tr-TR");
                var date = original.SendDate.ToString("d MMMM yyyy HH:mm", tr);
                var quoted = new HtmlSanitizer().Sanitize(original.MessageDetail ?? "");

                if (replyTo != null)
                {
                    // Kendi gönderdiğim mesajı yanıtlıyorsam, alıcı yine karşı taraf olsun
                    var to = original.SenderEmail == me.Email ? original.ReceiverEmail : original.SenderEmail;

                    return View(new CreateMessageViewModel
                    {
                        ReceiverEmail = to,
                        CategoryId = original.CategoryId,
                        Subject = AddPrefix("Re: ", original.Subject),
                        MessageDetail =
                            "<p><br></p>" +
                            $"<p style='color:#85899B'>{date} tarihinde {WebUtility.HtmlEncode(original.SenderEmail)} şunu yazdı:</p>" +
                            $"<blockquote style='border-left:3px solid #D9D3F6;margin:0;padding-left:12px;color:#5D6075'>{quoted}</blockquote>"
                    });
                }

                return View(new CreateMessageViewModel
                {
                    CategoryId = original.CategoryId,
                    Subject = AddPrefix("Fwd: ", original.Subject),
                    MessageDetail =
                        "<p><br></p>" +
                        "<p style='color:#85899B'>---------- İletilen mesaj ----------<br>" +
                        $"Kimden: {WebUtility.HtmlEncode(original.SenderEmail)}<br>" +
                        $"Kime: {WebUtility.HtmlEncode(original.ReceiverEmail)}<br>" +
                        $"Tarih: {date}<br>" +
                        $"Konu: {WebUtility.HtmlEncode(original.Subject)}</p>" +
                        quoted
                });
            }

            if (draftId == null)
                return View(new CreateMessageViewModel());

            // Taslak düzenleniyor: bilgileri forma doldur
            var user = await _userManager.GetUserAsync(User);
            var draft = await FindDraft(draftId.Value, user!.Email!);
            if (draft == null) return NotFound();

            return View(new CreateMessageViewModel
            {
                DraftId = draft.MessageId,
                ReceiverEmail = draft.ReceiverEmail,
                CategoryId = draft.CategoryId,
                Subject = draft.Subject,
                MessageDetail = draft.MessageDetail
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(CreateMessageViewModel model, string? submitAction)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // "Taslak Kaydet" butonuna basıldıysa kontrol yapmadan taslak olarak kaydet
            if (submitAction == "draft")
                return await SaveDraft(model, user);

            // ---- 1) Kontroller ----
            model.ReceiverEmail = model.ReceiverEmail?.Trim();
            model.Subject = model.Subject?.Trim();

            // HTML etiketlerini atıp içeriğin gerçekten boş olup olmadığına bakıyoruz
            var plainText = WebUtility.HtmlDecode(Regex.Replace(model.MessageDetail ?? "", "<[^>]+>", " ")).Trim();

            if (string.IsNullOrEmpty(model.ReceiverEmail))
                ModelState.AddModelError(nameof(model.ReceiverEmail), "Alıcının e-posta adresini yaz.");
            else if (!new EmailAddressAttribute().IsValid(model.ReceiverEmail))
                ModelState.AddModelError(nameof(model.ReceiverEmail), "Geçerli bir e-posta adresi yaz.");
            else if (string.Equals(model.ReceiverEmail, user.Email, StringComparison.OrdinalIgnoreCase))
                ModelState.AddModelError(nameof(model.ReceiverEmail), "Kendine mesaj gönderemezsin 🙂");

            if (model.CategoryId == null)
                ModelState.AddModelError(nameof(model.CategoryId), "Bir kategori seç.");

            if (string.IsNullOrEmpty(model.Subject))
                ModelState.AddModelError(nameof(model.Subject), "Konu boş olamaz.");

            if (string.IsNullOrEmpty(plainText))
                ModelState.AddModelError(nameof(model.MessageDetail), "Mesaj içeriği boş olamaz.");

            if (!ModelState.IsValid)
            {
                LoadCategories();
                return View(model);
            }

            // ---- 2) Zararlı HTML'i temizle ve kaydet ----
            var safeHtml = new HtmlSanitizer().Sanitize(model.MessageDetail!);

            // Taslaktan gönderiliyorsa aynı satırı kullan, değilse yeni satır aç
            Message message;
            if (model.DraftId != null)
            {
                var draft = await FindDraft(model.DraftId.Value, user.Email!);
                if (draft == null) return NotFound();
                message = draft;
            }
            else
            {
                message = new Message();
                _context.Messages.Add(message);
            }

            message.SenderEmail = user.Email!;
            message.ReceiverEmail = model.ReceiverEmail!;
            message.Subject = model.Subject!;
            message.MessageDetail = safeHtml;
            message.CategoryId = model.CategoryId!.Value;
            message.SendDate = DateTime.Now;
            message.IsRead = false;
            message.IsFavorite = false;
            message.IsDraft = false;

            await _context.SaveChangesAsync();

            // ---- 3) Alıcı Mailora'da kayıtlı değilse gerçek adresine mail at ----
            var receiver = await _userManager.FindByEmailAsync(model.ReceiverEmail!);

            if (receiver == null)
            {
                var senderName = WebUtility.HtmlEncode($"{user.Name} {user.Surname}".Trim());
                try
                {
                    await _mailService.SendAsync(
                        model.ReceiverEmail!,
                        model.Subject!,
                        $"<p style='color:#85899B'><b>{senderName}</b> sana Mailora üzerinden bir mesaj gönderdi:</p>" +
                        $"<hr style='border:none;border-top:1px solid #ECEBF5'>" +
                        safeHtml);

                    TempData["Toast"] = "Alıcı Mailora'da kayıtlı değil, mesaj e-posta adresine gönderildi.";
                }
                catch
                {
                    TempData["Toast"] = "Mesaj kaydedildi ama e-posta gönderilemedi.";
                }
            }
            else
            {
                TempData["Toast"] = "Mesajın gönderildi.";
            }

            return RedirectToAction("SendBox");
        }

        public async Task<IActionResult> SendBox()
        {
            var user = await _userManager.GetUserAsync(User);

            var values = _context.Messages
                .Where(x => x.SenderEmail == user!.Email && !x.IsDraft)
                .OrderByDescending(x => x.SendDate)
                .ToList();

            return View(values);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var message = await _context.Messages
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.MessageId == id);

            if (message == null)
                return NotFound();
            // Taslak: sadece yazan görebilir, açınca düzenleme sayfasına gider
            if (message.IsDraft)
            {
                if (message.SenderEmail == user!.Email)
                    return RedirectToAction("CreateMessage", new { draftId = id });
                return NotFound();
            }

            var isReceiver = message.ReceiverEmail == user!.Email;
            var isSender = message.SenderEmail == user.Email;

            // Başkasının mesajı: varlığını bile belli etmiyoruz
            if (!isReceiver && !isSender)
                return NotFound();
            // Alıcı kalıcı sildiyse artık göremez (gönderen hâlâ görebilir)
            if (isReceiver && !isSender && message.IsPurged)
                return NotFound();

            // Alıcı açtıysa otomatik okundu
            if (isReceiver && !message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            ViewBag.IsReceiver = isReceiver;
            return View(message);
        }

        // ---------- Gelen kutusu işlemleri (sayfadaki JavaScript çağırır) ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStar(int id, bool starred)
        {
            var user = await _userManager.GetUserAsync(User);
            var message = await _context.Messages
                .FirstOrDefaultAsync(x => x.MessageId == id && x.ReceiverEmail == user!.Email);

            if (message == null) return NotFound();

            message.IsFavorite = starred;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarkAsRead(List<int> ids) => SetRead(ids, true);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> MarkAsUnread(List<int> ids) => SetRead(ids, false);



        // Sadece giriş yapan kullanıcıya gelen mesajlar değişir
        private async Task<IActionResult> SetRead(List<int> ids, bool isRead)
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = await _context.Messages
                .Where(x => ids.Contains(x.MessageId) && x.ReceiverEmail == user!.Email)
                .ToListAsync();

            foreach (var m in messages)
                m.IsRead = isRead;

            await _context.SaveChangesAsync();
            return Ok();
        }
        // ---------- Çöp kutusu ----------

        // Gelen kutusundaki 🗑 butonu: seçilenleri çöpe at
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(List<int> ids)
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = await _context.Messages
                .Where(x => ids.Contains(x.MessageId) && x.ReceiverEmail == user!.Email && !x.IsDeleted)
                .ToListAsync();

            foreach (var m in messages)
            {
                m.IsDeleted = true;
                m.DeletedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        public async Task<IActionResult> Trash()
        {
            var user = await _userManager.GetUserAsync(User);
            var values = await _context.Messages
                .Include(x => x.Category)
                .Where(x => x.ReceiverEmail == user!.Email && x.IsDeleted && !x.IsPurged)
                .OrderByDescending(x => x.DeletedDate)
                .ToListAsync();

            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Restore(int id)
        {
            var m = await FindInTrash(id);
            if (m == null) return NotFound();

            m.IsDeleted = false;
            m.DeletedDate = null;
            await _context.SaveChangesAsync();

            TempData["Toast"] = "Mesaj gelen kutusuna geri taşındı.";
            return RedirectToAction("Trash");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Purge(int id)
        {
            var m = await FindInTrash(id);
            if (m == null) return NotFound();

            m.IsPurged = true;   // Satırı silmiyoruz: gönderen hâlâ görebilsin
            await _context.SaveChangesAsync();

            TempData["Toast"] = "Mesaj kalıcı olarak silindi.";
            return RedirectToAction("Trash");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmptyTrash()
        {
            var user = await _userManager.GetUserAsync(User);
            var messages = await _context.Messages
                .Where(x => x.ReceiverEmail == user!.Email && x.IsDeleted && !x.IsPurged)
                .ToListAsync();

            foreach (var m in messages)
                m.IsPurged = true;

            await _context.SaveChangesAsync();

            TempData["Toast"] = "Çöp kutusu boşaltıldı.";
            return RedirectToAction("Trash");
        }

        // Sadece bana ait ve çöpteki bir mesajı bulur
        private async Task<Message?> FindInTrash(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            return await _context.Messages.FirstOrDefaultAsync(x =>
                x.MessageId == id && x.ReceiverEmail == user!.Email && x.IsDeleted && !x.IsPurged);
        }
        // ---------- Taslaklar ----------
        public async Task<IActionResult> Drafts()
        {
            var user = await _userManager.GetUserAsync(User);
            var values = await _context.Messages
                .Where(x => x.SenderEmail == user!.Email && x.IsDraft)
                .OrderByDescending(x => x.SendDate)
                .ToListAsync();

            return View(values);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDraft(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var draft = await FindDraft(id, user!.Email!);
            if (draft == null) return NotFound();

            _context.Messages.Remove(draft);   // Taslağı sadece yazan görür: gerçekten silebiliriz
            await _context.SaveChangesAsync();

            TempData["Toast"] = "Taslak silindi.";
            return RedirectToAction("Drafts");
        }

        private async Task<IActionResult> SaveDraft(CreateMessageViewModel model, AppUser user)
        {
            Message draft;
            if (model.DraftId != null)
            {
                var existing = await FindDraft(model.DraftId.Value, user.Email!);
                if (existing == null) return NotFound();
                draft = existing;
            }
            else
            {
                draft = new Message { SenderEmail = user.Email!, IsDraft = true };
                _context.Messages.Add(draft);
            }

            draft.ReceiverEmail = model.ReceiverEmail?.Trim() ?? "";
            draft.Subject = model.Subject?.Trim() ?? "";
            draft.MessageDetail = new HtmlSanitizer().Sanitize(model.MessageDetail ?? "");
            draft.CategoryId = model.CategoryId;
            draft.SendDate = DateTime.Now;   // Taslakta "son düzenleme" zamanı

            await _context.SaveChangesAsync();

            TempData["Toast"] = "Taslak kaydedildi.";
            return RedirectToAction("Drafts");
        }

        // Sadece bana ait bir taslağı bulur
        private Task<Message?> FindDraft(int id, string email) =>
            _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && x.SenderEmail == email && x.IsDraft);

        // Bana ait (bana gelen ya da benim gönderdiğim) bir mesajı bulur
        private Task<Message?> FindOwnMessage(int id, string email) =>
            _context.Messages.FirstOrDefaultAsync(x => x.MessageId == id && !x.IsDraft &&
                ((x.ReceiverEmail == email && !x.IsPurged) || x.SenderEmail == email));

        // "Re: Re: Re: ..." olmasın diye önek zaten varsa tekrar eklemiyoruz
        private static string AddPrefix(string prefix, string? subject)
        {
            subject ??= "";
            return subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? subject : prefix + subject;
        }

        // Kategori açılır listesini doldurur (GET ve hatalı POST'ta lazım)
        private void LoadCategories()
        {
            ViewBag.Categories = new SelectList(_context.Categories.ToList(), "CategoryId", "CategoryName");
        }
    }
}