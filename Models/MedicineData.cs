namespace pract7.Models;

public class MedicineData
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Manufacturer { get; set; }
    public decimal? Price { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public bool? IsAvailable { get; set; }
}
