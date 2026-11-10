using MediatR;
using Microsoft.AspNetCore.Http;
using PayClaim.Common.Exceptions;
using PayClaim.Common.Security;
using PayClaim.Common.Services;
using PayClaim.Data.UnitOfWork;
using PayClaim.DTOs.TripProposal;
using PayClaim.Entities;
using PayClaim.Enums;

namespace PayClaim.Features.TripProposals.Commands.CreateTripProposal;

public record CreateTripProposalCommand(CreateTripProposalDto Request, Guid? SubmitterEmployeeId = null) : IRequest<TripProposalDetailDto>;

public class CreateTripProposalHandler(
    IUnitOfWork uow,
    IFileStorageService fileStorage,
    ICurrentUser currentUser,
    IHttpContextAccessor httpContextAccessor) : IRequestHandler<CreateTripProposalCommand, TripProposalDetailDto>
{
    public async Task<TripProposalDetailDto> Handle(CreateTripProposalCommand command, CancellationToken ct = default)
    {
        var submitterId = command.SubmitterEmployeeId ?? currentUser.RequiredEmployeeId;
        var request = command.Request;

        TripProposalParticipantParser.EnsureParticipants(request, httpContextAccessor.HttpContext?.Request);

        if (request.EndDate < request.StartDate)
        {
            throw new BusinessRuleException("End date cannot be earlier than start date.");
        }

        if (request.Participants == null || request.Participants.Count == 0)
        {
            throw new BusinessRuleException("At least one participant is required for a trip proposal.");
        }

        var picCount = request.Participants.Count(p => p.IsPic);
        if (picCount != 1)
        {
            throw new BusinessRuleException("A trip proposal must have exactly one Person In Charge (PIC).");
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
            throw new BusinessRuleException("Duplicate participant detected in proposal.");
        }

        var totalParticipantsAmount = request.Participants.Sum(p => p.Amount);
        if (totalParticipantsAmount != request.TotalAmount)
        {
            throw new BusinessRuleException($"Sum of participants' budgets ({totalParticipantsAmount:N0}) does not match the proposal total amount ({request.TotalAmount:N0}).");
        }

        var participantDtos = new List<ParticipantItemDto>();
        foreach (var p in request.Participants)
        {
            var employee = await uow.Employees.GetByIdAsync(new object[] { p.EmployeeId }, ct);
            if (employee == null)
            {
                throw new NotFoundException($"Employee with ID '{p.EmployeeId}' was not found.");
            }

            participantDtos.Add(new ParticipantItemDto
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.FullName,
                EmployeeNumber = employee.Number,
                IsPic = p.IsPic,
                Amount = p.Amount
            });
        }

        var filePath = string.Empty;
        if (request.File != null && request.File.Length > 0)
        {
            filePath = await fileStorage.SaveFileAsync(request.File, "proposals", ct);
        }

        var proposal = new TripProposal
        {
            Id = Guid.NewGuid(),
            Destination = request.Destination,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TotalAmount = request.TotalAmount,
            Description = request.Description,
            SubmittedAt = DateTime.UtcNow,
            Status = TripProposalStatus.Submitted,
            FilePath = filePath
        };

        foreach (var p in request.Participants)
        {
            proposal.Participants.Add(new ProposalParticipant
            {
                TripProposalId = proposal.Id,
                EmployeeId = p.EmployeeId,
                IsPic = p.IsPic,
                Amount = p.Amount
            });
        }

        await uow.TripProposals.AddAsync(proposal, ct);
        await uow.SaveChangesAsync(ct);

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
            Participants = participantDtos,
            ApprovalHistory = new List<TripApprovalHistoryDto>()
        };
    }
}
