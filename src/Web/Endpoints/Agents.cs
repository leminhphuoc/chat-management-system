using ChatSupportSystem.Application.Features.Agents.Commands.UpdateAgentAvailability;
using ChatSupportSystem.Application.Features.Agents.Queries.GetAvailableAgents;
using ChatSupportSystem.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatSupportSystem.Web.Endpoints;

public class Agents : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("available", GetAvailableAgents);
        group.MapPut("{id}/availability", UpdateAgentAvailability);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetAvailableAgents(
        [FromQuery] bool includeAllAgents,
        ISender sender)
    {
        var query = new GetAvailableAgentsQuery { IncludeAllAgents = includeAllAgents };
        var result = await sender.Send(query);

        if (!result.Success)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Ok(result);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> UpdateAgentAvailability(
        int id,
        [FromBody] UpdateAgentAvailabilityCommand command,
        ISender sender)
    {
        if (id != command.AgentId)
        {
            return TypedResults.BadRequest("ID in route must match ID in request body");
        }

        var result = await sender.Send(command);

        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return TypedResults.NotFound(result);
            }

            return TypedResults.BadRequest(result);
        }

        return TypedResults.Ok(result);
    }
}