using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class ClaimConfigurations : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ClaimType)
            .IsRequired();

        builder.Property(c => c.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.TransactionDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(c => c.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.SubmittedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.FilePath)
            .HasColumnType("varchar(500)")
            .IsRequired();

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("IX_Claims_Status");

        builder.HasIndex(c => c.SubmittedAt)
            .HasDatabaseName("IX_Claims_SubmittedAt");

        builder.HasIndex(c => c.TransactionDate)
            .HasDatabaseName("IX_Claims_TransactionDate");

        builder.HasIndex(c => new { c.Status, c.SubmittedAt })
            .HasDatabaseName("IX_Claims_Status_SubmittedAt");

        builder.HasIndex(c => new { c.EmployeeId, c.SubmittedAt })
            .HasDatabaseName("IX_Claims_EmployeeId_SubmittedAt");

        builder.HasOne(c => c.Employee)
            .WithMany()
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.EmployeeBenefit)
            .WithMany()
            .HasForeignKey(c => c.EmployeeBenefitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.TripProposal)
            .WithMany()
            .HasForeignKey(c => c.TripProposalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
