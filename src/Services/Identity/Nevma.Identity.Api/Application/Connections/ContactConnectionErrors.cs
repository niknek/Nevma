using Nevma.ServiceDefaults.Errors;

namespace Nevma.Identity.Api.Application.Connections;

public static class ContactConnectionErrors
{
    public static readonly Error SelfConnection = new(
        "connections.self_connection",
        "A user cannot send a connection request to themselves.",
        ErrorType.Validation);

    public static readonly Error TargetNotFound = new(
        "connections.target_not_found",
        "The target user was not found.",
        ErrorType.NotFound);

    public static readonly Error NotFound = new(
        "connections.not_found",
        "The connection request was not found.",
        ErrorType.NotFound);

    public static readonly Error AlreadyExists = new(
        "connections.already_exists",
        "A pending or accepted connection already exists between these users.",
        ErrorType.Conflict);

    public static readonly Error Forbidden = new(
        "connections.forbidden",
        "Only the requested user can respond to this connection request.",
        ErrorType.Forbidden);

    public static readonly Error AlreadyHandled = new(
        "connections.already_handled",
        "The connection request has already been handled.",
        ErrorType.Conflict);
}
