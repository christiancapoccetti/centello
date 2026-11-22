using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class ExpenseExportService_should_expected_behavior : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "centello-export-" + Guid.NewGuid());
    private readonly FakeExpenseRepository _expenses = new();
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeTagRepository _tags = new();
    private DateOnly _today = new(2026, 11, 20);

    public ExpenseExportService_should_expected_behavior()
    {
        var category = new Category(Guid.NewGuid(), "Cibo", "cart", "#176B5B");
        _categories.Items.Add(category);
        _expenses.Items.Add(new Expense(Guid.NewGuid(), category.CategoryId, new Money(128460), new DateOnly(2026, 11, 5)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    private ExpenseExportService Service() => new(_expenses, _categories, _tags, () => _today);

    [Fact]
    public async Task Write_the_csv_named_after_today_and_return_its_path()
    {
        var result = await Service().Export(_directory);

        Assert.True(result.IsSuccess);
        Assert.Equal(Path.Combine(_directory, "centello-spese-2026-11-20.csv"), result.Value.FilePath);
        Assert.Equal("text/csv", result.Value.ContentType);
        Assert.Equal(
            ExpenseCsv.Write(_expenses.Items, _categories.Items, _tags.Items),
            await File.ReadAllBytesAsync(result.Value.FilePath));
    }

    [Fact]
    public async Task Replace_the_file_of_the_same_day()
    {
        await Service().Export(_directory);
        _expenses.Items.Clear();

        var result = await Service().Export(_directory);

        Assert.Equal(
            ExpenseCsv.Write([], _categories.Items, _tags.Items),
            await File.ReadAllBytesAsync(result.Value.FilePath));
        Assert.Single(Directory.GetFiles(_directory));
    }

    [Fact]
    public async Task Keep_the_file_of_another_day()
    {
        await Service().Export(_directory);
        _today = new DateOnly(2026, 11, 21);

        await Service().Export(_directory);

        Assert.Equal(2, Directory.GetFiles(_directory).Length);
    }

    [Fact]
    public async Task Fail_and_write_nothing_when_a_read_fails()
    {
        var failure = new Error("Test.Failure", "boom");
        var failing = new Action[]
        {
            () => _expenses.Failure = failure,
            () => _categories.Failure = failure,
            () => _tags.Failure = failure
        };

        foreach (var fail in failing)
        {
            _expenses.Failure = null;
            _categories.Failure = null;
            _tags.Failure = null;
            fail();

            var result = await Service().Export(_directory);

            Assert.Equal(PortabilityErrors.ExportFailed, result.Error);
        }

        Assert.False(Directory.Exists(_directory));
    }

    [Fact]
    public async Task Fail_when_the_directory_cannot_be_created()
    {
        Directory.CreateDirectory(_directory);
        var blocker = Path.Combine(_directory, "blocker");
        await File.WriteAllTextAsync(blocker, "");

        var result = await Service().Export(Path.Combine(blocker, "exports"));

        Assert.Equal(PortabilityErrors.ExportFailed, result.Error);
    }
}
