using Microsoft.Data.Sqlite;
using System.Data;

namespace LR25_AviaTickets;

public static class Database
{
    private static readonly string DbFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "avia_tickets.db");
    private static readonly string ConnectionString = $"Data Source={DbFile}";

    public static SqliteConnection GetConnection()
    {
        return new SqliteConnection(ConnectionString);
    }

    public static void Initialize()
    {
        using var con = GetConnection();
        con.Open();

        string sql = @"
CREATE TABLE IF NOT EXISTS Flights (
    FlightId INTEGER PRIMARY KEY AUTOINCREMENT,
    FlightNumber TEXT NOT NULL,
    FromCity TEXT NOT NULL,
    ToCity TEXT NOT NULL,
    DepartureDate TEXT NOT NULL,
    Price REAL NOT NULL
);
CREATE TABLE IF NOT EXISTS Passengers (
    PassengerId INTEGER PRIMARY KEY AUTOINCREMENT,
    FullName TEXT NOT NULL,
    Passport TEXT NOT NULL,
    Phone TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS Tickets (
    TicketId INTEGER PRIMARY KEY AUTOINCREMENT,
    FlightId INTEGER NOT NULL,
    PassengerId INTEGER NOT NULL,
    SeatNumber TEXT NOT NULL,
    SaleDate TEXT NOT NULL,
    Status TEXT NOT NULL,
    FOREIGN KEY (FlightId) REFERENCES Flights(FlightId),
    FOREIGN KEY (PassengerId) REFERENCES Passengers(PassengerId)
);";
        using var cmd = new SqliteCommand(sql, con);
        cmd.ExecuteNonQuery();

        using var countCmd = new SqliteCommand("SELECT COUNT(*) FROM Flights", con);
        long count = (long)countCmd.ExecuteScalar()!;
        if (count == 0) Seed(con);
    }

    private static void Seed(SqliteConnection con)
    {
        string sql = @"
INSERT INTO Flights (FlightNumber, FromCity, ToCity, DepartureDate, Price) VALUES
('PS101','Київ','Варшава','2026-05-25 09:30',3500),
('PS202','Львів','Прага','2026-05-26 12:15',4200),
('PS303','Одеса','Стамбул','2026-05-27 18:45',5100);

INSERT INTO Passengers (FullName, Passport, Phone) VALUES
('Іваненко Марія','AA123456','+380501112233'),
('Петренко Олег','BB987654','+380672224455'),
('Романченко Олександра','CC555777','+380931234567');

INSERT INTO Tickets (FlightId, PassengerId, SeatNumber, SaleDate, Status) VALUES
(1,1,'12A','2026-05-20','Продано'),
(2,2,'08C','2026-05-20','Бронь'),
(3,3,'15B','2026-05-21','Продано');";
        using var cmd = new SqliteCommand(sql, con);
        cmd.ExecuteNonQuery();
    }

    public static DataTable GetTable(string sql)
    {
        using var con = GetConnection();
        con.Open();
        using var cmd = new SqliteCommand(sql, con);
        using var reader = cmd.ExecuteReader();
        var table = new DataTable();
        table.Load(reader);
        return table;
    }

    public static void Execute(string sql, params SqliteParameter[] parameters)
    {
        using var con = GetConnection();
        con.Open();
        using var cmd = new SqliteCommand(sql, con);
        cmd.Parameters.AddRange(parameters);
        cmd.ExecuteNonQuery();
    }
}
