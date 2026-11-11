using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Constants;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.ApproveTripProposal;

public record ApproveTripProposalCommand(
    Guid ProposalId,
    TripApprovalActionDto Request,
    Guid? LoggedInApproverId = null,
    string[]? ApproverRoles = null
) : IRequest<TripApprovalHistoryDto>;

public class ApproveTripProposalHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IEmailService emailService,
    ICurrentUser currentUser
) : IRequestHandler<ApproveTripProposalCommand, TripApprovalHistoryDto>
{
    public async Task<TripApprovalHistoryDto> Handle(
        ApproveTripProposalCommand command,
        CancellationToken ct = default)
    {
        var approverId =
            command.LoggedInApproverId ?? currentUser.RequiredEmployeeId;

        var approverRoles =
            command.ApproverRoles ?? currentUser.Roles;

        var proposal = await context.TripProposals
            .Include(tp => tp.Participants)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(
                tp => tp.Id == command.ProposalId,
                ct);

        if (proposal == null)
        {
            throw new NotFoundException(
                $"Trip proposal with ID '{command.ProposalId}' was not found.");
        }

        var approver = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == approverId,
                ct);

        if (approver == null)
        {
            throw new NotFoundException(
                "Approver employee was not found.");
        }

        if (!approver.IsActive)
        {
            throw new ForbiddenException(
                "Inactive employees cannot approve trip proposals.");
        }

        var pic = proposal.Participants
            .FirstOrDefault(p => p.IsPic);

        if (pic == null)
        {
            throw new BusinessRuleException(
                "Trip proposal does not have a PIC.");
        }

        if (pic.EmployeeId == approver.Id)
        {
            throw new ForbiddenException(
                "You cannot approve your own trip proposal.");
        }

        bool hasManagerRole = approverRoles.Contains(
            "Manager",
            StringComparer.OrdinalIgnoreCase);

        bool hasFinanceRole = approverRoles.Contains(
            "Finance",
            StringComparer.OrdinalIgnoreCase);

        bool hasFinanceManagerRole = approverRoles.Contains(
            "Finance Manager",
            StringComparer.OrdinalIgnoreCase);

        bool hasAdminRole = approverRoles.Contains(
            "Admin",
            StringComparer.OrdinalIgnoreCase);

        byte currentLevel;
        TripProposalStatus nextStatus;

        TripApprovalDecision recordedDecision =
            command.Request.Decision;

        // LEVEL 1: MANAGER
        if (proposal.Status == TripProposalStatus.Submitted)
        {
            if (!hasManagerRole && !hasAdminRole)
            {
                throw new ForbiddenException(
                    "Only Manager or Admin can approve this trip proposal.");
            }

            if (!hasAdminRole)
            {
                if (!pic.Employee.ManagerId.HasValue)
                {
                    throw new ForbiddenException(
                        "This employee does not have a direct manager. Only an Admin can approve this proposal.");
                }

                if (pic.Employee.ManagerId.Value != approver.Id)
                {
                    throw new ForbiddenException(
                        "You are not the manager of the employee who submitted this proposal.");
                }
            }

            currentLevel = 1;

            nextStatus = command.Request.Decision switch
            {
                TripApprovalDecision.Approved =>
                    TripProposalStatus.UnderReview,

                TripApprovalDecision.NeedsRevision =>
                    TripProposalStatus.NeedsRevision,

                TripApprovalDecision.Rejected =>
                    TripProposalStatus.Rejected,

                _ => throw new BusinessRuleException(
                    "Invalid approval decision.")
            };
        }

        // LEVEL 2: FINANCE
        else if (proposal.Status == TripProposalStatus.UnderReview)
        {
            if (!hasFinanceRole && !hasAdminRole)
            {
                throw new ForbiddenException(
                    "Only Finance or Admin can process this trip proposal.");
            }

            currentLevel = 2;

            if (command.Request.Decision == TripApprovalDecision.Approved)
            {
                var requestedParticipants = command.Request.Participants;

                // Finance must provide limits for all participants.
                if (requestedParticipants == null ||
                    requestedParticipants.Count != proposal.Participants.Count)
                {
                    throw new BusinessRuleException(
                        "Finance must provide reimbursement limits for all participants.");
                }

                // Prevent duplicate Employee IDs.
                var duplicateEmployees = requestedParticipants
                    .GroupBy(p => p.EmployeeId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateEmployees.Count > 0)
                {
                    throw new BusinessRuleException(
                        "Duplicate participant detected in approval request.");
                }

                // Save the reimbursement limit for each participant.
                foreach (var participantDto in requestedParticipants)
                {
                    if (participantDto.Amount <= 0)
                    {
                        throw new BusinessRuleException(
                            "Participant reimbursement limit must be greater than zero.");
                    }

                    var participant = proposal.Participants
                        .FirstOrDefault(
                            p => p.EmployeeId == participantDto.EmployeeId);

                    if (participant == null)
                    {
                        throw new BusinessRuleException(
                            $"Employee '{participantDto.EmployeeId}' is not a participant of this proposal.");
                    }

                    // This limit applies to the entire trip, not per day.
                    participant.Amount = participantDto.Amount;
                }

                // Total reimbursement limits for all participants.
                decimal participantTotal = proposal.Participants
                    .Sum(p => p.Amount);

                // Entity TotalAmount still stores accommodation only.
                decimal proposedTotalAmount =
                    proposal.TotalAmount + participantTotal;

                // FinanceLimit applies to the entire proposal.
                bool exceedsFinanceLimit =
                    proposedTotalAmount > TripProposalApprovalLimit.FinanceLimit;

                if (exceedsFinanceLimit)
                {
                    nextStatus =
                        TripProposalStatus.FinanceManagerReview;

                    recordedDecision =
                        TripApprovalDecision.ForwardedToFinanceManager;
                }
                else
                {
                    nextStatus = TripProposalStatus.Approved;
                }
            }
            else
            {
                nextStatus = command.Request.Decision switch
                {
                    TripApprovalDecision.NeedsRevision =>
                        TripProposalStatus.NeedsRevision,

                    TripApprovalDecision.Rejected =>
                        TripProposalStatus.Rejected,

                    _ => throw new BusinessRuleException(
                        "Invalid approval decision.")
                };
            }
        }

        // LEVEL 3: FINANCE MANAGER
        else if (proposal.Status ==
                 TripProposalStatus.FinanceManagerReview)
        {
            if (!hasFinanceManagerRole && !hasAdminRole)
            {
                throw new ForbiddenException(
                    "Only Finance Manager or Admin can approve this trip proposal.");
            }

            currentLevel = 3;

            nextStatus = command.Request.Decision switch
            {
                TripApprovalDecision.Approved =>
                    TripProposalStatus.Approved,

                TripApprovalDecision.NeedsRevision =>
                    TripProposalStatus.NeedsRevision,

                TripApprovalDecision.Rejected =>
                    TripProposalStatus.Rejected,

                _ => throw new BusinessRuleException(
                    "Invalid approval decision.")
            };
        }
        else
        {
            throw new BusinessRuleException(
                $"Cannot perform approval on a trip proposal with status '{proposal.Status}'.");
        }

        proposal.Status = nextStatus;

        uow.TripProposals.Update(proposal);

        var approvalRecord = new TripApproval
        {
            TripProposalId = proposal.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = recordedDecision,
            Notes = command.Request.Notes,
            ApprovalLevel = currentLevel,
            DecidedAt = DateTime.UtcNow
        };

        await uow.TripApprovals.AddAsync(
            approvalRecord,
            ct);

        await uow.SaveChangesAsync(ct);

        // Send a status notification email to the proposal PIC.
        var picParticipant = proposal.Participants
            .FirstOrDefault(p => p.IsPic)
            ?? proposal.Participants.FirstOrDefault();

        if (picParticipant != null)
        {
            var picEmp = await uow.Employees.GetByIdAsync(
                new object[] { picParticipant.EmployeeId },
                ct);

            if (picEmp != null &&
                !string.IsNullOrWhiteSpace(picEmp.Email))
            {
                await emailService.SendTripProposalStatusEmailAsync(
                    picEmp.Email,
                    picEmp.FullName,
                    proposal.Destination,
                    proposal.Status.ToString(),
                    approver.FullName,
                    command.Request.Notes,
                    ct);
            }
        }

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