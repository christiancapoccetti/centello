using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.Infrastructure.Tests;

public sealed class ServiceCollectionExtensions_should_expected_behavior
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddInfrastructure(Path.Combine(Path.GetTempPath(), "centello-di-test.db3"));
        services.AddSingleton<IDraftConsentStore, InMemoryDraftConsentStore>();
        services.AddSingleton<IFirebaseGeneration, StubFirebaseGeneration>();
        services.AddSingleton<IPdfPageCounter, StubPdfPageCounter>();
        services.AddSingleton<ISpeechRecorder, SilentSpeechRecorder>();
        services.AddSingleton<IInternetAccess, AlwaysOnlineInternetAccess>();
        services.AddSingleton<IRecurringPaymentReminder, NoOpRecurringPaymentReminder>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Register_the_firebase_interpreter()
    {
        using var provider = BuildProvider();

        Assert.IsType<FirebaseExpenseInterpreter>(provider.GetRequiredService<IExpenseInterpreter>());
    }

    [Fact]
    public void Build_the_speech_draft_service_from_the_registered_providers()
    {
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetRequiredService<ExpenseDraftService>());
    }

    [Fact]
    public void Build_the_home_overview_service_with_the_system_clock()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<HomeOverviewService>());
    }

    [Fact]
    public void Build_the_expense_history_service_with_the_system_clock()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ExpenseHistoryService>());
    }

    [Fact]
    public void Build_the_category_detail_service_with_the_system_clock()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<CategoryDetailService>());
    }

    [Fact]
    public void Build_the_expense_service_with_the_default_id_source()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ExpenseService>());
    }

    [Fact]
    public void Build_the_expense_export_service_with_the_system_clock()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ExpenseExportService>());
    }

    [Fact]
    public void Build_the_data_backup_over_the_scoped_database()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        Assert.IsType<SqliteDataBackup>(scope.ServiceProvider.GetRequiredService<IDataBackup>());
    }

    [Fact]
    public void Share_one_recurring_payment_automation()
    {
        using var provider = BuildProvider();

        Assert.Same(provider.GetRequiredService<RecurringPaymentAutomation>(),
            provider.GetRequiredService<RecurringPaymentAutomation>());
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

    private sealed class InMemoryDraftConsentStore : IDraftConsentStore
    {
        public int AcceptedVersion { get; set; }
    }

    private sealed class StubFirebaseGeneration : IFirebaseGeneration
    {
        public Task<Result<FirebaseGenerationResponse>> Generate(
            string prompt, string mediaType, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
            throw new InvalidOperationException();
    }
    private sealed class StubPdfPageCounter : IPdfPageCounter
    {
        public Task<Result<int>> Count(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
            throw new InvalidOperationException();
    }
}
