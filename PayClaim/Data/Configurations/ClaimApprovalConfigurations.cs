using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class ClaimApprovalConfigurations
    : IEntityTypeConfiguration<ClaimApproval>
{
    public void Configure(EntityTypeBuilder<ClaimApproval> builder)
    {
        builder.ToTable("Claim_Approvals");

        builder.HasKey(ca => ca.Id);

        builder.Property(ca => ca.ApproverName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ca => ca.Decision)
            .IsRequired();

        builder.Property(ca => ca.Notes)
            .HasMaxLength(500);

        builder.Property(ca => ca.ApprovalLevel)
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(ca => ca.DecidedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(ca => ca.Claim)
            .WithMany()
            .HasForeignKey(ca => ca.ClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ca => ca.Approver)
            .WithMany()
            .HasForeignKey(ca => ca.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}