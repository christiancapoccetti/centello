using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.Core.View;
using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Platform;
using Color = Android.Graphics.Color;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;

namespace Centesimo.App;

internal sealed class VoicePanelSheet
{
    private const int SheetCorner = 28;
    private const int Gutter = 24;
    private const int Target = 48;
    private const int MicSize = 88;
    private const int LimitSeconds = SpeechRecordingLimits.MaximumDurationSeconds;
    private const string Tabular = "tnum";
    private const string ListeningTitle = "Ti ascolto…";
    private const string ListeningLead = "Dì cosa hai speso, per esempio “ieri 12 euro di pizza”.";
    private const string SendLabel = "Tocca per inviare";

    private readonly Context _context;
    private readonly IFontManager _fonts;
    private readonly FrameLayout _root;
    private readonly View _scrim;
    private readonly LinearLayout _sheet;
    private readonly TextView _title;
    private readonly TextView _lead;
    private readonly LinearLayout _timeRow;
    private readonly TextView _elapsed;
    private readonly TextView _remaining;
    private readonly FrameLayout _track;
    private readonly View _fill;
    private readonly LinearLayout _talk;
    private readonly TextView _mic;
    private readonly TextView _micLabel;
    private readonly LinearLayout _actions;
    private readonly TextView _cancel;
    private readonly TextView _primary;
    private bool _isDark;
    private DictationState _state = new DictationState.Starting();

    public VoicePanelSheet(Context context, IFontManager fonts, bool isDark)
    {
        _context = context;
        _fonts = fonts;
        _isDark = isDark;

        _scrim = new View(context) { Clickable = true };
        _scrim.Click += (_, _) => Dismissed?.Invoke();

        _title = Text(20, Fonts.Bold);
        _title.SetLineSpacing(0, 1.25f);
        _title.AccessibilityLiveRegion = AccessibilityLiveRegion.Polite;
        _lead = Text(15, Fonts.Regular);
        _lead.SetLineSpacing(0, 1.35f);

        _elapsed = Text(12, Fonts.Bold);
        _remaining = Text(12, Fonts.Regular);
        _remaining.Text = Clock(TimeSpan.FromSeconds(LimitSeconds));
        _fill = new View(context) { PivotX = 0 };
        _track = new FrameLayout(context);
        _track.AddView(_fill, new FrameLayout.LayoutParams(Match, Match));
        _timeRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        _timeRow.SetGravity(GravityFlags.CenterVertical);
        _timeRow.AddView(_elapsed);
        _timeRow.AddView(_track, new LinearLayout.LayoutParams(0, Dp(4), 1) { LeftMargin = Dp(10), RightMargin = Dp(10) });
        _timeRow.AddView(_remaining);

        _mic = Text(38, Fonts.Icons, ComplexUnitType.Dip);
        _mic.Text = MaterialSymbols.Mic;
        _mic.Gravity = GravityFlags.Center;
        _mic.Clickable = true;
        _mic.Click += (_, _) => MicTapped?.Invoke();
        _micLabel = Text(14, Fonts.SemiBold);
        _talk = new LinearLayout(context) { Orientation = Orientation.Vertical };
        _talk.SetGravity(GravityFlags.CenterHorizontal);
        _talk.AddView(_mic, new LinearLayout.LayoutParams(Dp(MicSize), Dp(MicSize)));
        _talk.AddView(_micLabel, new LinearLayout.LayoutParams(Wrap, Wrap) { TopMargin = Dp(10) });

        _cancel = Button(14, Fonts.SemiBold, horizontalPadding: 12);
        _cancel.Text = "Annulla";
        _cancel.ContentDescription = "Annulla";
        _cancel.Click += (_, _) => Dismissed?.Invoke();
        _primary = Button(16, Fonts.Bold, horizontalPadding: Gutter);
        _primary.Click += (_, _) => (_state is DictationState.NeedsApiKey ? SettingsTapped : RetryTapped)?.Invoke();
        _actions = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        _actions.SetGravity(GravityFlags.CenterVertical);
        _actions.SetMinimumHeight(Dp(56));
        _actions.AddView(_cancel, new LinearLayout.LayoutParams(Wrap, Dp(Target)) { LeftMargin = -Dp(12) });
        _actions.AddView(new View(context), new LinearLayout.LayoutParams(0, 1, 1));
        _actions.AddView(_primary, new LinearLayout.LayoutParams(Wrap, Dp(Target)));

        _sheet = new LinearLayout(context) { Orientation = Orientation.Vertical, Clickable = true };
        _sheet.SetPadding(Dp(Gutter), Dp(Gutter), Dp(Gutter), Dp(4));
        _sheet.AddView(_title);
        _sheet.AddView(_lead, new LinearLayout.LayoutParams(Match, Wrap) { TopMargin = Dp(6) });
        _sheet.AddView(_timeRow, new LinearLayout.LayoutParams(Match, Wrap) { TopMargin = Dp(22) });
        _sheet.AddView(_talk, new LinearLayout.LayoutParams(Match, Wrap) { TopMargin = Dp(26) });
        _sheet.AddView(_actions, new LinearLayout.LayoutParams(Match, Wrap) { TopMargin = Dp(18) });

        _root = new FrameLayout(context);
        _root.AddView(_scrim, new FrameLayout.LayoutParams(Match, Match));
        _root.AddView(_sheet, new FrameLayout.LayoutParams(Match, Wrap, GravityFlags.Bottom));
        ViewCompat.SetOnApplyWindowInsetsListener(_root, new SheetInsets(_sheet, Dp(4)));

        Render();
    }

