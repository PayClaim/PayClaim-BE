using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
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
    private readonly IEmailService _emailService;

    public ApproveTripProposalHandler(IUnitOfWork uow, PayClaimDbContext context, IEmailService emailService)
    {
        _uow = uow;
        _context = context;
        _emailService = emailService;
    }

    public async Task<TripApprovalHistoryDto> Handle(ApproveTripProposalCommand command, CancellationToken ct = default)
    {
        var proposal = await _context.TripProposals
            .Include(tp => tp.Participants)
            .FirstOrDefaultAsync(tp => tp.Id == command.ProposalId, ct);

        if (proposal == null)
        {
            throw new NotFoundException($"Trip proposal with ID '{command.ProposalId}' was not found.");
        }

        if (proposal.Status != TripProposalStatus.Submitted && proposal.Status != TripProposalStatus.UnderReview)
        {
            throw new BusinessRuleException(
                $"Cannot process trip proposal with status '{proposal.Status}'. Only proposals in 'Submitted' (Manager Review) or 'UnderReview' (Finance Review) can be processed.");
        }

        byte currentLevel;
        string roleKeyword;
        TripProposalStatus nextStatus;

        // 1. Tentukan tingkat persetujuan berdasarkan status saat ini
        if (proposal.Status == TripProposalStatus.Submitted)
        {
            // Level 1: Persetujuan Manager
            currentLevel = 1;
            roleKeyword = "Manager";

            nextStatus = command.Request.Decision switch
            {
                ApprovalDecision.Approved => TripProposalStatus.UnderReview, // Diteruskan ke Finance untuk set budget
                ApprovalDecision.NeedsRevision => TripProposalStatus.NeedsRevision, // Minta revisi data peserta
                ApprovalDecision.Rejected => TripProposalStatus.Rejected,
                _ => throw new BusinessRuleException("Invalid approval decision.")
            };
        }
        else // proposal.Status == TripProposalStatus.UnderReview
        {
            // Level 2: Penetapan Anggaran & Verifikasi oleh Finance
            currentLevel = 2;
            roleKeyword = "Finance";

            nextStatus = command.Request.Decision switch
            {
                ApprovalDecision.Approved => TripProposalStatus.Approved, // Disetujui final oleh Finance
                ApprovalDecision.NeedsRevision => TripProposalStatus.NeedsRevision,
                ApprovalDecision.Rejected => TripProposalStatus.Rejected,
                _ => throw new BusinessRuleException("Invalid approval decision.")
            };

            // Jika Finance menyetujui dan memperbarui alokasi anggaran (BPMN: Set Hotel, Transport & Allowance Budget)
            if (command.Request.Decision == ApprovalDecision.Approved)
            {
                if (command.Request.TotalAmount.HasValue && command.Request.TotalAmount.Value > 0)
                {
                    proposal.TotalAmount = command.Request.TotalAmount.Value;
                }

                if (command.Request.Participants != null && command.Request.Participants.Count > 0)
                {
                    foreach (var partDto in command.Request.Participants)
                    {
                        var participant = proposal.Participants.FirstOrDefault(p => p.EmployeeId == partDto.EmployeeId);
                        if (participant != null)
                        {
                            participant.Amount = partDto.Amount;
                            participant.IsPic = partDto.IsPic;
                        }
                    }
                }
            }
        }

        // 2. Resolusi Approver
        var approverId = command.Request.ApproverId ?? command.LoggedInApproverId;
        Employee? approver = null;

        if (approverId.HasValue)
        {
            approver = await _uow.Employees.GetByIdAsync(new object[] { approverId.Value }, ct);
        }

        if (approver == null)
        {
            approver = await _context.Employees
                .Include(e => e.Position)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Position.Title.Contains(roleKeyword) || e.Position.Title.Contains("Lead") || e.Position.Title.Contains("Director"), ct)
                ?? await _context.Employees.AsNoTracking().FirstOrDefaultAsync(ct);

            if (approver == null)
            {
                throw new NotFoundException($"No eligible approver ({roleKeyword}) found in the system.");
            }
        }

        proposal.Status = nextStatus;
        _uow.TripProposals.Update(proposal);

        // 3. Catat Riwayat Approval
        var approvalRecord = new TripApproval
        {
            TripProposalId = proposal.Id,
            ApproverId = approver.Id,
            ApproverName = approver.FullName,
            Decision = command.Request.Decision,
            Notes = command.Request.Notes,
            ApprovalLevel = currentLevel,
            DecidedAt = DateTime.UtcNow
        };

        await _uow.TripApprovals.AddAsync(approvalRecord, ct);
        await _uow.SaveChangesAsync(ct);

        // 4. Kirim notifikasi email ke PIC Proposal Perjalanan Dinas
        var picParticipant = proposal.Participants.FirstOrDefault(p => p.IsPic) ?? proposal.Participants.FirstOrDefault();
        if (picParticipant != null)
        {
            var picEmp = await _uow.Employees.GetByIdAsync(new object[] { picParticipant.EmployeeId }, ct);
            if (picEmp != null && !string.IsNullOrWhiteSpace(picEmp.Email))
            {
                await _emailService.SendTripProposalStatusEmailAsync(
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
