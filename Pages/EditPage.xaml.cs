using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using pract7.Models;

namespace pract7.Pages;

public partial class EditPage : Page
{
    MainPage mainPage;
    Medicine original;
    MedicineForm form;
    bool busy;

    public EditPage(MainPage mainPage, Medicine medicine)
    {
        InitializeComponent();
        this.mainPage = mainPage;
        original = medicine;
        form = new MedicineForm(medicine);
        DataContext = form;
    }

    async void Put_ClickAsync(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        var medicine = form.ReadMedicine();
        if (medicine == null) return;
        medicine.Id = original.Id;
        busy = true;
        try
        {
            using var response = await mainPage.Client.PutAsJsonAsync(
                mainPage.GetAddress("/" + original.Id), medicine);
            response.EnsureSuccessStatusCode();
            MessageBox.Show("Лекарство изменено.");
            await mainPage.LoadMedicinesAsync();
            mainPage.ShowPage(mainPage);
        }
        catch (Exception error)
        {
            MessageBox.Show("Не удалось изменить лекарство. Перед повтором проверь список.\n" + error.Message);
        }
        finally
        {
            busy = false;
        }
    }

    async void Patch_ClickAsync(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        var medicine = form.ReadMedicine();
        if (medicine == null) return;
        var changes = new MedicineData();
        if (medicine.Name != original.Name) changes.Name = medicine.Name;
        if (medicine.Manufacturer != original.Manufacturer) changes.Manufacturer = medicine.Manufacturer;
        if (medicine.Price != original.Price) changes.Price = medicine.Price;
        if (medicine.ExpirationDate != original.ExpirationDate) changes.ExpirationDate = medicine.ExpirationDate;
        if (medicine.IsAvailable != original.IsAvailable) changes.IsAvailable = medicine.IsAvailable;

        if (changes.Name == null && changes.Manufacturer == null && changes.Price == null &&
            changes.ExpirationDate == null && changes.IsAvailable == null)
        {
            MessageBox.Show("Сначала измени хотя бы одно поле.");
            return;
        }
        busy = true;
        try
        {
            using var response = await mainPage.Client.PatchAsJsonAsync(
                mainPage.GetAddress("/" + original.Id), changes);
            response.EnsureSuccessStatusCode();
            MessageBox.Show("Изменённые поля сохранены.");
            await mainPage.LoadMedicinesAsync();
            mainPage.ShowPage(mainPage);
        }
        catch (Exception error)
        {
            MessageBox.Show("Не удалось изменить лекарство. Перед повтором проверь список.\n" + error.Message);
        }
        finally
        {
            busy = false;
        }
    }

    async void Delete_ClickAsync(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        if (MessageBox.Show("Удалить лекарство?", "Удаление", MessageBoxButton.YesNo)
            != MessageBoxResult.Yes) return;
        busy = true;
        try
        {
            using var response = await mainPage.Client.DeleteAsync(mainPage.GetAddress("/" + original.Id));
            response.EnsureSuccessStatusCode();
            MessageBox.Show("Лекарство удалено.");
            await mainPage.LoadMedicinesAsync();
            mainPage.ShowPage(mainPage);
        }
        catch (Exception error)
        {
            MessageBox.Show("Не удалось удалить лекарство. Проверь список.\n" + error.Message);
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
