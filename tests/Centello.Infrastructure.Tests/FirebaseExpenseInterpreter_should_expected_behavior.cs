using Centello.Application;

namespace Centello.Infrastructure.Tests;

public sealed class FirebaseExpenseInterpreter_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly DraftTag Pizza = new(Guid.NewGuid(), "Pizza");
    private static readonly DraftCategory Food = new(Guid.NewGuid(), "Cibo", [Pizza]);
    private static readonly DraftCategory House = new(Guid.NewGuid(), "Casa", [new(Guid.NewGuid(), "Bollette")]);
    private static readonly RecordedAudio Audio = new(new byte[] { 12, 34, 56, 78 });
    private const string VoiceResponse =
        """{"amount":12.50,"categoryKey":"c1","tagKey":"t1","date":"2026-11-22","note":"","heard":"Ieri dodici e cinquanta di pizza"}""";
    private const string EmptyResponse =
        """{"amount":null,"categoryKey":null,"tagKey":null,"date":null,"note":null,"heard":null}""";

    private readonly ConsentStore _store = new();
    private readonly Generation _generation = new();
    private readonly FirebaseExpenseInterpreter _interpreter;

    public FirebaseExpenseInterpreter_should_expected_behavior() =>
        _interpreter = new FirebaseExpenseInterpreter(_generation, new DraftConsent(_store));

    [Fact]
    public async Task Send_one_wav_request_and_resolve_the_italian_voice_draft()
    {
        var result = await _interpreter.Interpret(Audio, [Food, House], Today);

        Assert.Equal(new ExpenseDraft(1_250, Food.CategoryId, Pizza.TagId, Today.AddDays(-1), "",
            DraftSource.Voice, "Ieri dodici e cinquanta di pizza"), result.Value);
        Assert.Equal(1, _generation.Count);
        Assert.Equal("audio/wav", _generation.MediaType);
        Assert.Equal("RIFF"u8.ToArray(), _generation.Bytes.Span[..4].ToArray());
        Assert.Equal(Audio.Pcm16.ToArray(), _generation.Bytes.Span[44..].ToArray());
        Assert.Contains("t1: Pizza", _generation.Prompt);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Send_the_document_and_reject_invalid_model_fields(bool pdf)
    {
        var bytes = pdf ? "%PDF-1.7\n"u8.ToArray() : new byte[] { 0xFF, 0xD8, 0xFF };
        _generation.Json =
            """{"amount":12.505,"categoryKey":"c1","tagKey":"t2","date":"2026-11-24","note":"  Forno Demo  ","heard":"ignore this"}""";

        var result = await _interpreter.Interpret(ExpenseDocument.Read(bytes).Value, [Food, House], Today);

        Assert.Equal(new ExpenseDraft(null, Food.CategoryId, null, null, "Forno Demo",
            pdf ? DraftSource.Pdf : DraftSource.Photo, ""), result.Value);
        Assert.Equal(pdf ? "application/pdf" : "image/jpeg", _generation.MediaType);
        Assert.Equal(bytes, _generation.Bytes.ToArray());
    }

    [Fact]
    public async Task Return_an_empty_draft_for_a_document_without_a_readable_expense()
    {
        _generation.Json = EmptyResponse;
        var document = ExpenseDocument.Read("%PDF-1.7"u8.ToArray()).Value;

        var result = await _interpreter.Interpret(document, [], Today);

        Assert.Equal(new ExpenseDraft(null, null, null, null, "", DraftSource.Pdf, ""), result.Value);
    }

    [Fact]
    public async Task Reject_a_silent_recording()
    {
        _generation.Json = EmptyResponse;

        var result = await _interpreter.Interpret(Audio, [], Today);

        Assert.Equal(SpeechErrors.EmptyTranscription, result.Error);
    }

    [Fact]
    public async Task Reject_a_request_that_exceeds_the_budget_after_encoding_the_catalog_and_pdf()
    {
        var bytes = new byte[DocumentLimits.MaximumPdfBytes];
        "%PDF-"u8.CopyTo(bytes);
        var document = ExpenseDocument.Read(bytes).Value;
        var hugeCatalog = new DraftCategory(Guid.NewGuid(), new string('è', 2_500_000), []);

        var result = await _interpreter.Interpret(document, [hugeCatalog], Today);

        Assert.Equal(DraftErrors.RequestTooLarge, result.Error);
        Assert.Equal(0, _generation.Count);
    }

    [Fact]
    public async Task Skip_dispatch_after_consent_is_revoked()
    {
        var consent = new DraftConsent(_store);
        Assert.True(consent.IsEnabled);
        consent.SetEnabled(false);

        var result = await _interpreter.Interpret(Audio, [Food], Today);

        Assert.Equal(DraftErrors.ConsentRequired, result.Error);
        Assert.Equal(0, _generation.Count);
    }

    public static TheoryData<Error> ProviderFailures => new()
    {
        DraftErrors.Blocked, DraftErrors.AccessDenied, DraftErrors.RateLimited, DraftErrors.TimedOut, DraftErrors.Unavailable
    };

    [Theory]
    [MemberData(nameof(ProviderFailures))]
    public async Task Preserve_the_provider_failure_and_allow_a_new_attempt(Error failure)
    {
        _generation.Error = failure;

        var result = await _interpreter.Interpret(Audio, [Food], Today);

        Assert.Equal(failure, result.Error);
        Assert.Equal(1, _generation.Count);
        _generation.Error = null;
        Assert.Equal(1_250, (await _interpreter.Interpret(Audio, [Food], Today)).Value.AmountCents);
        Assert.Equal(2, _generation.Count);
    }

    [Fact]
    public async Task Ignore_a_late_result_after_cancellation_and_allow_a_new_attempt()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _generation.Gate = gate.Task;
        using var cancellation = new CancellationTokenSource();

        var abandoned = _interpreter.Interpret(Audio, [Food], Today, cancellation.Token);
        cancellation.Cancel();
        _generation.Gate = null;
        var current = await _interpreter.Interpret(Audio, [Food], Today);
        gate.SetResult();
        var late = await abandoned;

        Assert.Equal(DraftErrors.Canceled, late.Error);
        Assert.Equal(1_250, current.Value.AmountCents);
        Assert.Equal(2, _generation.Count);
    }

    private sealed class ConsentStore : IDraftConsentStore
    {
        public int AcceptedVersion { get; set; } = DraftConsent.CurrentVersion;
    }

    private sealed class Generation : IFirebaseGeneration
    {
        public int Count { get; private set; }
        public string MediaType { get; private set; } = "";
        public string Prompt { get; private set; } = "";
        public ReadOnlyMemory<byte> Bytes { get; private set; }
        public string Json { get; set; } = VoiceResponse;
        public Error? Error { get; set; }
        public Task? Gate { get; set; }

        public async Task<Result<FirebaseGenerationResponse>> Generate(
            string prompt, string mediaType, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            Count++;
            Prompt = prompt;
            MediaType = mediaType;
            Bytes = bytes;
            if (Gate is { } gate)
                await gate;

            return Error is { } error
                ? Result<FirebaseGenerationResponse>.Failure(error)
                : Result<FirebaseGenerationResponse>.Success(new FirebaseGenerationResponse(Json, 42, 15));
        }
    }
}
