using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayClaim.Entities;

namespace PayClaim.Data.Configurations;

public class ProposalParticipantConfigurations
    : IEntityTypeConfiguration<ProposalParticipant>
{
    public void Configure(EntityTypeBuilder<ProposalParticipant> builder)
    {
        builder.ToTable("Proposal_participants");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.IsPic)
            .IsRequired();

        builder.Property(pp => pp.Amount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasOne(pp => pp.TripProposal)
            .WithMany(tp => tp.Participants)
            .HasForeignKey(pp => pp.TripProposalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pp => pp.Employee)
            .WithMany(e => e.ProposalParticipants)
            .HasForeignKey(pp => pp.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}