using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Backend.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> option) : base(option)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<CoupenDb> CoupenDbs { get; set; }       
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BookingModel> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<CoupenDb>().ToTable("coupen");
            modelBuilder.Entity<Notification>().ToTable("notification");
            modelBuilder.Entity<BookingModel>().ToTable("booking");
        }
    }
}
