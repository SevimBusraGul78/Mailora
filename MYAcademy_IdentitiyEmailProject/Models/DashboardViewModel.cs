using MYAcademy_IdentitiyEmailProject.Entities;

namespace MYAcademy_IdentitiyEmailProject.Models
{
    public class DashboardViewModel
    {
        // Selamlama
        public string FullName { get; set; } = "";

        // Üstteki kartlar (animasyonla sayacak)
        public int InboxCount { get; set; }
        public int UnreadCount { get; set; }
        public int SentCount { get; set; }
        public int StarredCount { get; set; }
        public int DraftCount { get; set; }
        public int TrashCount { get; set; }

        // Aktivite haritası: son 91 gün (13 hafta), her gün kaç mesaj
        public DateTime HeatmapStart { get; set; }
        public List<int> HeatmapCounts { get; set; } = new();

        // En çok yazıştığın 5 kişi
        public List<ContactStat> TopContacts { get; set; } = new();

        // Yoğun saatler: günü 3'er saatlik 8 dilime bölüyoruz (00-03, 03-06 ...)
        public List<int> HourlyCounts { get; set; } = new();

        // Kategori grafiği
        public List<string> CategoryNames { get; set; } = new();
        public List<int> CategoryCounts { get; set; } = new();

        // Son gelen 5 mesaj
        public List<Message> RecentMessages { get; set; } = new();
    }

    // "Kiminle kaç mesaj" bilgisini tutan küçük yardımcı sınıf
    public class ContactStat
    {
        public string Email { get; set; } = "";
        public int Count { get; set; }
    }
}