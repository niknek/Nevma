namespace Nevma.Planning.Api.Application;

public sealed class PlanningConcurrencyException(string message, Exception innerException)
    : Exception(message, innerException);
