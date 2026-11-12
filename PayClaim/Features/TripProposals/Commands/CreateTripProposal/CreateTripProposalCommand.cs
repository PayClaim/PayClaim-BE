using MediatR;
using Microsoft.EntityFrameworkCore;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Common.Services;
using PayClaim.Data;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.CreateTripProposal;

public record CreateTripProposalCommand(
    CreateTripProposalDto Request,
    Guid? SubmitterEmployeeId = null
) : IRequest<TripProposalDetailDto>;

public class CreateTripProposalHandler(
    IUnitOfWork uow,
    PayClaimDbContext context,
    IFileStorageService fileStorage,
    IEmailService emailService,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<CreateTripProposalCommand, TripProposalDetailDto>
{
    public async Task<TripProposalDetailDto> Handle(
        CreateTripProposalCommand command,
        CancellationToken ct = default)
    {
        var submitterId = command.SubmitterEmployeeId ?? currentUser.RequiredEmployeeId;
        var roles = currentUser.Roles;

        // Cegah role Admin membuat proposal perjalanan dinas
        if (roles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(
                "Akun Administrator tidak diperbolehkan mengajukan proposal perjalanan dinas.");
        }

        // Jika pengaju adalah Manager, tahap approval Manager dilewati
        // sehingga proposal langsung masuk ke Finance (UnderReview).
        var submittedByManager = roles.Contains(
            "Manager",
            StringComparer.OrdinalIgnoreCase);

        var submitterEmployee = await uow.Employees.GetByIdAsync(new object[] { submitterId }, ct);
        if (submitterEmployee != null && string.Equals(submitterEmployee.PositionId, "DIR", StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException(
                "Posisi Director tidak diperbolehkan mengajukan proposal perjalanan dinas.");
        }

        var request = command.Request;

        TripProposalParticipantParser.EnsureParticipants(
            request,
            httpContextAccessor.HttpContext?.Request);

        if (request.TotalAmount <= 0)
        {
            throw new BusinessRuleException(
                "Accommodation amount must be greater than zero.");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new BusinessRuleException(
                "End date cannot be earlier than start date.");
        }

        // Minimal satu peserta, termasuk PIC.
        if (request.Participants == null ||
            request.Participants.Count == 0)
        {
            throw new BusinessRuleException(
                "At least one participant is required for a trip proposal.");
        }

        var picCount = request.Participants.Count(p => p.IsPic);

        if (picCount != 1)
        {
            throw new BusinessRuleException(
                "A trip proposal must have exactly one Person In Charge (PIC).");
        }

        var pic = request.Participants.First(p => p.IsPic);

        if (pic.EmployeeId != submitterId)
        {
            throw new ForbiddenException(
                "Only the logged-in employee can submit a proposal as its PIC.");
        }

        var duplicateEmployees = request.Participants
            .GroupBy(p => p.EmployeeId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateEmployees.Count != 0)
        {
            throw new BusinessRuleException(
                "Duplicate participant detected in proposal.");
        }

        var participantIds = request.Participants.Select(p => p.EmployeeId).ToList();
        var employeesById = await context.Employees
            .AsNoTracking()
            .Where(e => participantIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        var participantDtos = new List<ParticipantItemDto>();

        foreach (var p in request.Participants)
        {
            if (!employeesById.TryGetValue(p.EmployeeId, out var employee))
            {
                throw new NotFoundException(
                    $"Employee with ID '{p.EmployeeId}' was not found.");
            }

            participantDtos.Add(new ParticipantItemDto
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName,
                EmployeeNumber = employee.Number,
                IsPic = p.IsPic,
                Amount = 0
            });
        }

        // Surat proposal wajib diunggah.
        if (request.File == null || request.File.Length == 0)
        {
            throw new BusinessRuleException(
                "Proposal letter is required.");
        }

        var filePath = await fileStorage.SaveFileAsync(
            request.File,
            "proposals",
            ct);

        var proposal = new TripProposal
        {
            Id = Guid.NewGuid(),
            Destination = request.Destination,
            StartDate = request.StartDate,
            EndDate = request.EndDate,

            // Saat Create, TotalAmount Entity hanya berisi biaya akomodasi.
            TotalAmount = request.TotalAmount,

            Description = request.Description,
            SubmittedAt = DateTime.UtcNow,
            Status = submittedByManager
                ? TripProposalStatus.UnderReview
                : TripProposalStatus.Submitted,
            FilePath = filePath
        };

        foreach (var p in request.Participants)
        {
            proposal.Participants.Add(new ProposalParticipant
            {
                TripProposalId = proposal.Id,
                EmployeeId = p.EmployeeId,
                IsPic = p.IsPic,

                // Limit peserta akan ditentukan Finance.
                Amount = 0
            });
        }

        await uow.TripProposals.AddAsync(proposal, ct);
        await uow.SaveChangesAsync(ct);

        // Notifikasi email ke approver pertama.
        // Jika pengaju Manager, tahap Manager dilewati -> langsung ke Finance.
        var submitterName = submitterEmployee?.FullName ?? "PIC";

        if (submittedByManager)
        {
            var financeUsers = (await context.GetActiveByRolesAsync(ct, "Finance"))
                .Where(f => !string.Equals(
                    f.Email, submitterEmployee?.Email, StringComparison.OrdinalIgnoreCase))
                .ToList();

            await Task.WhenAll(financeUsers.Select(fin =>
                emailService.SendTripProposalActionRequiredEmailAsync(
                    fin.Email,
                    fin.Name,
                    submitterName,
                    proposal.Destination,
                    proposal.StartDate,
                    proposal.EndDate,
                    proposal.TotalAmount,
                    "Finance Verification",
                    proposal.Id,
                    null,
                    ct)));
        }
        else
        {
            var directManager = await context.GetDirectManagerAsync(
                submitterId, ct);

            if (directManager != null)
            {
                await emailService.SendTripProposalActionRequiredEmailAsync(
                    directManager.Email,
                    directManager.Name,
                    submitterName,
                    proposal.Destination,
                    proposal.StartDate,
                    proposal.EndDate,
                    proposal.TotalAmount,
                    "Manager Approval",
                    proposal.Id,
                    null,
                    ct);
            }
        }

        // Saat Create semua limit peserta masih 0.
        decimal participantTotal = proposal.Participants.Sum(p => p.Amount);

        return new TripProposalDetailDto
        {
            Id = proposal.Id,
            Destination = proposal.Destination,
            StartDate = proposal.StartDate,

            // Nilai yang tersimpan sebagai akomodasi.
            AccommodationAmount = proposal.TotalAmount,

            // Total awal sama dengan akomodasi karena limit peserta masih 0.
            TotalAmount = proposal.TotalAmount + participantTotal,

            SubmittedAt = proposal.SubmittedAt,
            Description = proposal.Description,
            Status = proposal.Status,
            FilePath = proposal.FilePath,
            Participants = participantDtos,
            ApprovalHistory = new List<TripApprovalHistoryDto>()
        };
    }
}