# Category budgets and spending comparisons (#33)

## Feature phases

Frame complete (L). Design approved for Today and Insight. Architecture ready for operator checkpoint. Implementation, independent review, Android verification, documentation and hand-off follow approval.

## Design

Today follows mockups/2026-11-24-home-budget-a-grafici.html. Keep the category strip, daily chart, expense history entry and add dock. Put budgeted categories first, including at zero spend. Show amount spent, remaining or overshoot, and progress. Show spending only for unbudgeted categories with expenses.

Insight follows mockups/2026-11-24-insight-scroll-c-senza-badge.html. Use a small total and date range, category headline, three shares with comparable-period markers, full list disclosure, and one lower chart at a time. Start on Andamento; Per categoria shows signed euro differences. Retain period navigation, closed-period recap, accessibility, dark/light themes and landscape support. Omit observation badges. Generate copy from actual values with deterministic fallbacks for ties, empty or incomparable periods.

The proposed category widget uses the current month and the same rows as Today. Show as many as fit the widget size, prioritizing budgeted categories; open Today for the full list. Use the existing refresh and launcher pipeline.

## Architecture decision

Extend the two existing reads with pure projections. HomeOverviewService.Get and Glance already share a Reading. Expose an immutable CategoryBudgetRow(CategoryId, Name, IconKey, ColorKey, SpentCents, BudgetCents?) list on HomeOverview and MonthGlance. Use signed remaining cents and cap only the visual bar. Use PeriodBudget.Of for annual limits. Drop the tag read if no remaining caller there uses tags. Preserve the independent spending order of the composition strip.

InsightsService derives comparison rows from SpendComparison. Each category has current/prior cents and shares with period-specific denominators. Show leading current spenders, retain previous-only categories in the full comparison, and reconcile archived/missing residuals to the actual total. An unavailable denominator suppresses its marker. Sum signed euro deltas without rounding raw cents. Preserve the existing cumulative chart and scope comparison selection and disclosure state to the selected period.

An alternative shared report/query would need both full and elapsed period windows. It risks counting future-dated expenses differently or querying twice. Reuse pure calculations wherever the inputs match. Prefer the existing reads on period correctness, I/O cost and scope of change. The two same-family candidates were assessed in the main session.

## Implementation steps

1. Application contracts and regression tests. Update HomeOverviewService, InsightsService, related value types and existing test suites. Cover zero and exceeded budgets, annual views, incomplete comparison windows, missing previous spend, archived categories, and previous-only categories. Verify aggregate identities for all the chart rows.
2. After contracts build, split into up to three isolated worktrees: Today page/view model and history navigation; Insight page/view model and comparison controls; Android category widget provider, layout, registration and preview. Keep Android launcher updates in the widget branch. Check build and full tests after each merge.
3. Independent review against the approved layouts and production flows. Update documentation to reflect Today, Insight and the new launcher widget.

## Verification and delivery

Run the solution tests and Android checks. Exercise seeded and empty data in a dedicated Dev emulator: month/year, no prior history, category detail, all expenses, expandable list, chart switching, widget refresh after mutation, light/dark and portrait/landscape. Compare native screenshots to the approved mocks. Publish an installable Release APK with DevBuild=true and verify the signed package identity it.christiancapoccetti.centesimo.dev. Reserve physical-device installation for the user's connected phone, with an in-place update that preserves existing Dev data.
