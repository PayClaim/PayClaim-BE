using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.CompleteTripProposal;

public record CompleteTripProposalCommand(Guid ProposalId, IFormFile? ReportFile = null, string? ReportNotes = null);

public class CompleteTripProposalHandler
{
    private readonly IUnitOfWork _uow;
    private readonly PayClaimDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public CompleteTripProposalHandler(IUnitOfWork uow, PayClaimDbContext context, IFileStorageService fileStorage)
    {
        _uow = uow;
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<TripProposalDetailDto> Handle(CompleteTripProposalCommand command, CancellationToken ct = default)
    {
        var proposal = await _context.TripProposals
            .Include(tp => tp.Participants)
                .ThenInclude(p => p.Employee)
            .FirstOrDefaultAsync(tp => tp.Id == command.ProposalId, ct);

        if (proposal == null)
        {
            throw new NotFoundException($"Trip proposal with ID '{command.ProposalId}' was not found.");
        }

        if (proposal.Status != TripProposalStatus.Approved)
        {
            throw new BusinessRuleException(
                $"Only Approved trip proposals can be completed with a travel report. Current status is '{proposal.Status}'.");
        }

        // Simpan dokumen laporan perjalanan jika ada lampiran baru
        if (command.ReportFile != null && command.ReportFile.Length > 0)
        {
            proposal.FilePath = await _fileStorage.SaveFileAsync(command.ReportFile, "reports", ct);
        }

        proposal.Status = TripProposalStatus.Completed;
        _uow.TripProposals.Update(proposal);

        // Catat jejak pengumpulan laporan perjalanan
        var picParticipant = proposal.Participants.FirstOrDefault(p => p.IsPic) ?? proposal.Participants.FirstOrDefault();
        var submitterId = picParticipant?.EmployeeId ?? Guid.Empty;
        var submitterName = picParticipant?.Employee?.FullName ?? "Trip Participant";

        var completeRecord = new TripApproval
        {
            TripProposalId = proposal.Id,
            ApproverId = submitterId != Guid.Empty ? submitterId : proposal.Id, // shortcut: fallback ID if no pic, self-contained in trip
            ApproverName = submitterName,
            Decision = ApprovalDecision.Approved,
            Notes = command.ReportNotes ?? "Laporan perjalanan dinas (Travel Report) berhasil diserahkan.",
            ApprovalLevel = 4, // Level 4: Post-Travel Report Submission
            DecidedAt = DateTime.UtcNow
        };

        await _uow.TripApprovals.AddAsync(completeRecord, ct);
        await _uow.SaveChangesAsync(ct);

        var approvals = await _context.TripApprovals
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
