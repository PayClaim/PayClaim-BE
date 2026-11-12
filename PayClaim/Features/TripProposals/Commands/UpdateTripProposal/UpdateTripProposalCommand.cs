using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.UpdateTripProposal;

public record UpdateTripProposalCommand(Guid ProposalId, CreateTripProposalDto Request, Guid? SubmitterEmployeeId = null) : IRequest<TripProposalDetailDto>;

public class UpdateTripProposalHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IFileStorageService fileStorage,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<UpdateTripProposalCommand, TripProposalDetailDto>
{
    public async Task<TripProposalDetailDto> Handle(UpdateTripProposalCommand command, CancellationToken ct = default)
    {
        var submitterId = command.SubmitterEmployeeId ?? currentUser.RequiredEmployeeId;

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

        if (currentPic.EmployeeId != submitterId)
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

        TripProposalParticipantParser.EnsureParticipants(req, httpContextAccessor.HttpContext?.Request);

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

            if (newPic.EmployeeId != submitterId)
            {
                throw new ForbiddenException(
                    "The PIC of this proposal cannot be changed.");
            }

            if (req.Participants.GroupBy(p => p.EmployeeId).Any(g => g.Count() > 1))
            {
                throw new BusinessRuleException("Duplicate participant detected in proposal.");
            }

            var totalParticipantsAmount = req.Participants.Sum(p => p.Amount);
            if (totalParticipantsAmount != req.TotalAmount)
            {
                throw new BusinessRuleException(
                    $"Sum of participants' budgets ({totalParticipantsAmount:N0}) does not match the proposal total amount ({req.TotalAmount:N0}).");
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
        else
        {
            var existingTotal = proposal.Participants.Sum(p => p.Amount);
            if (existingTotal != req.TotalAmount)
            {
                throw new BusinessRuleException(
                    $"Proposal total amount ({req.TotalAmount:N0}) does not match existing participants' budgets ({existingTotal:N0}).");
            }
        }

        // Perbarui waktu submission ketika proposal diajukan ulang.
        proposal.SubmittedAt = DateTime.UtcNow.AddHours(7);

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
