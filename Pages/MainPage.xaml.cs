using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using pract7.Models;

namespace pract7.Pages;

public partial class MainPage : Page, INotifyPropertyChanged
{
    private readonly MainWindow window;
    private static readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly JsonSerializerOptions options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    public string ServerUrl { get; set; } = "https://localhost:7001";
    public List<Medicine> Medicines { get; set; } = new();
    public Medicine? SelectedMedicine { get; set; }
    public string ManufacturerFilter { get; set; } = "";
    public int FilterIndex { get; set; }
    public string[] AvailabilityOptions { get; } = { "Все", "Есть", "Нет" };
    public bool CanWork { get; set; } = true;

    public int CurrentId { get; set; }
    public string NameText { get; set; } = "";
    public string ManufacturerText { get; set; } = "";
    public string PriceText { get; set; } = "";
    public string DateText { get; set; } = "";
    public bool Available { get; set; }
    public bool UseName { get; set; }
    public bool UseManufacturer { get; set; }
    public bool UsePrice { get; set; }
    public bool UseDate { get; set; }
    public bool UseAvailable { get; set; }

    public MainPage(MainWindow window)
    {
        InitializeComponent();
        this.window = window;
        DataContext = this;
    }

    private void UpdatePage()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    private async Task<string?> Send(HttpMethod method, string path, object? data = null)
    {
        string address = ServerUrl.Trim().TrimEnd('/') + "/api/v1/Medicines" + path;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            MessageBox.Show("Проверь адрес API. Он должен начинаться с http:// или https://.");
            return null;
        }

        CanWork = false;
        UpdatePage();
        try
        {
            using var request = new HttpRequestMessage(method, uri);
            if (data != null)
                request.Content = JsonContent.Create(data, options: options);

            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Ошибка API: " + (int)response.StatusCode +
                    ". Проверь данные и наличие записи на сервере.");
                return null;
            }
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            MessageBox.Show("Нет подключения к API. Проверь адрес и запусти сервер.");
            return null;
        }
        catch (TaskCanceledException)
        {
            MessageBox.Show("Сервер не ответил. Обнови список перед повторным изменением.");
            return null;
        }
        finally
        {
            CanWork = true;
            UpdatePage();
        }
    }

    private async Task LoadList()
    {
        string path = "?manufacturer=" + Uri.EscapeDataString(ManufacturerFilter.Trim());
        if (FilterIndex == 1) path += "&available=true";
        if (FilterIndex == 2) path += "&available=false";

        string? json = await Send(HttpMethod.Get, path);
        if (json == null) return;
        try
        {
            Medicines = JsonSerializer.Deserialize<List<Medicine>>(json, options)
                ?? throw new JsonException();
            SelectedMedicine = null;
            UpdatePage();
        }
        catch (JsonException)
        {
            MessageBox.Show("Сервер вернул неправильный формат списка.");
        }
    }

    private async Task<bool> LoadRecord(int id)
    {
        string? json = await Send(HttpMethod.Get, "/" + id);
        if (json == null) return false;
        try
        {
            var medicine = JsonSerializer.Deserialize<Medicine>(json, options)
                ?? throw new JsonException();
            ClearForm();
            CurrentId = medicine.Id;
            NameText = medicine.Name;
            ManufacturerText = medicine.Manufacturer;
            PriceText = medicine.Price.ToString();
            DateText = medicine.ExpirationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Available = medicine.IsAvailable;
            UpdatePage();
            return true;
        }
        catch (JsonException)
        {
            MessageBox.Show("Сервер вернул неправильный формат лекарства.");
            return false;
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadList();
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedMedicine == null)
        {
            MessageBox.Show("Выбери лекарство в таблице.");
            return;
        }
        if (await LoadRecord(SelectedMedicine.Id))
            window.MainFrame.Navigate(new EditPage(this));
    }

    private void ClearForm()
    {
        CurrentId = 0;
        NameText = "";
        ManufacturerText = "";
        PriceText = "";
        DateText = "";
        Available = false;
        UseName = UseManufacturer = UsePrice = UseDate = UseAvailable = false;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        UpdatePage();
        window.MainFrame.Navigate(new AddPage(this));
    }

    public void Catalog_Click(object sender, RoutedEventArgs e)
    {
        window.MainFrame.Navigate(this);
    }

    private MedicineData? ReadForm(bool partial)
    {
        var data = new MedicineData();
        if (partial && !UseName && !UseManufacturer && !UsePrice && !UseDate && !UseAvailable)
        {
            MessageBox.Show("Отметь поля для частичного изменения.");
            return null;
        }
        if (!partial || UseName)
        {
            if (string.IsNullOrWhiteSpace(NameText) || NameText.Trim().Length > 150)
            {
                MessageBox.Show("Введи название: от 1 до 150 символов.");
                return null;
            }
            data.Name = NameText.Trim();
        }
        if (!partial || UseManufacturer)
        {
            if (string.IsNullOrWhiteSpace(ManufacturerText) || ManufacturerText.Trim().Length > 120)
            {
                MessageBox.Show("Введи производителя: от 1 до 120 символов.");
                return null;
            }
            data.Manufacturer = ManufacturerText.Trim();
        }
        if (!partial || UsePrice)
        {
            if (!decimal.TryParse(PriceText.Trim().Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out decimal price) ||
                price < 0 || price > 99999999.99m || decimal.Round(price, 2) != price)
            {
                MessageBox.Show("Введи цену от 0 до 99999999,99, не больше двух знаков после запятой.");
                return null;
            }
            data.Price = price;
        }
        if (!partial || UseDate)
        {
            if (!DateOnly.TryParseExact(DateText.Trim(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                MessageBox.Show("Введи дату в формате ГГГГ-ММ-ДД, например 2027-12-31.");
                return null;
            }
            data.ExpirationDate = date;
        }
        if (!partial || UseAvailable) data.IsAvailable = Available;
        return data;
    }

    public async void Create_Click(object sender, RoutedEventArgs e)
    {
        var medicine = ReadForm(false);
        if (medicine == null) return;
        if (await Send(HttpMethod.Post, "", medicine) == null) return;
        MessageBox.Show("Лекарство добавлено.");
        ClearForm();
        UpdatePage();
        await LoadList();
        window.MainFrame.Navigate(this);
    }

    public async void Put_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentId == 0) return;
        var medicine = ReadForm(false);
        if (medicine == null) return;
        medicine.Id = CurrentId;
        if (await Send(HttpMethod.Put, "/" + CurrentId, medicine) == null) return;
        MessageBox.Show("Лекарство изменено.");
        await LoadRecord(CurrentId);
        await LoadList();
    }

    public async void Patch_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentId == 0) return;
        var medicine = ReadForm(true);
        if (medicine == null) return;
        if (await Send(HttpMethod.Patch, "/" + CurrentId, medicine) == null) return;
        MessageBox.Show("Выбранные поля изменены.");
        await LoadRecord(CurrentId);
        await LoadList();
    }

    public async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentId == 0) return;
        if (MessageBox.Show("Удалить лекарство?", "Удаление", MessageBoxButton.YesNo)
            != MessageBoxResult.Yes) return;
        if (await Send(HttpMethod.Delete, "/" + CurrentId) == null) return;
        MessageBox.Show("Лекарство удалено.");
        ClearForm();
        UpdatePage();
        await LoadList();
        window.MainFrame.Navigate(this);
    }
}
