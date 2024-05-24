namespace Backend.Dto
{
    public class BookingDTO
    {
        public int UserID { get; set; }

        public string Category { get; set; }

        public string BookingType { get; set; }

        public string CupponID { get; set; }
        public DateTime BookingDate { get; set; }

        public DateTime BookingStartDate { get; set; }

        public DateTime BookingEndDate { get; set; }

        public bool ISBooked { get; set; }
    }
}
