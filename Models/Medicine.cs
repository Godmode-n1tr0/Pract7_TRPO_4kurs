using System;
using System.Collections.Generic;

namespace Pract6.Models;

public partial class Medicine
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Manufacturer { get; set; } = null!;

    public decimal Price { get; set; }

    public DateOnly ExpirationDate { get; set; }

    public bool IsAvailable { get; set; }
}
