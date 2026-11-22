using Centello.Application;
using Centello.Domain;

namespace Centello.App.ViewModels;

public sealed class LicensesViewModel : ObservableObject
{
    private const string LicenseAsset = "PolyForm-Noncommercial-1.0.0.md";
    private const string NoticesAsset = "THIRD_PARTY_NOTICES.txt";
    // Copied verbatim from the .NET runtime packs: they cover what those runtimes compile in, as zlib-ng or lz4.
    private static readonly (string Asset, string Name)[] RuntimeNotices =
    [
        ("DOTNET_RUNTIME_THIRD_PARTY_NOTICES.txt", "Componenti inclusi nel runtime .NET"),
        ("DOTNET_ANDROID_THIRD_PARTY_NOTICES.txt", "Componenti inclusi in .NET for Android"),
    ];

    private string _licenseTerms = "";
    private IReadOnlyList<NoticeComponent> _components = [];
    private string _componentsMessage = "";
    private bool _isLoaded;

    public string LicenseTerms { get => _licenseTerms; private set => SetProperty(ref _licenseTerms, value); }
    public IReadOnlyList<NoticeComponent> Components { get => _components; private set => SetProperty(ref _components, value); }
    /// <summary>Why the list is empty, or nothing while it is not.</summary>
    public string ComponentsMessage
    {
        get => _componentsMessage;
        private set
        {
            if (SetProperty(ref _componentsMessage, value))
                OnPropertyChanged(nameof(HasComponentsMessage));
        }
    }
    public bool HasComponentsMessage => ComponentsMessage.HasValue();

    public async Task Load()
    {
        if (_isLoaded)
            return;

        _isLoaded = true;
        var licenseRead = Read(LicenseAsset);
        var noticesRead = Read(NoticesAsset);
        var runtimeReads = RuntimeNotices.Select(runtime => Read(runtime.Asset)).ToList();
        await Task.WhenAll([licenseRead, noticesRead, .. runtimeReads]);

        var license = licenseRead.Result;
        LicenseTerms = license is null ? "Non è stato possibile caricare il testo della licenza." : LicenseText.FromMarkdown(license);
        var notices = noticesRead.Result is null ? [] : ThirdPartyNotices.Parse(noticesRead.Result);
        if (notices.Count == 0)
        {
            ComponentsMessage = "Non è stato possibile caricare gli avvisi di licenza.";
            return;
        }

        var runtimes = RuntimeNotices
            .Zip(runtimeReads, (runtime, read) => (runtime.Name, Text: read.Result))
            .Where(runtime => runtime.Text is not null)
            .Select(runtime => ThirdPartyNotices.Upstream(runtime.Name, runtime.Text!));
        Components = [.. notices, .. runtimes];
    }

    private static async Task<string?> Read(string asset)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(asset);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch
        {
            return null;
        }
    }
}
