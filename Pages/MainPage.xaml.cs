using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using pract7.Models;

namespace pract7.Pages;

public partial class MainPage : Page
{
    MainWindow window;
    bool busy;

    public HttpClient Client { get; } = new() { Timeout = TimeSpan.FromSeconds(10) };
    public ObservableCollection<Medicine> Medicines { get; } = new();
    public Medicine? SelectedMedicine { get; set; }
    public string ServerUrl { get; set; } = "https://localhost:7001";
    public string ManufacturerFilter { get; set; } = "";
    public int FilterIndex { get; set; }

    public MainPage(MainWindow window)
    {
        InitializeComponent();
        this.window = window;
        DataContext = this;
    }

    public Uri GetAddress(string path = "")
    {
        string address = ServerUrl.Trim().TrimEnd('/') + "/api/v1/Medicines" + path;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new ArgumentException("Введи адрес API с http:// или https://.");
        return uri;
    }

    public void ShowPage(Page page)
    {
        window.MainFrame.Navigate(page);
    }

    public async Task LoadMedicines()
    {
        if (busy) return;
        busy = true;
        try
        {
            string path = "?manufacturer=" + Uri.EscapeDataString(ManufacturerFilter.Trim());
            if (FilterIndex == 1) path += "&available=true";
            if (FilterIndex == 2) path += "&available=false";

            var list = await Client.GetFromJsonAsync<List<Medicine>>(GetAddress(path));
            if (list == null) throw new Exception("Сервер не вернул список.");
            Medicines.Clear();
            foreach (var medicine in list)
                Medicines.Add(medicine);
        }
        catch (Exception error)
        {
            MessageBox.Show("Не удалось загрузить список. Проверь API.\n" + error.Message);
        }
        finally
        {
            busy = false;
        }
    }

    async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadMedicines();
    }

    void New_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        ShowPage(new AddPage(this));
    }

    async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        if (SelectedMedicine == null)
        {
            MessageBox.Show("Выбери лекарство в таблице.");
            return;
        }
        busy = true;
        try
        {
            var medicine = await Client.GetFromJsonAsync<Medicine>(GetAddress("/" + SelectedMedicine.Id));
            if (medicine == null) throw new Exception("Лекарство не найдено.");
            ShowPage(new EditPage(this, medicine));
        }
        catch (Exception error)
        {
            MessageBox.Show("Не удалось открыть лекарство.\n" + error.Message);
        }
        finally
        {
            busy = false;
        }
    }
}
