using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Constants;
using PayClaim.Common.Exceptions;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.ApproveTripProposal;

public record ApproveTripProposalCommand(
    Guid ProposalId,
    TripApprovalActionDto Request,
    Guid LoggedInApproverId,
    string[] ApproverRoles);

public class ApproveTripProposalHandler(
    IUnitOfWork uow,
    PayClaimDbContext context)
{
    public async Task<TripApprovalHistoryDto> Handle(
        ApproveTripProposalCommand command,
        CancellationToken ct = default)
    {
        // 1. Ambil proposal beserta peserta dan data Employee.
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

        // 2. Ambil approver berdasarkan EmployeeId dari JWT.
        var approver = await context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == command.LoggedInApproverId, ct);

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

        // 3. Cari PIC sebagai pengaju proposal.
        var pic = proposal.Participants
            .FirstOrDefault(p => p.IsPic);

        if (pic == null)
        {
            throw new BusinessRuleException(
                "Trip proposal does not have a PIC.");
        }

        // Pengaju tidak boleh menyetujui proposalnya sendiri.
        if (pic.EmployeeId == approver.Id)
        {
            throw new ForbiddenException(
                "You cannot approve your own trip proposal.");
        }

        // 4. Periksa role yang dimiliki approver.
        bool hasManagerRole = command.ApproverRoles.Contains(
            "Manager", StringComparer.OrdinalIgnoreCase);

        bool hasFinanceRole = command.ApproverRoles.Contains(
            "Finance", StringComparer.OrdinalIgnoreCase);

        bool hasFinanceManagerRole = command.ApproverRoles.Contains(
            "Finance Manager", StringComparer.OrdinalIgnoreCase);

        byte currentLevel;
        TripProposalStatus nextStatus;
        TripApprovalDecision recordedDecision = command.Request.Decision;

        // =====================================================
        // LEVEL 1: DIRECT MANAGER
        // Status awal: Submitted
        // =====================================================
        if (proposal.Status == TripProposalStatus.Submitted)
        {
            if (!hasManagerRole)
            {
                throw new ForbiddenException(
                    "Only Manager can approve this trip proposal.");
            }

            // Pastikan approver adalah atasan langsung PIC.
            if (pic.Employee.ManagerId != approver.Id)
            {
                throw new ForbiddenException(
                    "You are not the manager of the employee who submitted this proposal.");
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

        // =====================================================
        // LEVEL 2: FINANCE
        // Status awal: UnderReview
        // =====================================================
        else if (proposal.Status == TripProposalStatus.UnderReview)
        {
            if (!hasFinanceRole)
            {
                throw new ForbiddenException(
                    "Only Finance can process this trip proposal.");
            }

            currentLevel = 2;

            // Jika Finance menyetujui, perbarui anggaran terlebih dahulu.
            if (command.Request.Decision == TripApprovalDecision.Approved)
            {
                // Update total anggaran jika Finance mengirim nominal baru.
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

                // Update pembagian anggaran peserta jika dikirim.
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

                // Periksa limit berdasarkan total anggaran terbaru.
                if (proposal.TotalAmount >
                    TripProposalApprovalLimit.FinanceLimit)
                {
                    // Finance tidak menyetujui final.
                    // Proposal diteruskan ke Finance Manager.
                    nextStatus =
                        TripProposalStatus.FinanceManagerReview;

                    recordedDecision =
                        TripApprovalDecision.ForwardedToFinanceManager;
                }
                else
                {
                    // Masih dalam limit Finance.
                    // Finance boleh menyetujui secara final.
                    nextStatus = TripProposalStatus.Approved;
                }
            }
            else
            {
                // Finance dapat menolak atau meminta revisi.
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

        // =====================================================
        // LEVEL 3: FINANCE MANAGER
        // Status awal: FinanceManagerReview
        // =====================================================
        else if (proposal.Status ==
                 TripProposalStatus.FinanceManagerReview)
        {
            if (!hasFinanceManagerRole)
            {
                throw new ForbiddenException(
                    "Only Finance Manager can approve this trip proposal.");
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

        // 5. Update status proposal.
        proposal.Status = nextStatus;

        uow.TripProposals.Update(proposal);

        // 6. Simpan riwayat approval.
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

        // 7. Kembalikan riwayat approval.
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