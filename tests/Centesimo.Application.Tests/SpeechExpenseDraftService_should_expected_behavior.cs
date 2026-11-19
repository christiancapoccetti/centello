using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application.Tests;

public sealed class SpeechExpenseDraftService_should_expected_behavior
{
    private static readonly RecordedAudio Audio = new(new byte[] { 1, 2, 3, 4 });
    private static readonly Error RecordingFailure = new("Test.Recording", "Registrazione non riuscita.");
    private static readonly Error TranscriptionFailure = new("Test.Transcription", "Trascrizione non riuscita.");
    private static readonly Error InterpretationFailure = new("Test.Interpretation", "Interpretazione non riuscita.");

    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeTagRepository _tags = new();
    private readonly FakeApiKeyStore _keys = new();
    private readonly FakeInternetAccess _internet = new();
    private readonly FakeSpeechRecorder _recorder = new();
    private readonly FakeSpeechTranscriber _transcriber = new();
    private readonly FakeExpenseTextInterpreter _interpreter = new();
    private readonly PendingExpenseSpeechDraft _pending = new();
    private readonly CountingScopeFactory _scopes;
    private readonly SpeechExpenseDraftService _service;
    private readonly Category _food = new(Guid.NewGuid(), "Cibo", "food", "#176B5B");
    private readonly Category _car = new(Guid.NewGuid(), "Auto", "car", "#176B5B");

    public SpeechExpenseDraftService_should_expected_behavior()
    {
        var archived = new Category(Guid.NewGuid(), "Vecchia", "gear", "#176B5B");
        archived.Archive();
        _categories.Items.AddRange([_food, _car, archived]);
        _tags.Items.Add(new Tag(Guid.NewGuid(), _car.CategoryId, "Carburante"));
        _scopes = new CountingScopeFactory(new ServiceMap(
            new CategoryService(_categories),
            new TagService(_categories, _tags)));
        _service = new SpeechExpenseDraftService(
            _recorder,
            _transcriber,
            _interpreter,
            new OpenAiSettings(_keys),
            _internet,
            _scopes,
            _pending);
    }

    [Fact]
    public async Task Be_available_only_when_an_api_key_is_saved()
    {
        Assert.False(await _service.IsAvailable());

        _keys.Key = "sk-test";

        Assert.True(await _service.IsAvailable());
    }

    [Fact]
    public async Task Transcribe_the_recording_with_the_user_vocabulary_and_interpret_it()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Success("Ho speso 100 euro di carburante");
        _interpreter.Draft = new ExpenseSpeechDraft(10_000, _car.CategoryId, "Auto", null, "", null, "", "Ho speso 100 euro di carburante");

        var result = await _service.StopAndPrepare();

