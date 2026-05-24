using Microsoft.EntityFrameworkCore;

namespace DataLayer.Models
{
    public class DBContext : DbContext
    {
        public DBContext() : base() { }

        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<TelegramSettings> TelegramSettings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }

        public DbSet<BookingData> Bookings { get; set; }
        public DbSet<Telephones> Telephones { get; set; }

        public DbSet<BookingRoom> BookingRooms { get; set; }
        public DbSet<BookingPayment> BookingPayments { get; set; }
        public DbSet<BookingTransportationSeat> BookingTransportationSeats { get; set; }

        public DbSet<BookingAudit> BookingAudits { get; set; }
        public DbSet<BookingAuditDetail> BookingAuditDetails { get; set; }

        public DbSet<TransportationCompany> Companies { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<BusSeat> BusSeats { get; set; }
        public DbSet<BusTrip> BusTrips { get; set; }

        public DbSet<CompanySeatBooking> CompanySeatBookings { get; set; }
        public DbSet<CompanySeatPayment> CompanySeatPayments { get; set; }

        public DbSet<Works> Works { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Decimal precision global ──────────────────────────────
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                var decimalProperties = entity
                    .GetProperties()
                    .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

                foreach (var property in decimalProperties)
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }

            // ── BookingData relations ─────────────────────────────────
            modelBuilder.Entity<BookingData>()
                .HasOne(b => b.User).WithMany()
                .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingData>()
                .HasMany(b => b.PhoneNumbers).WithOne(t => t.Booking)
                .HasForeignKey(t => t.BookingID).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingData>()
                .HasMany(b => b.Rooms).WithOne(r => r.Booking)
                .HasForeignKey(r => r.BookingId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingData>()
                .HasMany(b => b.Payments).WithOne(p => p.Booking)
                .HasForeignKey(p => p.BookingId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingData>()
                .HasMany(b => b.TransportationSeats).WithOne(s => s.Booking)
                .HasForeignKey(s => s.BookingId).OnDelete(DeleteBehavior.Cascade);

            // ── BookingAudit relations ────────────────────────────────
            modelBuilder.Entity<BookingAudit>()
                .HasOne(a => a.Booking).WithMany(b => b.AuditLogs)
                .HasForeignKey(a => a.BookingId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BookingAudit>()
                .HasOne(a => a.ChangedByUser).WithMany()
                .HasForeignKey(a => a.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookingAudit>()
                .HasMany(a => a.Details).WithOne(d => d.Audit)
                .HasForeignKey(d => d.AuditId).OnDelete(DeleteBehavior.Cascade);

            // ── BookingRoom / BookingPayment indexes ──────────────────
            modelBuilder.Entity<BookingRoom>()
                .HasIndex(x => new { x.BookingId, x.RoomType }).IsUnique(false);

            modelBuilder.Entity<BookingPayment>()
                .HasIndex(x => x.BookingId);

            // ── Bus / BusSeat / BusTrip ───────────────────────────────
            modelBuilder.Entity<BusSeat>()
                .HasOne(x => x.Bus).WithMany(x => x.Seats)
                .HasForeignKey(x => x.BusId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BusTrip>()
                .HasOne(x => x.Bus).WithMany(x => x.Trips)
                .HasForeignKey(x => x.BusId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BusTrip>()
                .HasOne(x => x.Company).WithMany()
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<BusTrip>()
                .HasIndex(x => new { x.TripDate, x.Direction, x.BusId });

            modelBuilder.Entity<BookingTransportationSeat>()
                .HasOne(x => x.Trip).WithMany(x => x.ReservedSeats)
                .HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BookingTransportationSeat>()
                .HasIndex(x => new { x.TripId, x.SeatId }).IsUnique();

            // ── CompanySeatBooking ────────────────────────────────────
            modelBuilder.Entity<CompanySeatBooking>()
                .HasOne(x => x.Company).WithMany()
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

            // رحلة الذهاب (Inbound)
            modelBuilder.Entity<CompanySeatBooking>()
                .HasOne(x => x.Trip).WithMany()
                .HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.Restrict);

            // مقعد الذهاب (Inbound)
            modelBuilder.Entity<CompanySeatBooking>()
                .HasOne(x => x.Seat).WithMany()
                .HasForeignKey(x => x.SeatId).OnDelete(DeleteBehavior.Restrict);

            // رحلة العودة (RoundTrip Inbound)
            modelBuilder.Entity<CompanySeatBooking>()
                .HasOne(x => x.ReturnTrip).WithMany()
                .HasForeignKey(x => x.ReturnTripId).OnDelete(DeleteBehavior.Restrict);

            // مقعد العودة (RoundTrip Inbound)
            modelBuilder.Entity<CompanySeatBooking>()
                .HasOne(x => x.ReturnSeat).WithMany()
                .HasForeignKey(x => x.ReturnSeatId).OnDelete(DeleteBehavior.Restrict);

            // Unique: نفس المقعد في نفس رحلة الذهاب لا يتكرر
            modelBuilder.Entity<CompanySeatBooking>()
                .HasIndex(x => new { x.TripId, x.SeatId })
                .IsUnique();
  //              .HasFilter("[TripId] IS NOT NULL AND [SeatId] IS NOT NULL");

            // Unique: نفس المقعد في نفس رحلة العودة لا يتكرر
            modelBuilder.Entity<CompanySeatBooking>()
                .HasIndex(x => new { x.ReturnTripId, x.ReturnSeatId })
                .IsUnique();
//                .HasFilter("[ReturnTripId] IS NOT NULL AND [ReturnSeatId] IS NOT NULL");

            modelBuilder.Entity<CompanySeatBooking>()
                .HasIndex(x => new { x.CompanyId, x.TripId });

            // Index للـ Transfer queries
            modelBuilder.Entity<CompanySeatBooking>()
                .HasIndex(x => x.TransferredFromBookingId);
//                .HasFilter("[TransferredFromBookingId] IS NOT NULL");

            // Index للـ Daily Work queries
            modelBuilder.Entity<CompanySeatBooking>()
                .HasIndex(x => new { x.TripId, x.BookingDirection });

            // ── CompanySeatPayment ────────────────────────────────────
            modelBuilder.Entity<CompanySeatPayment>()
                .HasOne(x => x.Company).WithMany()
                .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CompanySeatPayment>()
                .HasOne(x => x.Trip).WithMany()
                .HasForeignKey(x => x.TripId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CompanySeatPayment>()
                .HasIndex(x => x.CompanyId);

            modelBuilder.Entity<CompanySeatPayment>()
                .HasIndex(x => x.TripId);
        }
    }
}