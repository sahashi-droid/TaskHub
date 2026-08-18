using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskHub.Models;

namespace TaskHub.Data.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.Property(project => project.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(project => project.Description)
            .HasMaxLength(2000);

        builder.Property(project => project.CreatedAtUtc)
            .IsRequired();

        builder.Property(project => project.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne(project => project.Owner)
            .WithMany(user => user.OwnedProjects)
            .HasForeignKey(project => project.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
