using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class TripProposalConfigurations
    : IEntityTypeConfiguration<TripProposal>
{
    public void Configure(EntityTypeBuilder<TripProposal> builder)
    {
        builder.ToTable("Trip_proposals");

        builder.HasKey(tp => tp.Id);

        builder.Property(tp => tp.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(tp => tp.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(tp => tp.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(tp => tp.SubmittedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(tp => tp.Description)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(tp => tp.Status)
            .IsRequired();

        builder.Property(tp => tp.FilePath)
            .HasColumnType("varchar(500)")
            .IsRequired();
    }
}