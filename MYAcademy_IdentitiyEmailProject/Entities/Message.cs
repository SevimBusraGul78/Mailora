namespace MYAcademy_IdentitiyEmailProject.Entities
{
    public class Message
    {
        public int MessageId { get; set; }
        public string Subject { get; set; }
        public string MessageDetail { get; set; }
        public string SenderEmail { get; set; }
        public string ReceiverEmail { get; set; }
        public DateTime SendDate { get; set; }
        public bool IsRead { get; set; }
        public bool IsFavorite { get; set; }

        // Taslakta kategori henüz seçilmemiş olabilir, bu yüzden boş geçilebilir (?)
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        // ---- Çöp kutusu (alıcı tarafı) ----
        public bool IsDeleted { get; set; }          // Alıcı çöpe attı
        public DateTime? DeletedDate { get; set; }   // Ne zaman çöpe atıldı
        public bool IsPurged { get; set; }           // Alıcı çöpten de sildi (kalıcı)

        // ---- Taslak (gönderen tarafı) ----
        public bool IsDraft { get; set; }            // Henüz gönderilmedi
    }
}