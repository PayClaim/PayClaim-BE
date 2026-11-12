using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.CompleteTripProposal;

public record CompleteTripProposalCommand(
    Guid ProposalId,
    Guid? SubmitterEmployeeId = null,
    IFormFile? ReportFile = null,
    string? ReportNotes = null) : IRequest<TripProposalDetailDto>;

public class CompleteTripProposalHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IFileStorageService fileStorage,
    IEmailService emailService,
    ICurrentUser currentUser) : IRequestHandler<CompleteTripProposalCommand, TripProposalDetailDto>
{
    public async Task<TripProposalDetailDto> Handle(
        CompleteTripProposalCommand command,
        CancellationToken ct = default)
    {
        var submitterId = command.SubmitterEmployeeId ?? currentUser.RequiredEmployeeId;

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

        if (proposal.Status != TripProposalStatus.Approved)
        {
            throw new BusinessRuleException(
                $"Only Approved trip proposals can be completed with a travel report. Current status is '{proposal.Status}'.");
        }

        var pic = proposal.Participants
            .FirstOrDefault(p => p.IsPic);

        if (pic == null)
        {
            throw new BusinessRuleException(
                "Trip proposal does not have a PIC.");
        }

        if (pic.EmployeeId != submitterId)
        {
            throw new ForbiddenException(
                "Only the PIC can submit the travel report.");
        }

        if (command.ReportFile != null &&
            command.ReportFile.Length > 0)
        {
            proposal.FilePath = await fileStorage.SaveFileAsync(
                command.ReportFile,
                "reports",
                ct);
        }

        if (!string.IsNullOrWhiteSpace(command.ReportNotes))
        {
            // shortcut: simpan report notes ke description agar tidak perlu migrasi skema tabel, upgrade jika kolom ReportNotes resmi ditambahkan ke Trip_proposals
            proposal.Description = string.IsNullOrWhiteSpace(proposal.Description)
                ? $"[Travel Report]: {command.ReportNotes.Trim()}"
                : $"{proposal.Description}\n\n[Travel Report]: {command.ReportNotes.Trim()}";
        }

        proposal.Status = TripProposalStatus.Completed;

        uow.TripProposals.Update(proposal);

        await uow.SaveChangesAsync(ct);

        // Notifikasi ke PIC dan seluruh peserta bahwa perjalanan dinas selesai.
        var recipients = proposal.Participants
            .Where(p => p.Employee != null &&
                        !string.IsNullOrWhiteSpace(p.Employee.Email))
            .GroupBy(p => p.Employee.Email)
            .Select(g => new
            {
                Email = g.Key,
                Name = g.First().Employee.FullName
            })
            .ToList();

        await Task.WhenAll(recipients.Select(r =>
            emailService.SendTripProposalStatusEmailAsync(
                r.Email,
                r.Name,
                proposal.Destination,
                "Completed",
                pic.Employee?.FullName,
                string.IsNullOrWhiteSpace(command.ReportNotes)
                    ? "Laporan perjalanan telah diunggah."
                    : command.ReportNotes,
                proposal.Id,
                ct)));

        var approvals = await context.TripApprovals
            .AsNoTracking()
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

            Participants = proposal.Participants.Select(p =>
                new ParticipantItemDto
                {
                    EmployeeId = p.EmployeeId,
                    EmployeeName = p.Employee.FullName,
                    EmployeeNumber = p.Employee.Number,
                    IsPic = p.IsPic,
                    Amount = p.Amount
                }).ToList(),

            ApprovalHistory = approvals
        };
    }
}
