using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class ExpenseDraftService_should_expected_behavior
{
    private static readonly RecordedAudio Audio = new(new byte[] { 1, 2, 3, 4 });
    private static readonly RecordedAudio Full = new(new byte[SpeechRecordingLimits.MaximumAudioBytes]);
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02];
    private static readonly Error RecordingFailure = new("Test.Recording", "Registrazione non riuscita.");
    private static readonly Error InterpretationFailure = new("Test.Interpretation", "Interpretazione non riuscita.");

    private readonly FakeDraftConsentStore _consentStore = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeTagRepository _tags = new();
    private readonly FakePdfPageCounter _pdfPages = new();
    private readonly FakeInternetAccess _internet = new();
    private readonly FakeSpeechRecorder _recorder = new();
    private readonly FakeExpenseInterpreter _interpreter = new();
    private readonly FakeAiAccessStore _aiStore = new();
    private readonly FakeAiRemoteConfig _remote = new();
    private readonly FakeAiInviteVerifier _verifier = new();
    private readonly AiAccess _access;
    private readonly CountingScopeFactory _scopes;
    private readonly ExpenseDraftService _service;
    private readonly Category _food = new(Guid.NewGuid(), "Cibo", "food", "#176B5B");
    private readonly Category _car = new(Guid.NewGuid(), "Auto", "car", "#176B5B");

    public ExpenseDraftService_should_expected_behavior()
    {
        var archived = new Category(Guid.NewGuid(), "Vecchia", "gear", "#176B5B");
        archived.Archive();
        _categories.Items.AddRange([_food, _car, archived]);
        _tags.Items.Add(new Tag(Guid.NewGuid(), _car.CategoryId, "Carburante"));
        _scopes = new CountingScopeFactory(new ServiceMap(
            new CategoryService(_categories),
            new TagService(_categories, _tags)));
        _access = new AiAccess(_aiStore, _remote, _verifier, TimeProvider.System);
        _service = new ExpenseDraftService(
            _recorder,
            _interpreter,
            _internet,
            new DraftConsent(_consentStore),
            _pdfPages,
            _scopes,
            _access);
    }

    [Fact]
    public async Task Send_a_full_recording()
    {
        _recorder.Audio = Full;
        _interpreter.Draft = new ExpenseDraft(10_000, _car.CategoryId, null, null, "", DraftSource.Voice, "cento euro carburante");

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsSuccess);
        Assert.Equal(Full, _interpreter.Audio);
        Assert.Same(_interpreter.Draft, result.Value);
        Assert.Equal(1, _aiStore.State.Used);
    }

    [Fact]
    public async Task Send_the_recording_and_active_catalog_to_the_interpreter()
    {
        _recorder.Audio = Audio;
        _interpreter.Draft = new ExpenseDraft(10_000, _car.CategoryId, null, null, "", DraftSource.Voice, "Ho speso 100 euro di carburante");

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsSuccess);
        Assert.Equal(Audio, _interpreter.Audio);
        Assert.Equal(["Auto", "Cibo"], _interpreter.Categories.Select(category => category.Name));
        Assert.Same(_interpreter.Draft, result.Value);
        Assert.Equal(1, _scopes.CreatedCount);
    }

    [Fact]
    public async Task Stop_without_interpreting_when_the_recording_failed()
    {
        _recorder.StopResult = Result<RecordedAudio>.Failure(RecordingFailure);

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(RecordingFailure, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Fail_before_interpreting_when_the_categories_cannot_be_loaded()
    {
        var failure = new Error("Test.Categories", "Categorie non disponibili.");
        _recorder.Audio = Audio;
        _categories.Failure = failure;

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(failure, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Not_start_recording_without_internet()
    {
        _internet.IsAvailable = false;

        var result = await _service.StartListening();

        Assert.True(result.IsFailure);
        Assert.Equal(DraftErrors.NoInternet, result.Error);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Stop_the_recording_but_not_call_the_interpreter_when_the_connection_was_lost()
    {
        _recorder.Audio = Audio;
        _internet.IsAvailable = false;

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(DraftErrors.NoInternet, result.Error);
        Assert.Equal(1, _recorder.StopCount);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Stop_the_recording_before_observing_preparation_cancellation()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.PrepareFromSpeech(canceled.Token));

        Assert.Equal(1, _recorder.StopCount);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Interpret_a_photo_with_the_user_categories()
    {
        _interpreter.Draft = new ExpenseDraft(1_250, _food.CategoryId, null, new DateOnly(2026, 11, 18), "Conad", DraftSource.Photo, "");

        var result = await _service.PrepareFromDocument(Jpeg);

        Assert.True(result.IsSuccess);
        Assert.Same(_interpreter.Draft, result.Value);
        Assert.Equal(Jpeg, _interpreter.Document!.Bytes.ToArray());
        Assert.Equal(["Auto", "Cibo"], _interpreter.Categories.Select(category => category.Name));
        Assert.Equal(1, _scopes.CreatedCount);
    }

    [Fact]
    public async Task Interpret_a_pdf_like_a_photo()
    {
        byte[] pdf = [.. "%PDF-1.7"u8, 0x0A];
        _interpreter.Draft = new ExpenseDraft(8_990, _food.CategoryId, null, new DateOnly(2026, 11, 10), "Enel", DraftSource.Pdf, "");

        var result = await _service.PrepareFromDocument(pdf);

        Assert.Same(_interpreter.Draft, result.Value);
        Assert.True(_interpreter.Document!.IsPdf);
        Assert.Equal(pdf, _interpreter.Document.Bytes.ToArray());
    }

    [Fact]
    public async Task Refuse_a_pdf_with_too_many_pages_before_calling_the_interpreter()
    {
        _pdfPages.Pages = DocumentLimits.MaximumPdfPages + 1;

        var result = await _service.PrepareFromDocument("%PDF-1.7"u8.ToArray());

        Assert.Equal(DraftErrors.PdfTooManyPages, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Refuse_a_pdf_over_the_pdf_limit_without_calling_the_interpreter()
    {
        var oversized = new byte[DocumentLimits.MaximumPdfBytes + 1];
        "%PDF-"u8.CopyTo(oversized);

        var result = await _service.PrepareFromDocument(oversized);

        Assert.Equal(PhotoErrors.PdfTooLarge, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Not_call_the_interpreter_for_a_file_that_is_neither_an_image_nor_a_pdf()
    {
        var result = await _service.PrepareFromDocument("plain text"u8.ToArray());

        Assert.True(result.IsFailure);
        Assert.Equal(PhotoErrors.Unsupported, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
        Assert.Equal(0, _scopes.CreatedCount);
    }

    [Fact]
    public async Task Not_call_the_interpreter_without_internet_for_a_photo()
    {
        _internet.IsAvailable = false;

        var result = await _service.PrepareFromDocument(Jpeg);

        Assert.True(result.IsFailure);
        Assert.Equal(DraftErrors.NoInternet, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Fail_before_interpreting_a_photo_when_the_categories_cannot_be_loaded()
    {
        var failure = new Error("Test.Categories", "Categorie non disponibili.");
        _categories.Failure = failure;

        var result = await _service.PrepareFromDocument(Jpeg);

        Assert.True(result.IsFailure);
        Assert.Equal(failure, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Return_the_interpretation_failure_for_a_photo()
    {
        _interpreter.Failure = InterpretationFailure;

        var result = await _service.PrepareFromDocument(Jpeg);

        Assert.Equal(InterpretationFailure, result.Error);
        Assert.Equal(0, _aiStore.State.Used);
    }

    [Fact]
    public async Task Deny_exhausted_voice_photos_and_pdfs_before_interpretation()
    {
        _aiStore.State = new AiAccessState(5, "");
        _recorder.Audio = Audio;

        Assert.Equal(DraftErrors.TrialExhausted, (await _service.StartListening()).Error);
        Assert.Equal(DraftErrors.TrialExhausted, (await _service.PrepareFromSpeech()).Error);
        Assert.Equal(DraftErrors.TrialExhausted, (await _service.PrepareFromDocument(Jpeg)).Error);
        Assert.Equal(DraftErrors.TrialExhausted, (await _service.PrepareFromDocument("%PDF-1.7"u8.ToArray())).Error);
        Assert.Equal(0, _recorder.StartCount);
        Assert.Equal(1, _recorder.StopCount);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Let_only_one_concurrent_draft_take_the_last_trial()
    {
        _aiStore.State = new AiAccessState(4, "");
        _interpreter.Draft = new ExpenseDraft(1_000, null, null, null, "", DraftSource.Photo, "");
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _interpreter.Gate = complete.Task;

        var first = _service.PrepareFromDocument(Jpeg);
        var second = _service.PrepareFromDocument("%PDF-1.7"u8.ToArray());
        complete.SetResult();

        Assert.True((await first).IsSuccess);
        Assert.Equal(DraftErrors.TrialExhausted, (await second).Error);
        Assert.Equal(1, _interpreter.CallCount);
        Assert.Equal(5, _aiStore.State.Used);
    }

    [Fact]
    public async Task Keep_the_trial_when_interpretation_returns_an_empty_draft()
    {
        _interpreter.Draft = new ExpenseDraft(null, null, null, null, "", DraftSource.Photo, "");

        Assert.True((await _service.PrepareFromDocument(Jpeg)).IsSuccess);
        Assert.Equal(0, _aiStore.State.Used);
    }

    [Fact]
    public async Task Keep_the_trial_when_a_success_arrives_after_cancellation()
    {
        _interpreter.Draft = new ExpenseDraft(1_000, null, null, null, "", DraftSource.Photo, "");
        using var canceled = new CancellationTokenSource();
        _interpreter.OnInterpret = canceled.Cancel;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _service.PrepareFromDocument(Jpeg, canceled.Token));

        Assert.Equal(0, _aiStore.State.Used);
    }

    [Fact]
    public async Task Keep_the_trial_and_withhold_the_draft_when_persistence_fails()
    {
        _interpreter.Draft = new ExpenseDraft(1_000, null, null, null, "", DraftSource.Photo, "");
        _aiStore.FailWrite = true;

        Assert.Equal(DraftErrors.Unavailable, (await _service.PrepareFromDocument(Jpeg)).Error);
        Assert.Equal(0, _aiStore.State.Used);

        _aiStore.FailWrite = false;
        Assert.True((await _service.PrepareFromDocument(Jpeg)).IsSuccess);
        Assert.Equal(1, _aiStore.State.Used);
    }

    [Fact]
    public async Task Use_a_current_invite_without_spending_the_trial_and_require_ai_consent()
    {
        _aiStore.State = new AiAccessState(4, "invitation");
        _verifier.Valid["invitation"] = "a1b2c3d4e5f600000000000000000000";
        _interpreter.Draft = new ExpenseDraft(1_000, null, null, null, "", DraftSource.Photo, "");
        _consentStore.AcceptedVersion = 0;

        Assert.Equal(DraftErrors.ConsentRequired, (await _service.PrepareFromDocument(Jpeg)).Error);
        Assert.Empty(_remote.Requests);
        Assert.Equal(0, _interpreter.CallCount);

        _consentStore.AcceptedVersion = DraftConsent.CurrentVersion;
        Assert.True((await _service.PrepareFromDocument(Jpeg)).IsSuccess);
        Assert.Equal(4, _aiStore.State.Used);
    }

    [Fact]
    public async Task Withhold_an_inflight_draft_when_its_invite_is_revoked()
    {
        const string inviteId = "a1b2c3d4e5f600000000000000000000";
        _aiStore.State = new AiAccessState(4, "invitation");
        _verifier.Valid["invitation"] = inviteId;
        _interpreter.Draft = new ExpenseDraft(1_000, null, null, null, "", DraftSource.Photo, "");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _interpreter.OnInterpret = started.SetResult;
        _interpreter.Gate = complete.Task;

        var inFlight = _service.PrepareFromDocument(Jpeg);
        await started.Task;
        _remote.Publish(new AiRemoteSettings("", new HashSet<string> { inviteId }));
        complete.SetResult();

        Assert.Equal(DraftErrors.InviteRevoked, (await inFlight).Error);
        Assert.Equal(new AiAccessState(4, ""), _aiStore.State);
        _interpreter.OnInterpret = null;
        Assert.True((await _service.PrepareFromDocument(Jpeg)).IsSuccess);
        Assert.Equal(5, _aiStore.State.Used);
    }
}
