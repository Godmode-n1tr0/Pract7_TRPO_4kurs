using System.Globalization;
using System.Windows;

namespace pract7.Models;

public class MedicineForm
{
    public string Name { get; set; } = "";
    public string Manufacturer { get; set; } = "";
    public string Price { get; set; } = "";
    public string Date { get; set; } = "";
    public bool IsAvailable { get; set; }

    public MedicineForm(Medicine? medicine = null)
    {
        if (medicine == null) return;
        Name = medicine.Name;
        Manufacturer = medicine.Manufacturer;
        Price = medicine.Price.ToString(CultureInfo.InvariantCulture);
        Date = medicine.ExpirationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        IsAvailable = medicine.IsAvailable;
    }

    public Medicine? ReadMedicine()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Trim().Length > 150)
        {
            MessageBox.Show("Введи название: от 1 до 150 символов.");
            return null;
        }
        if (string.IsNullOrWhiteSpace(Manufacturer) || Manufacturer.Trim().Length > 120)
        {
            MessageBox.Show("Введи производителя: от 1 до 120 символов.");
            return null;
        }
        if (!decimal.TryParse(Price.Trim().Replace(',', '.'),
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out decimal price) ||
            price < 0 || price > 99999999.99m || decimal.Round(price, 2) != price)
        {
            MessageBox.Show("Введи цену от 0 до 99999999,99, не больше двух знаков после запятой.");
            return null;
        }
        if (!DateOnly.TryParseExact(Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly date))
        {
            MessageBox.Show("Введи существующую дату в формате ГГГГ-ММ-ДД.");
            return null;
        }
        return new Medicine
        {
            Name = Name.Trim(),
            Manufacturer = Manufacturer.Trim(),
            Price = price,
            ExpirationDate = date,
            IsAvailable = IsAvailable
        };
    }
}
