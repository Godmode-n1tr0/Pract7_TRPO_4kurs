using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using pract7.Models;

namespace pract7.Pages;

public partial class AddPage : Page
{
    MainPage mainPage;
    MedicineForm form = new();
    bool busy;

    public AddPage(MainPage mainPage)
    {
        InitializeComponent();
        this.mainPage = mainPage;
        DataContext = form;
    }

    async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        var medicine = form.ReadMedicine();
        if (medicine == null) return;
        busy = true;
        try
        {
            using var response = await mainPage.Client.PostAsJsonAsync(mainPage.GetAddress(), medicine);
            response.EnsureSuccessStatusCode();
            MessageBox.Show("Лекарство добавлено.");
            await mainPage.LoadMedicines();
            mainPage.ShowPage(mainPage);
        }
        catch (Exception error)
        {
            MessageBox.Show("Не удалось добавить лекарство. Перед повтором проверь список.\n" + error.Message);
        }
        finally
        {
            busy = false;
        }
    }

    void Back_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        mainPage.ShowPage(mainPage);
    }
}
