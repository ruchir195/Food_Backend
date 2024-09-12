using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{

        [Table("tblBooking")]
        public class BookingModel
        {
            [Key]
            public int Id { get; set; }

            [Required]
            [ForeignKey("User")]
            public int UserID { get; set; }

            public User User { get; set; }

            public string BookingType { get; set; }
            
            public DateTime BookingDate { get; set; }

            public DateTime BookingStartDate { get; set; }

            public DateTime? BookingEndDate { get; set; }

            public bool ISBooked { get; set; }

            //public int? CoupenDbId { get; set; }
            //[ForeignKey("CoupenDbId")] 
            //public CoupenDb CoupenDb { get; set; }

        }
}
