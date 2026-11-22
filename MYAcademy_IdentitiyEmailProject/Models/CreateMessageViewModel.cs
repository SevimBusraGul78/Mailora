namespace MYAcademy_IdentitiyEmailProject.Models
{
    public class CreateMessageViewModel
    {
        public string? ReceiverEmail { get; set; }
        public int? CategoryId { get; set; }
        public string? Subject { get; set; }
        public string? MessageDetail { get; set; }
        public int? DraftId { get; set; }


    }
}