    public View Root => _root;

    public event Action? MicTapped;
    public event Action? Dismissed;
    public event Action? RetryTapped;
    public event Action? SettingsTapped;

    public void Show(DictationState state)
    {
        _state = state;
        Render();
    }

    public void Recolor(bool isDark)
    {
        _isDark = isDark;
        Render();
    }

    private static int Match => ViewGroup.LayoutParams.MatchParent;

    private static int Wrap => ViewGroup.LayoutParams.WrapContent;

    private void Render()
    {
        switch (_state)
        {
            case DictationState.Starting:
                ShowRecording(ListeningTitle, ListeningLead, TimeSpan.Zero, SendLabel, isActive: false);
                break;
            case DictationState.Listening listening:
                ShowRecording(ListeningTitle, ListeningLead, listening.Heard, SendLabel, isActive: true);
                break;
            case DictationState.Preparing preparing:
                ShowRecording("Preparo la bozza…", "Poi la controlli prima di salvarla.", preparing.Heard, "Invio in corso", isActive: false);
                break;
            case DictationState.NeedsApiKey:
                ShowMessage("Configura la chiave API OpenAI nelle impostazioni per usare i comandi vocali.", "Apri impostazioni");
                break;
            case DictationState.Failed failed:
                ShowMessage(failed.Error.Message, "Riprova");
                break;
        }

        Paint();
    }

    private void ShowRecording(string title, string lead, TimeSpan heard, string label, bool isActive)
    {
        _title.Text = title;
        _lead.Text = lead;
        _timeRow.Visibility = ViewStates.Visible;
        _elapsed.Text = Clock(heard);
        _fill.ScaleX = (float)Math.Clamp(heard.TotalSeconds / LimitSeconds, 0, 1);
        _talk.Visibility = ViewStates.Visible;
        _talk.Alpha = _state is DictationState.Preparing ? 0.38f : 1f;
        _micLabel.Text = label;
        _mic.ContentDescription = label;
        _mic.Enabled = isActive;
        _primary.Visibility = ViewStates.Gone;
        SetTopMargin(_actions, 18);
    }

    private void ShowMessage(string lead, string action)
    {
        _title.Text = "Inserimento vocale";
        _lead.Text = lead;
        _timeRow.Visibility = ViewStates.Gone;
        _talk.Visibility = ViewStates.Gone;
        _primary.Text = action;
        _primary.ContentDescription = action;
        _primary.Visibility = ViewStates.Visible;
        SetTopMargin(_actions, 28);
    }

