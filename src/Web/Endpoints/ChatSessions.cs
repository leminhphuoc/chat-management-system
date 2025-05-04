using ChatSupportSystem.Application.Features.ChatSessions.Commands.CreateChatSession;
using ChatSupportSystem.Application.Features.ChatSessions.Commands.MarkSessionInactive;
using ChatSupportSystem.Application.Features.ChatSessions.Queries.PollChatSession;
using ChatSupportSystem.Web.Infrastructure;

using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ChatSupportSystem.Web.Endpoints;

public class ChatSessions : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        var group = app.MapGroup(this);

        group.MapPost("", CreateSession);
        group.MapGet("{id}/poll", PollSession);
        group.MapPut("{id}/inactive", MarkSessionInactive);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IResult> CreateSession(
      [FromBody] CreateChatSessionCommand command,
      ISender sender)
    {
        var result = await sender.Send(command);

        if (!result.Success)
        {
            return TypedResults.Problem(
                detail: result.Message,
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return TypedResults.Ok(result);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> PollSession(
        int id,
        ISender sender)
    {
        var query = new PollChatSessionQuery { SessionId = id };
        var result = await sender.Send(query);

        if (!result.Success)
        {
            return TypedResults.NotFound(result);
        }

        return TypedResults.Ok(result);
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> MarkSessionInactive(
    int id,
    ISender sender)
    {
        var command = new MarkSessionInactiveCommand { SessionId = id };
        var result = await sender.Send(command);

        if (!result.Success)
        {
            return TypedResults.NotFound(result);
        }

        return TypedResults.Ok(result);
    }
}