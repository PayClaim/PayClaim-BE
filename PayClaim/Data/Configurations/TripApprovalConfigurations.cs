using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class TripApprovalConfigurations
    : IEntityTypeConfiguration<TripApproval>
{
    public void Configure(EntityTypeBuilder<TripApproval> builder)
    {
        builder.ToTable("Trip_approvals");

        builder.HasKey(ta => ta.Id);

        builder.Property(ta => ta.ApproverName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ta => ta.Decision)
            .IsRequired();

        builder.Property(ta => ta.Notes)
            .HasMaxLength(500);

        builder.Property(ta => ta.ApprovalLevel)
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(ta => ta.DecidedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(ta => ta.TripProposal)
            .WithMany()
            .HasForeignKey(ta => ta.TripProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ta => ta.Approver)
            .WithMany()
            .HasForeignKey(ta => ta.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}