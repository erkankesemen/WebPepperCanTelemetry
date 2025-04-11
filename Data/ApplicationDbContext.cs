using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebPepperCan.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<VehicleSignal> VehicleSignals {get; set;}
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<TelemetryDevice> TelemetryDevices { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserSession için index tanımlamaları
            builder.Entity<UserSession>()
                .HasIndex(s => s.ConnectionId);

            builder.Entity<UserSession>()
                .HasIndex(s => s.LastActivity);

            builder.Entity<UserSession>()
                .HasIndex(s => new { s.IsActive, s.LastActivity });

            // Vehicle için unique plaka constraint'i
            builder.Entity<Vehicle>()
                .HasIndex(v => v.Plaka)
                .IsUnique();

            // Vehicle - Organization ilişkisi
            builder.Entity<Vehicle>()
                .HasOne(v => v.Organization)
                .WithMany(o => o.Vehicles)
                .HasForeignKey(v => v.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict); // Organizasyon silindiğinde araçlar silinmesin

            // TelemetryDevice için konfigürasyonlar
            builder.Entity<TelemetryDevice>(entity =>
            {
                entity.HasIndex(e => e.SerialNumber).IsUnique();
                
                entity.HasOne(d => d.Organization)
                    .WithMany(o => o.TelemetryDevices)
                    .HasForeignKey(d => d.OrganizationId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Vehicle)
                    .WithOne(v => v.TelemetryDevice)
                    .HasForeignKey<TelemetryDevice>(d => d.VehicleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<VehicleSignal>(entity=>
            {                
                entity.HasOne(c=>c.TelemetryDevice)
                .WithMany(b=>b.VehicleSignals)
                .HasForeignKey(c=>c.TelemetryDeviceId)
                .OnDelete(DeleteBehavior.Restrict);      
            });

            // UserActivity konfigürasyonu
            builder.Entity<UserActivity>(entity =>
            {
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
