using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class AccountRoleConfigurations
    : IEntityTypeConfiguration<AccountRole>
{
    public void Configure(EntityTypeBuilder<AccountRole> builder)
    {
        builder.ToTable("AccountRoles");

        builder.HasKey(ar => new { ar.RoleId, ar.AccountId });

        builder.Property(ar => ar.AssignedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(ar => ar.Role)
            .WithMany(r => r.AccountRoles)
            .HasForeignKey(ar => ar.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ar => ar.Account)
            .WithMany(a => a.AccountRoles)
            .HasForeignKey(ar => ar.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}