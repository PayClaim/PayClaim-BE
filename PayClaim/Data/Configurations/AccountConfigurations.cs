using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class AccountConfigurations : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Username)
            .HasColumnType("varchar(50)")
            .IsRequired();

        builder.HasIndex(a => a.Username)
            .IsUnique();

        builder.Property(a => a.PasswordHash)
            .HasColumnType("varchar(255)")
            .IsRequired();

        builder.Property(a => a.IsActive)
            .IsRequired();

        builder.HasOne(a => a.Employee)
            .WithOne()
            .HasForeignKey<Account>(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}