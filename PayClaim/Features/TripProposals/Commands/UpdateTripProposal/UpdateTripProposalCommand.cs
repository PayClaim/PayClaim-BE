using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.UpdateTripProposal;

public record UpdateTripProposalCommand(Guid ProposalId, CreateTripProposalDto Request, Guid SubmitterEmployeeId);

public class UpdateTripProposalHandler(IUnitOfWork uow, PayClaimDbContext context, IFileStorageService fileStorage)
{
    public async Task<TripProposalDetailDto> Handle(UpdateTripProposalCommand command, CancellationToken ct = default)
    {
        var proposal = await context.TripProposals
            .Include(tp => tp.Participants)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(tp => tp.Id == command.ProposalId, ct);

        if (proposal == null)
        {
            throw new NotFoundException($"Trip proposal with ID '{command.ProposalId}' was not found.");
        }

        var currentPic = proposal.Participants
            .FirstOrDefault(p => p.IsPic);

        if (currentPic == null)
        {
            throw new BusinessRuleException(
                "Trip proposal does not have a PIC.");
        }

        if (currentPic.EmployeeId != command.SubmitterEmployeeId)
        {
            throw new ForbiddenException(
                "Only the PIC can update this trip proposal.");
        }



        if (proposal.Status != TripProposalStatus.NeedsRevision && proposal.Status != TripProposalStatus.Submitted)
        {
            throw new BusinessRuleException(
                $"Cannot update trip proposal with status '{proposal.Status}'. Only proposals in 'NeedsRevision' or 'Submitted' can be modified.");
        }

        var req = command.Request;

        if (req.EndDate < req.StartDate)
        {
            throw new BusinessRuleException("End date cannot be earlier than start date.");
        }

        proposal.Destination = req.Destination;
        proposal.StartDate = req.StartDate;
        proposal.EndDate = req.EndDate;
        proposal.TotalAmount = req.TotalAmount;
        proposal.Description = req.Description;

        if (req.File != null && req.File.Length > 0)
        {
            proposal.FilePath = await fileStorage.SaveFileAsync(req.File, "proposals", ct);
        }

        if (req.Participants != null && req.Participants.Count > 0)
        {

            var newPicCount = req.Participants.Count(p => p.IsPic);

            if (newPicCount != 1)
            {
                throw new BusinessRuleException(
                    "Trip proposal must have exactly one PIC.");
            }

            var newPic = req.Participants.First(p => p.IsPic);

            if (newPic.EmployeeId != command.SubmitterEmployeeId)
            {
                throw new ForbiddenException(
                    "The PIC of this proposal cannot be changed.");
            }

            context.ProposalParticipants.RemoveRange(proposal.Participants);

            proposal.Participants = req.Participants.Select(p => new ProposalParticipant
            {
                TripProposalId = proposal.Id,
                EmployeeId = p.EmployeeId,
                IsPic = p.IsPic,
                Amount = p.Amount
            }).ToList();
        }

        proposal.Status = TripProposalStatus.Submitted;

        uow.TripProposals.Update(proposal);
        await uow.SaveChangesAsync(ct);


        var approvals = await context.TripApprovals
            .Where(a => a.TripProposalId == proposal.Id)
            .OrderBy(a => a.DecidedAt)
            .Select(a => new TripApprovalHistoryDto
            {
                Id = a.Id,
                ApproverId = a.ApproverId,
                ApproverName = a.ApproverName,
                Decision = a.Decision,
                Notes = a.Notes,
                ApprovalLevel = a.ApprovalLevel,
                DecidedAt = a.DecidedAt
            })
            .ToListAsync(ct);

        return new TripProposalDetailDto
        {
            Id = proposal.Id,
            Destination = proposal.Destination,
            StartDate = proposal.StartDate,
            EndDate = proposal.EndDate,
            TotalAmount = proposal.TotalAmount,
            SubmittedAt = proposal.SubmittedAt,
            Description = proposal.Description,
            Status = proposal.Status,
            FilePath = proposal.FilePath,
            Participants = proposal.Participants.Select(p => new ParticipantItemDto
            {
                EmployeeId = p.EmployeeId,
                EmployeeName = p.Employee?.FullName,
                EmployeeNumber = p.Employee?.Number,
                IsPic = p.IsPic,
                Amount = p.Amount
            }).ToList(),
            ApprovalHistory = approvals
        };
    }
}
