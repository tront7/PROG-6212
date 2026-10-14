using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- Roles -----
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(r => r.RoleId);
            entity.HasIndex(r => r.RoleName).IsUnique();
        });

        // ----- Users -----
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.UserId);
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(u => u.RoleId);
        });

        // ----- Events -----
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.EventId);

            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ----- Categories -----
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.CategoryId);

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ----- Enrolments (resolver between User, Event, Category) -----
        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasKey(en => en.EnrolmentId);

            // A participant cannot enrol in the same category twice.
            entity.HasIndex(en => new { en.UserId, en.CategoryId }).IsUnique();

            entity.HasOne(en => en.User)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(en => en.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Event)
                  .WithMany(e => e.Enrolments)
                  .HasForeignKey(en => en.EventId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(en => en.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(en => en.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ----- Results (one-to-one with Enrolment) -----
        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasKey(r => r.ResultId);
            entity.HasIndex(r => r.EnrolmentId).IsUnique();

            entity.HasOne(r => r.Enrolment)
                  .WithOne(en => en.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId);
        });

        // Seed the two fixed roles so registration can assign RoleId immediately.
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Organiser" },
            new Role { RoleId = 2, RoleName = "Participant" }
        );
    }
}
