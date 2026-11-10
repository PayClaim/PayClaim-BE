using Microsoft.AspNetCore.Http;
using PayClaim.DTOs.TripProposal;
using System.Text.Json;

namespace PayClaim.Features.TripProposals;

public static class TripProposalParticipantParser
{
    public static void EnsureParticipants(CreateTripProposalDto request, HttpRequest? httpRequest)
    {
        if (httpRequest == null || !httpRequest.HasFormContentType)
        {
            return;
        }

        if (request.Participants != null && request.Participants.Count > 0)
        {
            return;
        }

        if (httpRequest.Form.TryGetValue("Participants", out var jsonVal) || httpRequest.Form.TryGetValue("participants", out jsonVal))
        {
            var str = jsonVal.ToString().Trim();
            if (str.StartsWith("[") && str.EndsWith("]"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<ParticipantItemDto>>(
                        str,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (items != null && items.Count > 0)
                    {
                        request.Participants = items;
                    }
                }
                catch { }
            }
        }

        if (request.Participants == null || request.Participants.Count == 0)
        {
            var empIds = httpRequest.Form["employeeId"].Count > 0 ? httpRequest.Form["employeeId"] : httpRequest.Form["EmployeeId"];
            if (empIds.Count > 0)
            {
                var isPics = httpRequest.Form["isPic"].Count > 0 ? httpRequest.Form["isPic"] : httpRequest.Form["IsPic"];
                var amounts = httpRequest.Form["amount"].Count > 0 ? httpRequest.Form["amount"] : httpRequest.Form["Amount"];

                request.Participants ??= new();
                for (int i = 0; i < empIds.Count; i++)
                {
                    if (Guid.TryParse(empIds[i], out var id))
                    {
                        bool isPic = i < isPics.Count && bool.TryParse(isPics[i], out var b) && b;
                        decimal amount = i < amounts.Count && decimal.TryParse(amounts[i], out var a) ? a : request.TotalAmount;
                        request.Participants.Add(new ParticipantItemDto
                        {
                            EmployeeId = id,
                            IsPic = isPic,
                            Amount = amount
                        });
                    }
                }
            }
        }
    }
}
