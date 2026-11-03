using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class EmployeeBenefitConfigurations
    : IEntityTypeConfiguration<EmployeeBenefit>
{
    public void Configure(EntityTypeBuilder<EmployeeBenefit> builder)
    {
        builder.ToTable("Employee_Benefit");

        builder.HasKey(eb => eb.Id);

        builder.Property(eb => eb.Balance)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(eb => eb.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(eb => eb.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.HasOne(eb => eb.Employee)
            .WithMany()
            .HasForeignKey(eb => eb.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(eb => eb.Benefit)
            .WithMany()
            .HasForeignKey(eb => eb.BenefitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}