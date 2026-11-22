using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class CategoriesPage : ContentPage
{
    private readonly CategoriesViewModel _viewModel;

    public CategoriesPage(CategoriesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnNewCategoryClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(CategoryEditorPage.Route());

    // The editor opens a category in use to change it and an archived one only to read and restore it.
    private async void OnCategoryClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CategoryRow category })
            await Shell.Current.GoToAsync(CategoryEditorPage.Route(category.CategoryId));
    }
}
