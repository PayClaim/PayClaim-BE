
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class BenefitPlanConfigurations
    : IEntityTypeConfiguration<BenefitPlan>
{
    public void Configure(EntityTypeBuilder<BenefitPlan> builder)
    {
        builder.ToTable("Benefit_Plans");

        // Composite Primary Key
        builder.HasKey(bp => new
        {
            bp.DepartmentId,
            bp.BenefitId,
            bp.PositionId
        });

        builder.Property(bp => bp.DepartmentId)
            .HasColumnType("varchar(50)");

        builder.Property(bp => bp.BenefitId)
            .IsRequired();

        builder.Property(bp => bp.PositionId)
            .HasColumnType("varchar(50)");

        builder.Property(bp => bp.LimitAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        // Relationship with Department
        builder.HasOne(bp => bp.Department)
            .WithMany()
            .HasForeignKey(bp => bp.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with Benefit
        builder.HasOne(bp => bp.Benefit)
            .WithMany()
            .HasForeignKey(bp => bp.BenefitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship with Position
        builder.HasOne(bp => bp.Position)
            .WithMany()
            .HasForeignKey(bp => bp.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
