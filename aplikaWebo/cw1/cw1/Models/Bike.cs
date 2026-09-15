namespace cw1.Models;

public class Bike
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal PricePerDay { get; set; }
    public bool IsAvailable { get; set; }
}