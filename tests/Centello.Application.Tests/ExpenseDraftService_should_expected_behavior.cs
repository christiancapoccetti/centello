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
        _service = new ExpenseDraftService(
            _recorder,
            _interpreter,
            _internet,
            new DraftConsent(_consentStore),
            _pdfPages,
            _scopes);
    }

    [Fact]
    public void Treat_a_recording_of_the_maximum_length_as_full()
    {
        Assert.True(Full.IsFull);
        Assert.False(new RecordedAudio(new byte[SpeechRecordingLimits.MaximumAudioBytes - 1]).IsFull);
        Assert.False(Audio.IsFull);
    }

    [Fact]
    public async Task Refuse_a_full_recording_when_asked_to()
    {
        _recorder.Audio = Full;

        var result = await _service.PrepareFromSpeech(WhenFull.Refuse);

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.TooLong, result.Error);
        Assert.Equal("Il comando vocale è troppo lungo. Riprova con una frase più breve.", result.Error.Message);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Send_a_full_recording_when_asked_to()
    {
        _recorder.Audio = Full;
        _interpreter.Draft = new ExpenseDraft(10_000, _car.CategoryId, null, null, "", DraftSource.Voice, "cento euro carburante");

        var result = await _service.PrepareFromSpeech(WhenFull.Send);

        Assert.True(result.IsSuccess);
        Assert.Equal(Full, _interpreter.Audio);
        Assert.Same(_interpreter.Draft, result.Value);
    }

    [Fact]
    public async Task Send_a_recording_below_the_limit_whatever_the_choice()
    {
        _recorder.Audio = Audio;

        var result = await _service.PrepareFromSpeech(WhenFull.Refuse);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _interpreter.CallCount);
    }

    [Fact]
    public async Task Send_the_recording_and_active_catalog_to_the_interpreter()
    {
        _recorder.Audio = Audio;
        _interpreter.Draft = new ExpenseDraft(10_000, _car.CategoryId, null, null, "", DraftSource.Voice, "Ho speso 100 euro di carburante");

        var result = await _service.PrepareFromSpeech(WhenFull.Refuse);

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

        var result = await _service.PrepareFromSpeech(WhenFull.Refuse);

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

        var result = await _service.PrepareFromSpeech(WhenFull.Refuse);

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

        var result = await _service.PrepareFromSpeech(WhenFull.Refuse);

        Assert.True(result.IsFailure);
        Assert.Equal(DraftErrors.NoInternet, result.Error);
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
    }
}
