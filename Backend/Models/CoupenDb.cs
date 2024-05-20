namespace Backend.Models
{
    public class CoupenDb
    {
        public int Id { get; set; }
        public string? coupenCode { get; set; }
        public DateTime? createdTime { get; set; }
        public DateTime? ExpirationTime { get; set; }
        public int userID { get; set; }
    }
}
