using Centesimo.Application;

namespace Centesimo.App.Theming;

/// <summary>{t:Theme Ink}: an AppThemeBinding to the token's light/dark pair. Works in Setters and on Brush
/// properties because it forwards to MAUI's AppThemeBindingExtension (AppThemeBinding itself is internal).
/// The service list names only IProvideValueTarget: the converter provider the forwarded extension also reads
/// is internal to MAUI, and every XamlServiceProvider installs it on construction.</summary>
[ContentProperty(nameof(Color))]
[RequireService([typeof(IProvideValueTarget)])]
public sealed class ThemeExtension : IMarkupExtension<BindingBase>
{
    public ThemeColor Color { get; set; }

    public BindingBase ProvideValue(IServiceProvider serviceProvider) =>
        ((IMarkupExtension<BindingBase>)new AppThemeBindingExtension
        {
            Light = ThemeColors.Light(Color),
            Dark = ThemeColors.Dark(Color)
        }).ProvideValue(serviceProvider);

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
}
