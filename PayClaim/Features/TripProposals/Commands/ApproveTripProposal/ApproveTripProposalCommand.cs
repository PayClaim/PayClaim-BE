
using System.Globalization;
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

        var picRoles = await context.GetRoleNamesAsync(pic.EmployeeId, ct);
        var picHighestRole = ApprovalRouting.GetHighestBusinessRole(picRoles);

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

        var expectedCurrentStatus = proposal.Status;
        byte currentLevel;
        TripProposalStatus nextStatus;

        TripApprovalDecision recordedDecision =
            command.Request.Decision;

        decimal? previousAccommodationBudget = null;
        decimal? updatedAccommodationBudget = null;

        // LEVEL 1: MANAGER
        if (proposal.Status == TripProposalStatus.Submitted)
        {
            if (!hasManagerRole && !hasFinanceManagerRole && !hasAdminRole)
            {
                throw new ForbiddenException(
                    "Only Manager, Finance Manager, or Admin can approve this trip proposal.");
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
            bool canVerifyFinance =
                hasAdminRole ||
                hasFinanceManagerRole ||
                (hasFinanceRole &&
                 (picHighestRole == ApprovalRouting.Employee ||
                  picHighestRole == ApprovalRouting.Manager));

            if (!canVerifyFinance)
            {
                throw new ForbiddenException(
                    "Only Finance (or a higher role) can process this trip proposal.");
            }

            currentLevel = 2;

            if (command.Request.Decision == TripApprovalDecision.Approved)
            {
                ApplyBudgetAndParticipantLimits(
                    proposal,
                    command.Request,
                    out previousAccommodationBudget,
                    out updatedAccommodationBudget);

                // Total reimbursement limits for all participants.
                decimal participantTotal = proposal.Participants
                    .Sum(p => p.Amount);

                // Entity TotalAmount still stores accommodation only.
                decimal proposedTotalAmount =
                    proposal.TotalAmount + participantTotal;

                // FinanceLimit applies to the entire proposal.
                bool exceedsFinanceLimit =
                    proposedTotalAmount > TripProposalApprovalLimit.FinanceLimit;

                if (exceedsFinanceLimit && !hasFinanceManagerRole && !hasAdminRole)
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
            bool canApproveAsFinanceManager =
                hasAdminRole ||
                (hasFinanceManagerRole &&
                 picHighestRole != ApprovalRouting.FinanceManager);

            if (!canApproveAsFinanceManager)
            {
                throw new ForbiddenException(
                    "Only Finance Manager (or Admin) can approve this trip proposal.");
            }

            currentLevel = 3;

            if (command.Request.Decision == TripApprovalDecision.Approved &&
                (proposal.Participants.Any(p => p.Amount <= 0) ||
                 command.Request.Participants != null))
            {
                ApplyBudgetAndParticipantLimits(
                    proposal,
                    command.Request,
                    out previousAccommodationBudget,
                    out updatedAccommodationBudget);
            }

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

        decimal targetTotalAmount = updatedAccommodationBudget ?? proposal.TotalAmount;

        // Keep the approver's manual note.
        string? approvalNotes = command.Request.Notes;

        // Add budget adjustment information to Notes only when
        // Finance / Finance Manager approves and actually changes the Accommodation Budget.
        if ((currentLevel == 2 || currentLevel == 3) &&
            command.Request.Decision == TripApprovalDecision.Approved &&
            previousAccommodationBudget.HasValue &&
            updatedAccommodationBudget.HasValue &&
            previousAccommodationBudget.Value != updatedAccommodationBudget.Value)
        {
            string previousAmount = previousAccommodationBudget.Value
                .ToString(CultureInfo.InvariantCulture);

            string updatedAmount = updatedAccommodationBudget.Value
                .ToString(CultureInfo.InvariantCulture);

            string budgetMarker =
                $"[ACCOMMODATION_BUDGET:{previousAmount}|{updatedAmount}]";

            approvalNotes = string.IsNullOrWhiteSpace(approvalNotes)
                ? budgetMarker
                : $"{budgetMarker}{Environment.NewLine}{approvalNotes}";
        }

        var approvalRecord = new TripApproval
        {
            TripProposalId = proposal.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = recordedDecision,
            Notes = approvalNotes,
            ApprovalLevel = currentLevel,
            DecidedAt = AppTime.WibNow
        };

        await using var tx = await context.Database.BeginTransactionAsync(ct);

        var updatedRows = await context.TripProposals
            .Where(tp => tp.Id == proposal.Id && tp.Status == expectedCurrentStatus)
            .ExecuteUpdateAsync(s => s
                .SetProperty(tp => tp.Status, nextStatus)
                .SetProperty(tp => tp.TotalAmount, targetTotalAmount),
                ct);

        if (updatedRows == 0)
        {
            throw new ConflictException(
                "Trip proposal status has already been updated by another approver.");
        }

        proposal.Status = nextStatus;
        context.Entry(proposal).State = EntityState.Unchanged;

        await uow.TripApprovals.AddAsync(
            approvalRecord,
            ct);

        await uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Send a status notification email to the proposal PIC.
        // Send only the manual note, not the internal budget marker.
        var picParticipant = proposal.Participants
            .FirstOrDefault(p => p.IsPic)
            ?? proposal.Participants.FirstOrDefault();

        var picEmp = picParticipant?.Employee;
        var picName = picEmp?.FullName ?? "PIC";
        var proposalTotal = proposal.TotalAmount +
            proposal.Participants.Sum(p => p.Amount);

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
                proposal.Id,
                ct);
        }

        // Notifikasi ke approver berikutnya sesuai status hasil keputusan.
        if (proposal.Status == TripProposalStatus.UnderReview)
        {
            // Manager sudah menyetujui -> masuk antrean Finance.
            var financeUsers = (await context.GetActiveByRolesAsync(
                    ct, "Finance"))
                .Where(f => !string.Equals(
                    f.Email, approver.Email, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        f.Email, picEmp?.Email, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await Task.WhenAll(financeUsers.Select(fin =>
                emailService.SendTripProposalActionRequiredEmailAsync(
                    fin.Email,
                    fin.Name,
                    picName,
                    proposal.Destination,
                    proposal.StartDate,
                    proposal.EndDate,
                    proposalTotal,
                    "Finance Verification",
                    proposal.Id,
                    command.Request.Notes,
                    ct)));
        }
        else if (proposal.Status ==
                 TripProposalStatus.FinanceManagerReview)
        {
            // Nominal melebihi limit Finance -> menunggu Finance Manager.
            var financeManagers = (await context.GetActiveByRolesAsync(
                    ct, "Finance Manager"))
                .Where(f => !string.Equals(
                    f.Email, approver.Email, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(
                        f.Email, picEmp?.Email, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await Task.WhenAll(financeManagers.Select(fm =>
                emailService.SendTripProposalActionRequiredEmailAsync(
                    fm.Email,
                    fm.Name,
                    picName,
                    proposal.Destination,
                    proposal.StartDate,
                    proposal.EndDate,
                    proposalTotal,
                    "Finance Manager Approval",
                    proposal.Id,
                    command.Request.Notes,
                    ct)));
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

    private static void ApplyBudgetAndParticipantLimits(
        TripProposal proposal,
        TripApprovalActionDto request,
        out decimal? previousAccommodationBudget,
        out decimal? updatedAccommodationBudget)
    {
        if (!request.TotalAmount.HasValue)
        {
            throw new BusinessRuleException(
                "Finance must provide the accommodation budget.");
        }

        decimal approvedAccommodationBudget = request.TotalAmount.Value;
        if (approvedAccommodationBudget <= 0)
        {
            throw new BusinessRuleException(
                "Accommodation budget must be greater than zero.");
        }

        previousAccommodationBudget = proposal.TotalAmount;
        updatedAccommodationBudget = approvedAccommodationBudget;
        proposal.TotalAmount = approvedAccommodationBudget;

        var requestedParticipants = request.Participants;
        if (requestedParticipants == null ||
            requestedParticipants.Count != proposal.Participants.Count)
        {
            throw new BusinessRuleException(
                "Finance must provide reimbursement limits for all participants.");
        }

        var hasDuplicate = requestedParticipants
            .GroupBy(p => p.EmployeeId)
            .Any(g => g.Count() > 1);

        if (hasDuplicate)
        {
            throw new BusinessRuleException(
                "Duplicate participant detected in approval request.");
        }

        foreach (var participantDto in requestedParticipants)
        {
            if (participantDto.Amount <= 0)
            {
                throw new BusinessRuleException(
                    "Participant reimbursement limit must be greater than zero.");
            }

            var participant = proposal.Participants
                .FirstOrDefault(p => p.EmployeeId == participantDto.EmployeeId);

            if (participant == null)
            {
                throw new BusinessRuleException(
                    $"Employee '{participantDto.EmployeeId}' is not a participant of this proposal.");
            }

            // Limit berlaku untuk total perjalanan, bukan per hari.
            participant.Amount = participantDto.Amount;
        }
    }
}
