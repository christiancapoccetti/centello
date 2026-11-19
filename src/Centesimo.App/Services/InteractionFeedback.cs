using System.Runtime.CompilerServices;
using Centesimo.App.Theming;

namespace Centesimo.App;

public static class InteractionFeedback
{
    private static readonly ConditionalWeakTable<Border, PressedState> PressedCards = new();

    public static void Press(object? sender)
    {
        var card = FindCard(sender);
        if (card is null || PressedCards.TryGetValue(card, out _))
            return;

        PressedCards.Add(card, new PressedState(card.Opacity, card.Scale, card.Background));
        card.Opacity = 0.76;
        card.Scale = 0.965;
        card.Background = new SolidColorBrush(ThemeColors.Current(ThemeColor.SurfaceRaised));
    }

    public static void Release(object? sender)
    {
        var card = FindCard(sender);
        if (card is null || !PressedCards.TryGetValue(card, out var state))
            return;

        card.Opacity = state.Opacity;
        card.Scale = state.Scale;
        card.Background = state.Background;
        PressedCards.Remove(card);
    }

    private static Border? FindCard(object? sender)
    {
        if (sender is not Element element)
            return null;

        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
            if (parent is Border border)
                return border;

        return null;
    }

    private sealed record PressedState(double Opacity, double Scale, Brush? Background);
}