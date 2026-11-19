using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Infrastructure.Tests;

public sealed class ServiceCollectionExtensions_should_expected_behavior
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructure(Path.Combine(Path.GetTempPath(), "centesimo-di-test.db3"));
        services.AddSingleton<IOpenAiApiKeyStore, InMemoryOpenAiApiKeyStore>();
        services.AddSingleton<ISpeechRecorder, SilentSpeechRecorder>();
        services.AddSingleton<IInternetAccess, AlwaysOnlineInternetAccess>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Register_the_openai_interpreter_and_transcriber()
    {
        using var provider = BuildProvider();

        Assert.IsType<OpenAiExpenseTextInterpreter>(provider.GetRequiredService<IExpenseTextInterpreter>());
        Assert.IsType<OpenAiSpeechTranscriber>(provider.GetRequiredService<ISpeechTranscriber>());
    }

    [Fact]
    public void Read_the_api_key_from_the_openai_settings()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<OpenAiSettings>(), provider.GetRequiredService<IOpenAiApiKeyProvider>());
    }

    [Fact]
    public void Build_the_speech_draft_service_from_the_registered_providers()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<SpeechExpenseDraftService>());
    }

    [Fact]
    public void Build_the_home_overview_service_with_the_system_clock()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<HomeOverviewService>());
    }

    [Fact]
    public void Share_one_openai_client_between_transcriber_and_interpreter()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<OpenAiClient>(), provider.GetRequiredService<OpenAiClient>());
    }

    private sealed class AlwaysOnlineInternetAccess : IInternetAccess
    {
        public bool IsAvailable => true;
    }

    private sealed class SilentSpeechRecorder : ISpeechRecorder
    {
        public bool IsListening => false;
        public Task<Result> Start(CancellationToken cancellationToken = default) => Task.FromResult(Result.Success());
        public Task<Result<RecordedAudio>> Stop(CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<RecordedAudio>.Success(new RecordedAudio(ReadOnlyMemory<byte>.Empty)));
        public Task Cancel() => Task.CompletedTask;
    }

    private sealed class InMemoryOpenAiApiKeyStore : IOpenAiApiKeyStore
    {
        public Task<string> GetApiKey() => Task.FromResult("");
        public Task SetApiKey(string apiKey) => Task.CompletedTask;
        public void ClearApiKey()
        {
        }
    }
}
