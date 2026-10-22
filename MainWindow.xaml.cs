using System.Windows;
using pract7.Pages;

namespace pract7;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainFrame.Navigate(new MainPage(this));
    }
}
