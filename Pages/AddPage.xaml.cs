using System.Windows;
using System.Windows.Controls;

namespace pract7.Pages;

public partial class AddPage : Page
{
    private readonly MainPage mainPage;

    public AddPage(MainPage mainPage)
    {
        InitializeComponent();
        this.mainPage = mainPage;
        DataContext = mainPage;
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        mainPage.Create_Click(sender, e);
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        mainPage.Catalog_Click(sender, e);
    }
}
