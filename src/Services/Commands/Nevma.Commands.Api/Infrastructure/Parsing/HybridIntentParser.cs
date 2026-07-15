using Nevma.Commands.Api.Application;

namespace Nevma.Commands.Api.Infrastructure.Parsing;

public sealed class HybridIntentParser(
    RuleBasedIntentParser rules,
    IAiProvider aiProvider,
    ICommandValidator validator) : IIntentParser
{
    public async Task<ParseResult> ParseAsync(
        string transcript,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var ruleResult = rules.Parse(transcript, now);
        if (ruleResult is ParseResult.Parsed parsed)
            return validator.Validate(parsed.Command, now);

        var aiResult = await aiProvider.InterpretAsync(transcript, now, cancellationToken);
        return aiResult switch
        {
            AiInterpretationResult.Proposed proposed => validator.Validate(proposed.Command, now),
            AiInterpretationResult.NotUnderstood notUnderstood => new ParseResult.Invalid(notUnderstood.Message),
            _ => ruleResult
        };
    }
}
