namespace Centello.Application;

/// <summary>A sentence with one bold run: "Sabato 14 novembre" in "Sabato 14 novembre, in 3 spese".</summary>
public sealed record RecapPhrase(string Lead, string Emphasis = "", string Tail = "");

/// <summary>Lower is the green one: spending less is the good news.</summary>
public sealed record RecapChip(string Text, ComparisonTrend Trend);

public sealed record RecapCategory(Guid CategoryId, string Name, string IconKey, string ColorKey);

public sealed record RecapExpense(Guid ExpenseId, RecapCategory Category, string Note, string Amount);

public sealed record RecapMeasure(string Label, double Width, string Amount, bool IsCurrent);

public sealed record RecapShare(RecapCategory Category, double Fraction, string Amount, string Share);

public sealed record RecapChange(RecapCategory Category, string Amount, string Detail)
{
    public RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);
}

public sealed record RecapTagLine(RecapCategory Category, string Tag, string Detail, string Amount);

public sealed record RecapLine(string Name, string Amount);

public sealed record RecapSplit(double Automatic, string AutomaticName, string VariableName);

/// <summary>Glyph is a <see cref="MaterialSymbols"/> constant.</summary>
public sealed record RecapSummaryRow(string Glyph, string Label, string Value);

public enum RecapExitTarget { MonthExpenses, Insight }

public sealed record RecapExit(string Text, RecapExitTarget Target);
