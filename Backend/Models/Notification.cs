using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public string Message { get; set; }
        public int UserId { get; set; }
        public int? MealId { get; set; }
        public DateTime TimeStamp { get; set; }

    }
}
