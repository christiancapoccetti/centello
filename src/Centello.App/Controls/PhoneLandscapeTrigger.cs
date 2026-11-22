namespace Centello.App.Controls;

public static class PhoneLandscape
{
    public static bool IsActive(Page page)
    {
        // Android's configuration describes the whole window, including while the keyboard resizes its content.
        var configuration = page.Window?.Handler?.MauiContext?.Context?.Resources?.Configuration;
        return configuration is not null &&
            PhoneLandscapeSize.Matches(configuration.ScreenWidthDp, configuration.ScreenHeightDp);
    }
}

public sealed class PhoneLandscapeTrigger : StateTriggerBase
{
    public static readonly BindableProperty PageProperty = BindableProperty.Create(
        nameof(Page), typeof(Page), typeof(PhoneLandscapeTrigger), propertyChanged: OnPageChanged);

    public Page? Page
    {
        get => (Page?)GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    private static void OnPageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var trigger = (PhoneLandscapeTrigger)bindable;
        if (oldValue is Page previous)
        {
            previous.Loaded -= trigger.Refresh;
            previous.SizeChanged -= trigger.Refresh;
        }

        if (newValue is Page page)
        {
            page.Loaded += trigger.Refresh;
            page.SizeChanged += trigger.Refresh;
        }

        trigger.Refresh(null, EventArgs.Empty);
    }

    private void Refresh(object? sender, EventArgs e) => SetActive(Page is { } page && PhoneLandscape.IsActive(page));
}
