using Microsoft.EntityFrameworkCore;
using RaceDayAPI.Models;

namespace RaceDayAPI.Data
{
    public class RaceDayDbContext : DbContext
    {
        public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Participant> Participants => Set<Participant>();
        public DbSet<Organiser> Organisers => Set<Organiser>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Enrolment> Enrolments => Set<Enrolment>();
        public DbSet<Result> Results => Set<Result>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .ToTable(t => t.HasCheckConstraint("CK_Users_Role", "Role IN ('Organiser','Participant')"));

            modelBuilder.Entity<Participant>()
                .HasIndex(p => p.UserID)
                .IsUnique();

            modelBuilder.Entity<Participant>()
                .HasOne(p => p.User)
                .WithOne(u => u.Participant)
                .HasForeignKey<Participant>(p => p.UserID);

            modelBuilder.Entity<Organiser>()
                .HasIndex(o => o.UserID)
                .IsUnique();

            modelBuilder.Entity<Organiser>()
                .HasOne(o => o.User)
                .WithOne(u => u.Organiser)
                .HasForeignKey<Organiser>(o => o.UserID);

            modelBuilder.Entity<Event>()
                .ToTable(t => t.HasCheckConstraint("CK_Events_EventType", "EventType IN ('Run','Walk','Cycle')"));

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organiser)
                .WithMany(o => o.Events)
                .HasForeignKey(e => e.OrganiserID);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.Event)
                .WithMany(e => e.Categories)
                .HasForeignKey(c => c.EventID);

            modelBuilder.Entity<Enrolment>()
                .HasIndex(en => new { en.ParticipantID, en.EventID })
                .IsUnique();

            modelBuilder.Entity<Enrolment>()
                .ToTable(t => t.HasCheckConstraint("CK_Enrolments_Status", "Status IN ('Pending','Confirmed')"));

            modelBuilder.Entity<Enrolment>()
                .HasOne(en => en.Participant)
                .WithMany(p => p.Enrolments)
                .HasForeignKey(en => en.ParticipantID);

            modelBuilder.Entity<Enrolment>()
                .HasOne(en => en.Event)
                .WithMany(e => e.Enrolments)
                .HasForeignKey(en => en.EventID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrolment>()
                .HasOne(en => en.Category)
                .WithMany(c => c.Enrolments)
                .HasForeignKey(en => en.CategoryID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Result>()
                .HasIndex(r => r.EnrolmentID)
                .IsUnique();

            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithOne(en => en.Result)
                .HasForeignKey<Result>(r => r.EnrolmentID);
        }
    }
}
