using ChatSupportSystem.Application.Features.Queue.Commands.AssignChatToAgent;
using ChatSupportSystem.Application.Features.Queue.Queries.GetQueueStatus;
using ChatSupportSystem.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatSupportSystem.Web.Endpoints;

public class Queue : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapGet("status", GetQueueStatus);
        group.MapPost("assign", AssignChatToAgent);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> GetQueueStatus(
        ISender sender)
    {
        var query = new GetQueueStatusQuery();
        var result = await sender.Send(query);

        if (!result.Success)
        {
            return TypedResults.BadRequest(result);
        }

        return TypedResults.Ok(result);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> AssignChatToAgent(
        [FromBody] AssignChatToAgentCommand command,
        ISender sender)
    {
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