using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskHub.Models;

namespace TaskHub.Data.Configurations;

public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> builder)
    {
        builder.Property(comment => comment.Content)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(comment => comment.TaskItemId);

        builder.HasOne(comment => comment.TaskItem)
            .WithMany(task => task.Comments)
            .HasForeignKey(comment => comment.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(comment => comment.AuthorUser)
            .WithMany(user => user.TaskComments)
            .HasForeignKey(comment => comment.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
