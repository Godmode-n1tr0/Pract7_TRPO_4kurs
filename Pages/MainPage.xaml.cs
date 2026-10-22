using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using pract7.Models;

namespace pract7.Pages;

public partial class MainPage : Page, INotifyPropertyChanged
{
    private readonly MainWindow window;
    private bool firstLoad = true;
    private bool busy;
    private string savedMessage = "";

    private static readonly HttpClient client = new()
    {
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public event PropertyChangedEventHandler? PropertyChanged;
    public string ServerUrl { get; set; } = "https://localhost:7001";
    public List<Medicine> Medicines { get; set; } = new();
    public Medicine? SelectedMedicine { get; set; }
    public string ManufacturerFilter { get; set; } = "";
    public int FilterIndex { get; set; }
    public string[] AvailabilityOptions { get; } =
    {
        "Все", "Есть в наличии", "Нет в наличии"
    };

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
    public bool CanWork => !busy;
    public string Status { get; set; } = "Запусти API и обнови список.";

    public MainPage(MainWindow window)
    {
        InitializeComponent();
        this.window = window;
        DataContext = this;
    }

    private void UpdateBindings()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!firstLoad)
            return;

        firstLoad = false;
        await Run(LoadList, "Список загружен.");
    }

    private async Task<string> Send(HttpMethod method, string path, object? data = null)
    {
        string address = ServerUrl.Trim().TrimEnd('/') + "/" + path;
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new InvalidOperationException("Укажи адрес сервера с http:// или https://.");
        }

        using var request = new HttpRequestMessage(method, uri);
        if (data != null)
            request.Content = JsonContent.Create(data);

        using var response = await client.SendAsync(request);
        string text = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            string error = (int)response.StatusCode switch
            {
                400 => "Сервер отклонил данные.",
                404 => "Запись или адрес API не найдены.",
                500 => "Ошибка сервера. Проверь API и подключение к базе.",
                _ => "Ошибка API: " + (int)response.StatusCode
            };
            if (!string.IsNullOrWhiteSpace(text))
                error += "\n" + text[..Math.Min(text.Length, 400)];

            throw new InvalidOperationException(error);
        }

        return text;
    }

    private async Task Run(Func<Task> action, string success)
    {
        if (busy)
            return;

        busy = true;
        savedMessage = "";
        Status = "Выполняется запрос...";
        UpdateBindings();
        try
        {
            await action();
            Status = success;
        }
        catch (HttpRequestException)
        {
            Status = savedMessage + " Не удалось связаться с сервером. Проверь запуск API и адрес.";
        }
        catch (TaskCanceledException)
        {
            Status = savedMessage +
                " Сервер не ответил вовремя. Обнови список перед повторением изменения.";
        }
        catch (JsonException)
        {
            Status = savedMessage + " Сервер вернул данные в неожиданном формате.";
        }
        catch (InvalidOperationException error)
        {
            Status = savedMessage + " " + error.Message;
        }
        finally
        {
            busy = false;
            UpdateBindings();
        }
    }

    private async Task LoadList()
    {
        var filters = new List<string>();
        if (!string.IsNullOrWhiteSpace(ManufacturerFilter))
            filters.Add("manufacturer=" + Uri.EscapeDataString(ManufacturerFilter.Trim()));

        if (FilterIndex == 1)
            filters.Add("available=true");
        else if (FilterIndex == 2)
            filters.Add("available=false");

        string path = "api/v1/Medicines";
        if (filters.Count > 0)
            path += "?" + string.Join("&", filters);

        string json = await Send(HttpMethod.Get, path);
        Medicines = JsonSerializer.Deserialize<List<Medicine>>(json, jsonOptions)
            ?? throw new JsonException();
        SelectedMedicine = null;
    }

    public async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await Run(LoadList, "Список обновлён.");
    }

    public async void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        ManufacturerFilter = "";
        FilterIndex = 0;
        await Run(LoadList, "Фильтры сброшены.");
    }

    private async Task LoadRecord(int id)
    {
        string json = await Send(HttpMethod.Get, $"api/v1/Medicines/{id}");
        var medicine = JsonSerializer.Deserialize<Medicine>(json, jsonOptions)
            ?? throw new JsonException();
        CurrentId = medicine.Id;
        NameText = medicine.Name;
        ManufacturerText = medicine.Manufacturer;
        PriceText = medicine.Price.ToString(CultureInfo.CurrentCulture);
        DateText = medicine.ExpirationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        Available = medicine.IsAvailable;
        UseName = false;
        UseManufacturer = false;
        UsePrice = false;
        UseDate = false;
        UseAvailable = false;
    }

    public async void Open_Click(object sender, RoutedEventArgs e)
    {
        await Run(async () =>
        {
            if (SelectedMedicine == null)
                throw new InvalidOperationException("Сначала выбери лекарство в таблице.");

            await LoadRecord(SelectedMedicine.Id);
            window.MainFrame.Navigate(new EditPage(this));
        }, "Запись открыта.");
    }

    private void ClearForm()
    {
        CurrentId = 0;
        NameText = "";
        ManufacturerText = "";
        PriceText = "";
        DateText = "";
        Available = false;
        UseName = false;
        UseManufacturer = false;
        UsePrice = false;
        UseDate = false;
        UseAvailable = false;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        Status = "Заполни поля и нажми «Добавить».";
        UpdateBindings();
        window.MainFrame.Navigate(new AddPage(this));
    }

    public void Catalog_Click(object sender, RoutedEventArgs e)
    {
        Status = "Каталог лекарств.";
        UpdateBindings();
        window.MainFrame.Navigate(this);
    }

    private Dictionary<string, object> ReadForm(bool partial)
    {
        var data = new Dictionary<string, object>();
        if (!partial || UseName)
        {
            string name = NameText.Trim();
            if (name.Length == 0 || name.Length > 150)
                throw new InvalidOperationException("Название должно содержать от 1 до 150 символов.");
            data["name"] = name;
        }

        if (!partial || UseManufacturer)
        {
            string manufacturer = ManufacturerText.Trim();
            if (manufacturer.Length == 0 || manufacturer.Length > 120)
                throw new InvalidOperationException("Производитель должен содержать от 1 до 120 символов.");
            data["manufacturer"] = manufacturer;
        }

        if (!partial || UsePrice)
        {
            string text = PriceText.Trim().Replace(',', '.');
            if (!decimal.TryParse(text,
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture, out decimal price) ||
                price < 0 || price > 99999999.99m || decimal.Round(price, 2) != price)
            {
                throw new InvalidOperationException(
                    "Введи цену от 0 до 99999999,99, не больше двух знаков после запятой.");
            }
            data["price"] = price;
        }

        if (!partial || UseDate)
        {
            if (!DateOnly.TryParseExact(DateText.Trim(), "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            {
                throw new InvalidOperationException("Введи существующую дату в формате ГГГГ-ММ-ДД.");
            }
            data["expirationDate"] = date;
        }

        if (!partial || UseAvailable)
            data["isAvailable"] = Available;

        if (data.Count == 0)
            throw new InvalidOperationException("Отметь хотя бы одно поле для частичного изменения.");

        return data;
    }

    private async Task Save(HttpMethod method, bool partial)
    {
        if (method == HttpMethod.Post && CurrentId != 0)
            throw new InvalidOperationException("Для добавления сначала нажми «Новая запись».");
        if (method != HttpMethod.Post && CurrentId == 0)
            throw new InvalidOperationException("Сначала открой существующую запись.");

        var data = ReadForm(partial);
        string path = "api/v1/Medicines";
        int id = CurrentId;
        if (method != HttpMethod.Post)
            path += "/" + id;
        if (method == HttpMethod.Put)
            data["id"] = id;

        await Send(method, path, data);
        savedMessage = "Сервер сохранил изменения.";
        if (method == HttpMethod.Post)
            ClearForm();

        await LoadList();
        if (method != HttpMethod.Post)
            await LoadRecord(id);
    }

    public async void Create_Click(object sender, RoutedEventArgs e)
    {
        await Run(() => Save(HttpMethod.Post, false), "Лекарство добавлено.");
    }

    public async void Put_Click(object sender, RoutedEventArgs e)
    {
        await Run(() => Save(HttpMethod.Put, false), "Лекарство полностью изменено.");
    }

    public async void Patch_Click(object sender, RoutedEventArgs e)
    {
        await Run(() => Save(HttpMethod.Patch, true), "Выбранные поля изменены.");
    }

    public async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentId == 0)
        {
            Status = "Сначала открой запись для удаления.";
            UpdateBindings();
            return;
        }

        var answer = MessageBox.Show($"Удалить лекарство «{NameText}»?", "Удаление",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        await Run(async () =>
        {
            await Send(HttpMethod.Delete, $"api/v1/Medicines/{CurrentId}");
            savedMessage = "Сервер удалил запись.";
            ClearForm();
            await LoadList();
            window.MainFrame.Navigate(this);
        }, "Лекарство удалено.");
    }
}
