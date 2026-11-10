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
    string[]? ApproverRoles = null) : IRequest<TripApprovalHistoryDto>;

public class ApproveTripProposalHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<ApproveTripProposalCommand, TripApprovalHistoryDto>
{
    public async Task<TripApprovalHistoryDto> Handle(
        ApproveTripProposalCommand command,
        CancellationToken ct = default)
    {
        var approverId = command.LoggedInApproverId ?? currentUser.RequiredEmployeeId;
        var approverRoles = command.ApproverRoles ?? currentUser.Roles;

        var proposal = await context.TripProposals
            .Include(tp => tp.Participants)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(
                tp => tp.Id == command.ProposalId, ct);

        if (proposal == null)
        {
            throw new NotFoundException(
                $"Trip proposal with ID '{command.ProposalId}' was not found.");
        }

        var approver = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == approverId, ct);

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
            "Manager", StringComparer.OrdinalIgnoreCase);

        bool hasFinanceRole = approverRoles.Contains(
            "Finance", StringComparer.OrdinalIgnoreCase);

        bool hasFinanceManagerRole = approverRoles.Contains(
            "Finance Manager", StringComparer.OrdinalIgnoreCase);

        bool hasAdminRole = approverRoles.Contains(
            "Admin", StringComparer.OrdinalIgnoreCase);

        byte currentLevel;
        TripProposalStatus nextStatus;
        TripApprovalDecision recordedDecision = command.Request.Decision;

        if (proposal.Status == TripProposalStatus.Submitted)
        {
            if (!hasManagerRole && !hasFinanceManagerRole && !hasAdminRole)
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
        else if (proposal.Status == TripProposalStatus.UnderReview)
        {
            if (!hasFinanceRole && !hasFinanceManagerRole && !hasAdminRole)
            {
                throw new ForbiddenException(
                    "Only Finance or Admin can process this trip proposal.");
            }

            currentLevel = 2;

            if (command.Request.Decision == TripApprovalDecision.Approved)
            {
                if (command.Request.TotalAmount.HasValue)
                {
                    if (command.Request.TotalAmount.Value <= 0)
                    {
                        throw new BusinessRuleException(
                            "Total amount must be greater than zero.");
                    }

                    proposal.TotalAmount =
                        command.Request.TotalAmount.Value;
                }

                if (command.Request.Participants != null &&
                    command.Request.Participants.Count > 0)
                {
                    foreach (var partDto in command.Request.Participants)
                    {
                        if (partDto.Amount < 0)
                        {
                            throw new BusinessRuleException(
                                "Participant amount cannot be negative.");
                        }

                        var participant = proposal.Participants
                            .FirstOrDefault(
                                p => p.EmployeeId == partDto.EmployeeId);

                        if (participant == null)
                        {
                            throw new BusinessRuleException(
                                $"Employee '{partDto.EmployeeId}' is not a participant of this proposal.");
                        }

                        participant.Amount = partDto.Amount;
                    }
                }

                if (proposal.TotalAmount >
                    TripProposalApprovalLimit.FinanceLimit)
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
            approvalRecord, ct);

        await uow.SaveChangesAsync(ct);

        // Kirim notifikasi email ke PIC Proposal Perjalanan Dinas
        var picParticipant = proposal.Participants.FirstOrDefault(p => p.IsPic) ?? proposal.Participants.FirstOrDefault();
        if (picParticipant != null)
        {
            var picEmp = await uow.Employees.GetByIdAsync(new object[] { picParticipant.EmployeeId }, ct);
            if (picEmp != null && !string.IsNullOrWhiteSpace(picEmp.Email))
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
