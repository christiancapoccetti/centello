using Android.Animation;
using Android.Content;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.Core.View;
using Centello.App.Theming;
using Centello.Application;
using Microsoft.Maui.Platform;
using Color = Android.Graphics.Color;
using Orientation = Android.Widget.Orientation;
using View = Android.Views.View;

namespace Centello.App;

internal sealed class PanelSheet
{
    private const int SheetCorner = 28;
    private const int Gutter = 24;
    private const int Target = 48;
    private const int MicSize = 88;
    private const int ReceiptWidth = 56;
    private const int ReceiptHeight = 72;
    private const float SweepShare = 0.42f;
    private const long SweepMilliseconds = 1400;
    private const string Tabular = "tnum";

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
    private readonly LinearLayout _readingRow;
    private readonly TextView _receipt;
    private readonly FrameLayout _readingTrack;
    private readonly View _readingBar;
    private readonly TextView _privacy;
    private readonly ValueAnimator _sweep;
    private readonly LinearLayout _actions;
    private readonly TextView _cancel;
    private readonly TextView _primary;
    private bool _isDark;
    private PanelFace _face = new PanelFace.Hidden();

    public PanelSheet(Context context, IFontManager fonts, bool isDark)
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

        _receipt = Text(24, Fonts.Icons, ComplexUnitType.Dip);
        _receipt.Gravity = GravityFlags.Center;
        _readingBar = new View(context) { PivotX = 0, ScaleX = SweepShare };
        _readingTrack = new FrameLayout(context);
        _readingTrack.AddView(_readingBar, new FrameLayout.LayoutParams(Match, Match));
        _readingRow = new LinearLayout(context) { Orientation = Orientation.Horizontal };
        _readingRow.SetGravity(GravityFlags.CenterVertical);
        _readingRow.AddView(_receipt, new LinearLayout.LayoutParams(Dp(ReceiptWidth), Dp(ReceiptHeight)));
        _readingRow.AddView(_readingTrack, new LinearLayout.LayoutParams(0, Dp(4), 1) { LeftMargin = Dp(14) });
        _privacy = Text(12, Fonts.Regular);
        _sweep = ValueAnimator.OfFloat(0f, 1f)!;
        _sweep.SetDuration(SweepMilliseconds);
        _sweep.RepeatCount = ValueAnimator.Infinite;
        _sweep.SetInterpolator(new LinearInterpolator());
        _sweep.Update += (_, args) => Sweep((float)args.Animation.AnimatedValue!);

        _cancel = Button(14, Fonts.SemiBold, horizontalPadding: 12);
        _cancel.Text = "Annulla";
        _cancel.ContentDescription = "Annulla";
        _cancel.Click += (_, _) => Dismissed?.Invoke();
        _primary = Button(16, Fonts.Bold, horizontalPadding: Gutter);
        _primary.Click += (_, _) =>
        {
            if (_face is PanelFace.Message message)
                ActionTapped?.Invoke(message.Action);
        };
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
        _sheet.AddView(_readingRow, new LinearLayout.LayoutParams(Match, Wrap) { TopMargin = Dp(20) });
        _sheet.AddView(_privacy, new LinearLayout.LayoutParams(Match, Wrap) { TopMargin = Dp(16) });
        _sheet.AddView(_actions, new LinearLayout.LayoutParams(Match, Wrap) { TopMargin = Dp(18) });

        _root = new FrameLayout(context);
        _root.AddView(_scrim, new FrameLayout.LayoutParams(Match, Match));
        _root.AddView(_sheet, new FrameLayout.LayoutParams(Match, Wrap, GravityFlags.Bottom));
        ViewCompat.SetOnApplyWindowInsetsListener(_root, new SheetInsets(_sheet, Dp(4)));
        _root.ViewDetachedFromWindow += (_, _) => _sweep.Cancel();

