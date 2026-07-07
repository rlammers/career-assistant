using CareerAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CareerAssistant.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<JobAnalysisResult> JobAnalysisResults => Set<JobAnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Profile
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Summary).IsRequired(false);
            entity.Property(e => e.Skills).IsRequired(false);
            entity.Property(e => e.Experience).IsRequired(false);
        });

        // Configure JobApplication
        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Company).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.JobDescription).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // Configure JobAnalysisResult
        modelBuilder.Entity<JobAnalysisResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MatchScore).IsRequired();
            entity.Property(e => e.MissingSkills).IsRequired(false);
            entity.Property(e => e.Strengths).IsRequired(false);
            entity.Property(e => e.Suggestions).IsRequired(false);
            entity.Property(e => e.CoverLetterDraft).IsRequired(false);

            // Foreign key relationship
            entity.HasOne(e => e.JobApplication)
                .WithMany(j => j.AnalysisResults)
                .HasForeignKey(e => e.JobApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