        Assert.True(result.IsSuccess);
        Assert.Equal(Audio, _transcriber.Audio);
        Assert.Equal(["Auto", "Carburante", "Cibo"], _transcriber.Keywords);
        Assert.Equal("Ho speso 100 euro di carburante", _interpreter.Text);
        Assert.Equal(["Auto", "Cibo"], _interpreter.Categories.Select(category => category.Name));
        Assert.Equal("Ho speso 100 euro di carburante", _service.LastTranscription);
        Assert.Same(_interpreter.Draft, _pending.Take());
        Assert.Equal(1, _scopes.CreatedCount);
    }

    [Fact]
    public async Task Stop_without_transcribing_when_the_recording_failed()
    {
        _recorder.StopResult = Result<RecordedAudio>.Failure(RecordingFailure);

        var result = await _service.StopAndPrepare();

        Assert.True(result.IsFailure);
        Assert.Equal(RecordingFailure, result.Error);
        Assert.Equal(0, _transcriber.CallCount);
        Assert.Equal(0, _interpreter.CallCount);
        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Skip_the_interpretation_when_the_transcription_failed()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Failure(TranscriptionFailure);

        var result = await _service.StopAndPrepare();

        Assert.True(result.IsFailure);
        Assert.Equal(TranscriptionFailure, result.Error);
        Assert.Equal("", _service.LastTranscription);
        Assert.Equal(0, _interpreter.CallCount);
        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Keep_the_transcription_for_correction_when_the_interpretation_failed()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Success("cento euro carburante");
        _interpreter.Failure = InterpretationFailure;

        var result = await _service.StopAndPrepare();

        Assert.True(result.IsFailure);
        Assert.Equal(InterpretationFailure, result.Error);
        Assert.Equal("cento euro carburante", _service.LastTranscription);
        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Interpret_typed_text_without_recording_or_transcribing()
    {
        _interpreter.Draft = new ExpenseSpeechDraft(null, null, "", null, "", null, "caffè", "caffè");

        var result = await _service.PrepareFromText("caffè");

        Assert.True(result.IsSuccess);
        Assert.Equal("caffè", _interpreter.Text);
        Assert.Equal(0, _recorder.StopCount);
        Assert.Equal(0, _transcriber.CallCount);
        Assert.Same(_interpreter.Draft, _pending.Take());
        Assert.Equal(1, _scopes.CreatedCount);
    }

    [Fact]
    public async Task Leave_no_draft_when_typed_text_cannot_be_interpreted()
    {
        _interpreter.Failure = InterpretationFailure;

        var result = await _service.PrepareFromText("???");

        Assert.True(result.IsFailure);
        Assert.Equal(InterpretationFailure, result.Error);
        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Fail_when_the_categories_cannot_be_loaded()
    {
        var failure = new Error("Test.Categories", "Categorie non disponibili.");
        _categories.Failure = failure;

        var result = await _service.PrepareFromText("caffè");

        Assert.True(result.IsFailure);
        Assert.Equal(failure, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Not_start_recording_without_internet()
    {
        _internet.IsAvailable = false;

        var result = await _service.Start();

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.NoInternet, result.Error);
        Assert.Equal(0, _recorder.StartCount);
    }

    [Fact]
    public async Task Stop_the_recording_but_not_call_openai_when_the_connection_was_lost()
    {
        _recorder.Audio = Audio;
        _internet.IsAvailable = false;

        var result = await _service.StopAndPrepare();

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.NoInternet, result.Error);
        Assert.Equal(1, _recorder.StopCount);
        Assert.Equal(0, _transcriber.CallCount);
        Assert.Equal(0, _interpreter.CallCount);
    }

    [Fact]
    public async Task Not_interpret_typed_text_without_internet()
    {
        _internet.IsAvailable = false;

        var result = await _service.PrepareFromText("caffè");

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.NoInternet, result.Error);
        Assert.Equal(0, _interpreter.CallCount);
        Assert.Null(_pending.Take());
    }

    [Fact]
    public async Task Forget_the_previous_transcription_when_a_new_recording_starts()
    {
        _recorder.Audio = Audio;
        _transcriber.Result = Result<string>.Success("vecchia frase");
        _interpreter.Failure = InterpretationFailure;
        await _service.StopAndPrepare();

        await _service.Start();

        Assert.Equal("", _service.LastTranscription);
        Assert.Equal(1, _recorder.StartCount);
    }

    private sealed class FakeApiKeyStore : IOpenAiApiKeyStore
    {
        public string Key { get; set; } = "";
        public Task<string> GetApiKey() => Task.FromResult(Key);
        public Task SetApiKey(string apiKey) => Task.CompletedTask;
        public void ClearApiKey() => Key = "";
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

    private sealed class FakeExpenseTextInterpreter : IExpenseTextInterpreter
    {
        public ExpenseSpeechDraft Draft { get; set; } = new(null, null, "", null, "", null, "", "");
        public Error? Failure { get; set; }
        public string Text { get; private set; } = "";
        public IReadOnlyCollection<SpeechCategory> Categories { get; private set; } = [];
        public int CallCount { get; private set; }

        public Task<Result<ExpenseSpeechDraft>> Interpret(
            string text,
            IReadOnlyCollection<SpeechCategory> categories,
            DateOnly today,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            Text = text;
            Categories = categories;
            return Task.FromResult(Failure is null
                ? Result<ExpenseSpeechDraft>.Success(Draft)
                : Result<ExpenseSpeechDraft>.Failure(Failure));
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
