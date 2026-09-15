using MySql.Data.MySqlClient;

namespace cw1.Data;

public class Database
{
    private readonly string _connectionString = 
        "Server=127.0.0.1;Database=bike_rental;Uid=root;Pwd=;";

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}