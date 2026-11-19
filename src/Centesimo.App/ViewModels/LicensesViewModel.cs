using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.ViewModels;

public sealed class LicensesViewModel : ObservableObject
{
    private const string LicenseAsset = "GPL-3.0.txt";
    private const string NoticesAsset = "THIRD_PARTY_NOTICES.txt";

    private string _gplText = "";
    private IReadOnlyList<NoticeComponent> _components = [];
    private string _componentsMessage = "";
    private bool _isLoaded;

    public string GplText { get => _gplText; private set => SetProperty(ref _gplText, value); }
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
        var license = await Read(LicenseAsset);
        GplText = license is null ? "Non è stato possibile caricare il testo della licenza." : LicenseText.Reflow(license);
        var notices = await Read(NoticesAsset);
        Components = notices is null ? [] : ThirdPartyNotices.Parse(notices);
        if (Components.Count == 0)
            ComponentsMessage = "Non è stato possibile caricare gli avvisi di licenza.";
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
