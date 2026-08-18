using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskHub.Models;

namespace TaskHub.Data.Configurations;

public class ProjectMembershipConfiguration : IEntityTypeConfiguration<ProjectMembership>
{
    public void Configure(EntityTypeBuilder<ProjectMembership> builder)
    {
        builder.Property(membership => membership.Role)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();

        builder.HasIndex(membership => new { membership.ProjectId, membership.UserId })
            .IsUnique();

        builder.HasOne(membership => membership.Project)
            .WithMany(project => project.Memberships)
            .HasForeignKey(membership => membership.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(membership => membership.User)
            .WithMany(user => user.ProjectMemberships)
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
