namespace Centello.Application;

public sealed class FeedbackForm
{
    private Uri? _url;
    public Uri? Url => Volatile.Read(ref _url);
    public event Action? Changed;

    public void Apply(string? value)
    {
        var url = Uri.TryCreate(value, UriKind.Absolute, out var parsed) && parsed.Scheme == Uri.UriSchemeHttps
            && parsed.Host.Length > 0 ? parsed : null;
        var previous = Interlocked.Exchange(ref _url, url);
        if (previous?.AbsoluteUri != url?.AbsoluteUri)
            Changed?.Invoke();
    }
}
