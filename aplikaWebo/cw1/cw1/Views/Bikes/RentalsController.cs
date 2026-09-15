using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using cw1.Data;

namespace cw1.Controllers;

public class RentalsController : Controller
{
    private readonly Database _database;

    public RentalsController(Database database)
    {
        _database = database;
    }

    [HttpGet]
    public IActionResult Rent(int bikeId)
    {
        ViewBag.BikeId = bikeId;
        return View();
    }

    [HttpPost]
    public IActionResult Rent(int bikeId, string firstName, string lastName, string phone)
    {
        using (var connection = _database.GetConnection())
        {
            connection.Open();
            
            // 1. Zapisz klienta
            var cmdCustomer = new MySqlCommand(
                "INSERT INTO Customers (FirstName, LastName, Phone) VALUES (@fn, @ln, @ph); SELECT LAST_INSERT_ID();", connection);
            cmdCustomer.Parameters.AddWithValue("@fn", firstName);
            cmdCustomer.Parameters.AddWithValue("@ln", lastName);
            cmdCustomer.Parameters.AddWithValue("@ph", phone);
            int customerId = Convert.ToInt32(cmdCustomer.ExecuteScalar());

            // 2. Wygeneruj unikalny 5-cyfrowy kod
            Random rnd = new Random();
            string rentalCode = "";
            bool isUnique = false;

            while (!isUnique)
            {
                rentalCode = rnd.Next(10000, 99999).ToString();
                var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM Rentals WHERE RentalCode = @code", connection);
                checkCmd.Parameters.AddWithValue("@code", rentalCode);
                if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                {
                    isUnique = true; // Znaleźliśmy wolny kod!
                }
            }

            // 3. Utwórz wypożyczenie z kodem
            var cmdRental = new MySqlCommand(
                "INSERT INTO Rentals (RentalCode, BikeId, CustomerId, StartDate) VALUES (@code, @bId, @cId, CURDATE())", connection);
            cmdRental.Parameters.AddWithValue("@code", rentalCode);
            cmdRental.Parameters.AddWithValue("@bId", bikeId);
            cmdRental.Parameters.AddWithValue("@cId", customerId);
            cmdRental.ExecuteNonQuery();

            // 4. Zablokuj rower
            var cmdBike = new MySqlCommand("UPDATE Bikes SET IsAvailable = 0 WHERE Id = @bId", connection);
            cmdBike.Parameters.AddWithValue("@bId", bikeId);
            cmdBike.ExecuteNonQuery();

            // 5. Przekaż kod użytkownikowi przez TempData
            TempData["SuccessMessage"] = $"Sukces! Twój KOD ZWROTU to: {rentalCode}. Zapisz go!";
        }
        return RedirectToAction("Index", "Bikes");
    }

    [HttpGet]
    public IActionResult Return()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Return(string rentalCode)
    {
        using (var connection = _database.GetConnection())
        {
            connection.Open();
            // Pobieramy dane z 3 tabel naraz (Rentals, Bikes, Customers) szukając po kodzie (i tylko takich, które nie zostały jeszcze zwrócone)
            var cmdInfo = new MySqlCommand(@"
                SELECT r.Id AS RentalId, r.StartDate, b.PricePerDay, b.Id AS BikeId, c.FirstName, c.LastName 
                FROM Rentals r 
                JOIN Bikes b ON r.BikeId = b.Id 
                JOIN Customers c ON r.CustomerId = c.Id
                WHERE r.RentalCode = @code AND r.EndDate IS NULL", connection);
            cmdInfo.Parameters.AddWithValue("@code", rentalCode);
            
            using (var reader = cmdInfo.ExecuteReader())
            {
                if (reader.Read())
                {
                    int rentalId = reader.GetInt32("RentalId");
                    int bikeId = reader.GetInt32("BikeId");
                    string fullName = $"{reader.GetString("FirstName")} {reader.GetString("LastName")}";
                    DateTime startDate = reader.GetDateTime("StartDate");
                    decimal pricePerDay = reader.GetDecimal("PricePerDay");
                    
                    int days = (DateTime.Now.Date - startDate.Date).Days;
                    if (days == 0) days = 1;
                    
                    decimal totalPrice = days * pricePerDay;
                    DateTime deadline = DateTime.Now.AddDays(3); // +3 dni na zapłatę
                    reader.Close(); 

                    // Aktualizacja zwrotu w bazie
                    var cmdUpdateR = new MySqlCommand("UPDATE Rentals SET EndDate = CURDATE(), TotalPrice = @tp WHERE Id = @rId", connection);
                    cmdUpdateR.Parameters.AddWithValue("@tp", totalPrice);
                    cmdUpdateR.Parameters.AddWithValue("@rId", rentalId);
                    cmdUpdateR.ExecuteNonQuery();

                    var cmdUpdateB = new MySqlCommand("UPDATE Bikes SET IsAvailable = 1 WHERE Id = @bId", connection);
                    cmdUpdateB.Parameters.AddWithValue("@bId", bikeId);
                    cmdUpdateB.ExecuteNonQuery();
                    
                    // Przekazanie danych do pięknego widoku
                    ViewBag.CustomerName = fullName;
                    ViewBag.Days = days;
                    ViewBag.Total = totalPrice;
                    ViewBag.Deadline = deadline.ToString("dd.MM.yyyy");
                    return View();
                }
            }
        }
        ViewBag.Error = "Nie znaleziono aktywnego wypożyczenia o takim kodzie.";
        return View();
    }
}   