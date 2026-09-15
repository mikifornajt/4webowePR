using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using cw1.Data;
using cw1.Models;

namespace cw1.Controllers;

public class BikesController : Controller
{
    private readonly Database _database;

    public BikesController(Database database)
    {
        _database = database;
    }

    // Wyświetlanie listy rowerów
    public IActionResult Index()
    {
        var bikes = new List<Bike>();

        using (var connection = _database.GetConnection())
        {
            connection.Open();
            var command = new MySqlCommand("SELECT * FROM Bikes", connection);
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    bikes.Add(new Bike
                    {
                        Id = reader.GetInt32("Id"),
                        Name = reader.GetString("Name"),
                        PricePerDay = reader.GetDecimal("PricePerDay"),
                        IsAvailable = reader.GetBoolean("IsAvailable")
                    });
                }
            }
        }

        return View(bikes);
    }

    // Otwieranie widoku z formularzem
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // Odbieranie danych z formularza i zapis w MySQL
    [HttpPost]
    public IActionResult Create(Bike bike)
    {
        using (var connection = _database.GetConnection())
        {
            connection.Open();
            var command = new MySqlCommand(
                "INSERT INTO Bikes (Name, PricePerDay, IsAvailable) VALUES (@Name, @PricePerDay, @IsAvailable)", 
                connection);

            // Parametry chronią przed atakami typu SQL Injection
            command.Parameters.AddWithValue("@Name", bike.Name);
            command.Parameters.AddWithValue("@PricePerDay", bike.PricePerDay);
            command.Parameters.AddWithValue("@IsAvailable", bike.IsAvailable);

            command.ExecuteNonQuery();
        }

        return RedirectToAction("Index");
    }
}