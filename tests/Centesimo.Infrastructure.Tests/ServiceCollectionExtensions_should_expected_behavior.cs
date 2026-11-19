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
    public void Share_one_openai_client_between_transcriber_and_interpreter()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<OpenAiClient>(), provider.GetRequiredService<OpenAiClient>());
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
