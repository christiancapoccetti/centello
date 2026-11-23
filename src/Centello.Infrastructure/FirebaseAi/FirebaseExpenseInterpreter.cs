using Centello.Application;

namespace Centello.Infrastructure;

public sealed class FirebaseExpenseInterpreter(IFirebaseGeneration generation, DraftConsent consent) : IExpenseInterpreter
{
    private const int MaximumEncodedRequestBytes = 20_000_000;
    private const int RequestEnvelopeBudgetBytes = 64 * 1024;

    public async Task<Result<ExpenseDraft>> Interpret(
        RecordedAudio audio,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        if (audio.Pcm16.IsEmpty)
            return Result<ExpenseDraft>.Failure(SpeechErrors.EmptyTranscription);

        var catalog = new ExpenseInterpretationCatalog(categories);
        return await Interpret(
            "audio/wav", WavEncoder.Encode(audio), catalog, today, DraftSource.Voice,
            ExpenseInterpretationRequestBuilder.Voice(catalog, today), cancellationToken);
    }

    public Task<Result<ExpenseDraft>> Interpret(
        ExpenseDocument document,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var catalog = new ExpenseInterpretationCatalog(categories);
        return Interpret(
            document.MediaType, document.Bytes, catalog, today, DraftSource.Of(document),
            ExpenseInterpretationRequestBuilder.Document(catalog, today), cancellationToken);
    }

    private async Task<Result<ExpenseDraft>> Interpret(
        string mediaType,
        ReadOnlyMemory<byte> bytes,
        ExpenseInterpretationCatalog catalog,
        DateOnly today,
        DraftSource source,
        string prompt,
        CancellationToken cancellationToken)
    {
        if (EncodedSize(prompt, bytes) + RequestEnvelopeBudgetBytes > MaximumEncodedRequestBytes)
            return Result<ExpenseDraft>.Failure(DraftErrors.RequestTooLarge);

        if (cancellationToken.IsCancellationRequested)
            return Result<ExpenseDraft>.Failure(DraftErrors.Canceled);

        if (!consent.IsEnabled)
            return Result<ExpenseDraft>.Failure(DraftErrors.ConsentRequired);

        Result<FirebaseGenerationResponse> response;
        try
        {
            response = await generation.Generate(prompt, mediaType, bytes, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<ExpenseDraft>.Failure(DraftErrors.Canceled);
        }

        if (cancellationToken.IsCancellationRequested)
            return Result<ExpenseDraft>.Failure(DraftErrors.Canceled);

        if (response.IsFailure)
            return Result<ExpenseDraft>.Failure(response.Error);

        var parsed = ExpenseInterpretationResponseParser.Parse(response.Value.Json);
        if (parsed.IsFailure)
            return Result<ExpenseDraft>.Failure(parsed.Error);

        var (interpretation, heard) = parsed.Value;
        if (source == DraftSource.Voice && heard.Length == 0)
            return Result<ExpenseDraft>.Failure(SpeechErrors.EmptyTranscription);

        return Result<ExpenseDraft>.Success(ExpenseInterpretationResolver.Resolve(
            interpretation, catalog, today, source, source == DraftSource.Voice ? heard : ""));
    }

    private static long EncodedSize(string prompt, ReadOnlyMemory<byte> bytes)
    {
        var jsonPrompt = System.Text.Json.JsonEncodedText.Encode(prompt);
        return ((bytes.Length + 2L) / 3 * 4) + jsonPrompt.EncodedUtf8Bytes.Length;
    }
}
