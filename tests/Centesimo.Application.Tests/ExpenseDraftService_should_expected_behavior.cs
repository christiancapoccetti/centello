using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application.Tests;

public sealed class ExpenseDraftService_should_expected_behavior
{
    private static readonly RecordedAudio Audio = new(new byte[] { 1, 2, 3, 4 });
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02];
    private static readonly Error RecordingFailure = new("Test.Recording", "Registrazione non riuscita.");
    private static readonly Error TranscriptionFailure = new("Test.Transcription", "Trascrizione non riuscita.");
    private static readonly Error InterpretationFailure = new("Test.Interpretation", "Interpretazione non riuscita.");

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeTagRepository _tags = new();
    private readonly FakeInternetAccess _internet = new();
    private readonly FakeSpeechRecorder _recorder = new();
    private readonly FakeSpeechTranscriber _transcriber = new();
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
            _transcriber,
            _interpreter,
            _internet,
            _scopes);
    }

    [Fact]
    public async Task Transcribe_the_recording_with_the_user_vocabulary_and_interpret_it()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Success("Ho speso 100 euro di carburante");
        _interpreter.Draft = new ExpenseDraft(10_000, _car.CategoryId, null, null, "", DraftSource.Voice, "Ho speso 100 euro di carburante");

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsSuccess);
        Assert.Equal(Audio, _transcriber.Audio);
        Assert.Equal(["Auto", "Carburante", "Cibo"], _transcriber.Keywords);
        Assert.Equal("Ho speso 100 euro di carburante", _interpreter.Phrase);
        Assert.Equal(["Auto", "Cibo"], _interpreter.Categories.Select(category => category.Name));
        Assert.Equal("Ho speso 100 euro di carburante", _service.LastTranscription);
        Assert.Same(_interpreter.Draft, result.Value);
        Assert.Equal(1, _scopes.CreatedCount);
    }

    [Fact]
    public async Task Stop_without_transcribing_when_the_recording_failed()
    {
        _recorder.StopResult = Result<RecordedAudio>.Failure(RecordingFailure);

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(RecordingFailure, result.Error);
        Assert.Equal(0, _transcriber.CallCount);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Skip_the_interpretation_when_the_transcription_failed()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Failure(TranscriptionFailure);

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(TranscriptionFailure, result.Error);
        Assert.Equal("", _service.LastTranscription);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Keep_the_transcription_when_the_interpretation_failed()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Success("cento euro carburante");
        _interpreter.Failure = InterpretationFailure;

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(InterpretationFailure, result.Error);
        Assert.Equal("cento euro carburante", _service.LastTranscription);
    }

    [Fact]
    public async Task Fail_before_transcribing_when_the_categories_cannot_be_loaded()
    {
        var failure = new Error("Test.Categories", "Categorie non disponibili.");
        _recorder.Audio = Audio;
        _categories.Failure = failure;

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(failure, result.Error);
        Assert.Equal(0, _transcriber.CallCount);
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
    public async Task Stop_the_recording_but_not_call_openai_when_the_connection_was_lost()
    {
        _recorder.Audio = Audio;
        _internet.IsAvailable = false;

        var result = await _service.PrepareFromSpeech();

        Assert.True(result.IsFailure);
        Assert.Equal(DraftErrors.NoInternet, result.Error);
        Assert.Equal(1, _recorder.StopCount);
        Assert.Equal(0, _transcriber.CallCount);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Forget_the_previous_transcription_when_a_new_recording_starts()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Success("vecchia frase");
        _interpreter.Failure = InterpretationFailure;
        await _service.PrepareFromSpeech();

        await _service.StartListening();

        Assert.Equal("", _service.LastTranscription);
        Assert.Equal(1, _recorder.StartCount);
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
        Assert.Equal("", _service.LastTranscription);
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
    public async Task Refuse_a_pdf_over_the_pdf_limit_without_calling_openai()
    {
        var oversized = new byte[DocumentLimits.MaximumPdfBytes + 1];
        "%PDF-"u8.CopyTo(oversized);

        var result = await _service.PrepareFromDocument(oversized);

        Assert.Equal(PhotoErrors.PdfTooLarge, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Not_call_openai_for_a_file_that_is_neither_an_image_nor_a_pdf()
    {
        var result = await _service.PrepareFromDocument("plain text"u8.ToArray());

        Assert.True(result.IsFailure);
        Assert.Equal(PhotoErrors.Unsupported, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
        Assert.Equal(0, _scopes.CreatedCount);
    }

    [Fact]
    public async Task Not_call_openai_without_internet_for_a_photo()
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

    private sealed class FakeInternetAccess : IInternetAccess
    {
        public bool IsAvailable { get; set; } = true;
    }

    private sealed class FakeSpeechRecorder : ISpeechRecorder
    {
        public RecordedAudio Audio { get; set; } = new(ReadOnlyMemory<byte>.Empty);
        public Result<RecordedAudio>? StopResult { get; set; }
        public int StartCount { get; private set; }
        public int StopCount { get; private set; }
        public bool IsListening => false;

        public Task<Result> Start(CancellationToken cancellationToken = default)
        {
            StartCount++;
            return Task.FromResult(Result.Success());
        }

        public Task<Result<RecordedAudio>> Stop(CancellationToken cancellationToken = default)
        {
            StopCount++;
            return Task.FromResult(StopResult ?? Result<RecordedAudio>.Success(Audio));
        }

        public Task Cancel() => Task.CompletedTask;
    }

    private sealed class FakeSpeechTranscriber : ISpeechTranscriber
    {
        public Result<string> Result { get; set; } = Result<string>.Success("");
        public RecordedAudio? Audio { get; private set; }
        public IReadOnlyCollection<string> Keywords { get; private set; } = [];
        public int CallCount { get; private set; }

        public Task<Result<string>> Transcribe(
            RecordedAudio audio,
            IReadOnlyCollection<string> keywords,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Audio = audio;
            Keywords = keywords;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeExpenseInterpreter : IExpenseInterpreter
    {
        public ExpenseDraft Draft { get; set; } = new(null, null, null, null, "", DraftSource.Voice, "");
        public Error? Failure { get; set; }
        public string Phrase { get; private set; } = "";
        public ExpenseDocument? Document { get; private set; }
        public IReadOnlyCollection<DraftCategory> Categories { get; private set; } = [];
        public int CallCount { get; private set; }

        public Task<Result<ExpenseDraft>> Interpret(
            string phrase,
            IReadOnlyCollection<DraftCategory> categories,
            DateOnly today,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Phrase = phrase;
            Categories = categories;
            return Task.FromResult(Failure is null
                ? Result<ExpenseDraft>.Success(Draft)
                : Result<ExpenseDraft>.Failure(Failure));
        }

        public Task<Result<ExpenseDraft>> Interpret(
            ExpenseDocument document,
            IReadOnlyCollection<DraftCategory> categories,
            DateOnly today,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Document = document;
            Categories = categories;
            return Task.FromResult(Failure is null
                ? Result<ExpenseDraft>.Success(Draft)
                : Result<ExpenseDraft>.Failure(Failure));
        }
    }

    private sealed class ServiceMap(CategoryService categories, TagService tags) : IServiceProvider
    {
        public object? GetService(Type serviceType) =>
            serviceType == typeof(CategoryService) ? categories
            : serviceType == typeof(TagService) ? tags
            : null;
    }

    private sealed class CountingScopeFactory(IServiceProvider provider) : IServiceScopeFactory
    {
        public int CreatedCount { get; private set; }

        public IServiceScope CreateScope()
        {
            CreatedCount++;
            return new Scope(provider);
        }

        private sealed class Scope(IServiceProvider provider) : IServiceScope
        {
            public IServiceProvider ServiceProvider => provider;

            public void Dispose()
            {
            }
        }
    }
}