        Render();
    }

    public View Root => _root;

    public event Action? MicTapped;
    public event Action<PanelAction>? ActionTapped;
    public event Action? Dismissed;

    public void Show(PanelFace face)
    {
        _face = face;
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
        var visibility = _face is PanelFace.Hidden ? ViewStates.Gone : ViewStates.Visible;
        _scrim.Visibility = visibility;
        _sheet.Visibility = visibility;
        var isReading = _face is PanelFace.Reading;
        if (isReading)
            StartSweeping();
        else
            _sweep.Cancel();

        switch (_face)
        {
            case PanelFace.Recording recording:
                ShowRecording(recording);
                break;
            case PanelFace.Reading reading:
                ShowReading(reading);
                break;
            case PanelFace.Message message:
                ShowMessage(message);
                break;
        }

        Paint();
    }

    private void ShowRecording(PanelFace.Recording recording)
    {
        _title.Text = recording.Title;
        _lead.Text = recording.Lead;
        _timeRow.Visibility = ViewStates.Visible;
        _elapsed.Text = recording.Elapsed;
        _remaining.Text = recording.Remaining;
        _fill.ScaleX = (float)recording.Fill;
        _readingRow.Visibility = ViewStates.Gone;
        _privacy.Visibility = ViewStates.Gone;
        _talk.Visibility = ViewStates.Visible;
        _talk.Alpha = recording.Mic == MicStage.Sending ? 0.38f : 1f;
        _micLabel.Text = recording.MicLabel;
        _mic.ContentDescription = recording.MicLabel;
        _mic.Enabled = recording.Mic == MicStage.Listening;
        _primary.Visibility = ViewStates.Gone;
        SetTopMargin(_actions, 18);
    }

    private void ShowReading(PanelFace.Reading reading)
    {
        _title.Text = reading.Title;
        _lead.Text = reading.Lead;
        _timeRow.Visibility = ViewStates.Gone;
        _talk.Visibility = ViewStates.Gone;
        _receipt.Text = reading.Glyph;
        _readingRow.Visibility = ViewStates.Visible;
        _privacy.Text = reading.PrivacyNote;
        _privacy.Visibility = ViewStates.Visible;
        _primary.Visibility = ViewStates.Gone;
        SetTopMargin(_actions, 14);
    }

    private void ShowMessage(PanelFace.Message message)
    {
        var action = message.Action == PanelAction.OpenSettings ? "Apri impostazioni" : "Riprova";
        _title.Text = message.Title;
        _lead.Text = message.Lead;
        _timeRow.Visibility = ViewStates.Gone;
        _talk.Visibility = ViewStates.Gone;
        _readingRow.Visibility = ViewStates.Gone;
        _privacy.Visibility = ViewStates.Gone;
        _primary.Text = action;
        _primary.ContentDescription = action;
        _primary.Visibility = ViewStates.Visible;
        SetTopMargin(_actions, 28);
    }

    private void StartSweeping()
    {
        if (!_sweep.IsRunning)
            _sweep.Start();
    }

    private void Sweep(float progress)
    {
        var width = _readingTrack.Width;
        var bar = width * SweepShare;
        _readingBar.TranslationX = -bar + progress * (width + bar);
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
        _fill.SetBackgroundColor(Resolve(_face is PanelFace.Recording { Mic: MicStage.Sending } ? ThemeColor.Mute : ThemeColor.Ink));
        _micLabel.SetTextColor(Resolve(ThemeColor.Ink));
        _mic.SetTextColor(Resolve(ThemeColor.Accent));
        var mic = Shape(ThemeColor.SurfaceRaised, oval: true);
        mic.SetStroke(Dp(1), Resolve(ThemeColor.Outline));
        _mic.Background = mic;
        _mic.Foreground = Ripple(Shape(ThemeColor.Ink, oval: true));
        _receipt.SetTextColor(Resolve(ThemeColor.Mute));
        var receipt = Shape(ThemeColor.SurfaceRaised);
        receipt.SetStroke(Dp(1), Resolve(ThemeColor.Outline));
        _receipt.Background = receipt;
        _readingTrack.SetBackgroundColor(Resolve(ThemeColor.Hairline));
        _readingBar.SetBackgroundColor(Resolve(ThemeColor.Ink));
        _privacy.SetTextColor(Resolve(ThemeColor.Mute));
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
