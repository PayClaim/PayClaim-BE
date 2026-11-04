using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.ApproveTripProposal;

public record ApproveTripProposalCommand(Guid ProposalId, TripApprovalActionDto Request, Guid? LoggedInApproverId = null);

public class ApproveTripProposalHandler
{
    private readonly IUnitOfWork _uow;
    private readonly PayClaimDbContext _context;

    public ApproveTripProposalHandler(IUnitOfWork uow, PayClaimDbContext context)
    {
        _uow = uow;
        _context = context;
    }

    public async Task<TripApprovalHistoryDto> Handle(ApproveTripProposalCommand command, CancellationToken ct = default)
    {
        var proposal = await _uow.TripProposals.GetByIdAsync(new object[] { command.ProposalId }, ct);
        if (proposal == null)
        {
            throw new NotFoundException($"Trip proposal with ID '{command.ProposalId}' was not found.");
        }

        if (proposal.Status != TripProposalStatus.Submitted && proposal.Status != TripProposalStatus.UnderReview)
        {
            throw new BusinessRuleException($"Cannot perform approval on a trip proposal that is already '{proposal.Status}'.");
        }

        // Resolusi Approver
        var approverId = command.Request.ApproverId ?? command.LoggedInApproverId;
        Employee? approver = null;

        if (approverId.HasValue)
        {
            approver = await _uow.Employees.GetByIdAsync(new object[] { approverId.Value }, ct);
        }

        if (approver == null)
        {
            // Fallback: Cari Manager atau Employee aktif pertama
            approver = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Position.Title.Contains("Manager") || e.Position.Title.Contains("Lead"), ct)
                ?? await _context.Employees.AsNoTracking().FirstOrDefaultAsync(ct);

            if (approver == null)
            {
                throw new NotFoundException("No eligible approver found in the system.");
            }
        }

        // Update status proposal sesuai keputusan
        proposal.Status = command.Request.Decision switch
        {
            ApprovalDecision.Approved => TripProposalStatus.Approved,
            ApprovalDecision.Rejected => TripProposalStatus.Rejected,
            ApprovalDecision.NeedsRevision => TripProposalStatus.NeedsRevision,
            _ => throw new BusinessRuleException("Invalid approval decision.")
        };

        var approvalRecord = new TripApproval
        {
            TripProposalId = proposal.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = command.Request.Decision,
            Notes = command.Request.Notes,
            ApprovalLevel = 1,
            DecidedAt = DateTime.UtcNow
        };

        await _uow.TripApprovals.AddAsync(approvalRecord, ct);
        _uow.TripProposals.Update(proposal);
        await _uow.SaveChangesAsync(ct);

        return new TripApprovalHistoryDto
        {
            Id = approvalRecord.Id,
            ApproverId = approvalRecord.ApproverId,
            ApproverName = approvalRecord.ApproverName,
            Decision = approvalRecord.Decision,
            Notes = approvalRecord.Notes,
            ApprovalLevel = approvalRecord.ApprovalLevel,
            DecidedAt = approvalRecord.DecidedAt
        };
    }
}
