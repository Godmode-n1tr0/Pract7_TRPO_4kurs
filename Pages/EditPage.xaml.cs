using System.Windows;
using System.Windows.Controls;

namespace pract7.Pages;

public partial class EditPage : Page
{
    private readonly MainPage mainPage;

    public EditPage(MainPage mainPage)
    {
        InitializeComponent();
        this.mainPage = mainPage;
        DataContext = mainPage;
    }

    private void Put_Click(object sender, RoutedEventArgs e)
    {
        mainPage.Put_Click(sender, e);
    }

    private void Patch_Click(object sender, RoutedEventArgs e)
    {
        mainPage.Patch_Click(sender, e);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        mainPage.Delete_Click(sender, e);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        mainPage.Catalog_Click(sender, e);
    }
}