    private void Paint()
    {
        _scrim.SetBackgroundColor(Resolve(ThemeColor.Scrim));
        _sheet.Background = Shape(ThemeColor.Ground, topCorners: SheetCorner);
        _title.SetTextColor(Resolve(ThemeColor.Ink));
        _lead.SetTextColor(Resolve(ThemeColor.Mute));
        _elapsed.SetTextColor(Resolve(ThemeColor.Ink));
        _remaining.SetTextColor(Resolve(ThemeColor.Mute));
        _track.SetBackgroundColor(Resolve(ThemeColor.Hairline));
        _fill.SetBackgroundColor(Resolve(_state is DictationState.Preparing ? ThemeColor.Mute : ThemeColor.Ink));
        _micLabel.SetTextColor(Resolve(ThemeColor.Ink));
        _mic.SetTextColor(Resolve(ThemeColor.Accent));
        var mic = Shape(ThemeColor.SurfaceRaised, oval: true);
        mic.SetStroke(Dp(1), Resolve(ThemeColor.Outline));
        _mic.Background = mic;
        _mic.Foreground = Ripple(Shape(ThemeColor.Ink, oval: true));
        _cancel.SetTextColor(Resolve(ThemeColor.Ink));
        _cancel.Foreground = Ripple(new ColorDrawable(Color.White));
        _primary.SetTextColor(Resolve(ThemeColor.Ground));
        _primary.Background = Shape(ThemeColor.Ink, radius: Dp(Target) / 2f);
        _primary.Foreground = Ripple(Shape(ThemeColor.Ink, radius: Dp(Target) / 2f));
    }

    private TextView Text(double size, string font, ComplexUnitType unit = ComplexUnitType.Sp)
    {
        var text = new TextView(_context)
        {
            Typeface = _fonts.GetTypeface(Microsoft.Maui.Font.OfSize(font, size)),
            LetterSpacing = 0,
            FontFeatureSettings = Tabular
        };
        text.SetIncludeFontPadding(false);
        text.SetTextSize(unit, (float)size);
        return text;
    }

    private TextView Button(double size, string font, int horizontalPadding)
    {
        var button = Text(size, font);
        button.Gravity = GravityFlags.Center;
        button.Clickable = true;
        button.SetPadding(Dp(horizontalPadding), 0, Dp(horizontalPadding), 0);
        return button;
    }

    private GradientDrawable Shape(ThemeColor fill, float radius = 0, int topCorners = 0, bool oval = false)
    {
        var shape = new GradientDrawable();
        shape.SetColor(Resolve(fill));
        if (oval)
            shape.SetShape(ShapeType.Oval);
        else if (topCorners > 0)
            shape.SetCornerRadii([Dp(topCorners), Dp(topCorners), Dp(topCorners), Dp(topCorners), 0, 0, 0, 0]);
        else
            shape.SetCornerRadius(radius);

        return shape;
    }

    private RippleDrawable Ripple(Drawable mask) =>
        new(ColorStateList.ValueOf(Faint(Resolve(ThemeColor.Ink))), null, mask);

    private static Color Faint(Color color) => Color.Argb(0x1F, color.R, color.G, color.B);

    private Color Resolve(ThemeColor color) => ThemeColors.Resolve(color, _isDark).ToPlatform();

    private int Dp(double value) =>
        (int)Math.Round(TypedValue.ApplyDimension(ComplexUnitType.Dip, (float)value, _context.Resources!.DisplayMetrics));

    private void SetTopMargin(View view, int dp)
    {
        var margins = (LinearLayout.LayoutParams)view.LayoutParameters!;
        margins.TopMargin = Dp(dp);
        view.LayoutParameters = margins;
    }

    private static string Clock(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";

    private sealed class SheetInsets(View sheet, int restingBottom) : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(View? view, WindowInsetsCompat? insets)
        {
            var bars = insets!.GetInsets(WindowInsetsCompat.Type.SystemBars());
            sheet.SetPadding(sheet.PaddingLeft, sheet.PaddingTop, sheet.PaddingRight, restingBottom + bars!.Bottom);
            return insets!;
        }
    }
}
