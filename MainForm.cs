using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace LR25_AviaTickets;

public class MainForm : Form
{
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly TextBox txtSearch = new() { Width = 220 };
    private readonly ComboBox cmbFlight = new() { Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox cmbPassenger = new() { Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtSeat = new() { Width = 100 };
    private readonly ComboBox cmbStatus = new() { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };

    public MainForm()
    {
        Text = "ЛР25 — Авіарейси й продаж авіаквитків";
        Width = 1100;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        var topPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 115, Padding = new Padding(10), AutoScroll = true };

        var btnLoad = new Button { Text = "Показати всі квитки", Width = 150 };
        var btnAdd = new Button { Text = "Додати квиток", Width = 130 };
        var btnSearch = new Button { Text = "Пошук пасажира", Width = 130 };
        var btnReport1 = new Button { Text = "Звіт 1: продані квитки", Width = 170 };
        var btnReport2 = new Button { Text = "Звіт 2: виручка за рейсами", Width = 190 };
        var btnSave = new Button { Text = "Зберегти звіт CSV", Width = 150 };

        cmbStatus.Items.AddRange(new object[] { "Продано", "Бронь", "Повернено" });
        cmbStatus.SelectedIndex = 0;

        topPanel.Controls.Add(new Label { Text = "Рейс:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        topPanel.Controls.Add(cmbFlight);
        topPanel.Controls.Add(new Label { Text = "Пасажир:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        topPanel.Controls.Add(cmbPassenger);
        topPanel.Controls.Add(new Label { Text = "Місце:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        topPanel.Controls.Add(txtSeat);
        topPanel.Controls.Add(new Label { Text = "Статус:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        topPanel.Controls.Add(cmbStatus);
        topPanel.Controls.Add(btnAdd);
        topPanel.SetFlowBreak(btnAdd, true);

        topPanel.Controls.Add(btnLoad);
        topPanel.Controls.Add(new Label { Text = "ПІБ пасажира:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        topPanel.Controls.Add(txtSearch);
        topPanel.Controls.Add(btnSearch);
        topPanel.Controls.Add(btnReport1);
        topPanel.Controls.Add(btnReport2);
        topPanel.Controls.Add(btnSave);

        Controls.Add(grid);
        Controls.Add(topPanel);

        Load += (_, _) => { LoadCombos(); ShowAllTickets(); };
        btnLoad.Click += (_, _) => ShowAllTickets();
        btnAdd.Click += (_, _) => AddTicket();
        btnSearch.Click += (_, _) => SearchPassenger();
        btnReport1.Click += (_, _) => ShowSoldTicketsReport();
        btnReport2.Click += (_, _) => ShowRevenueReport();
        btnSave.Click += (_, _) => SaveGridToCsv();
    }

    private void LoadCombos()
    {
        cmbFlight.DataSource = Database.GetTable("SELECT FlightId, FlightNumber || ' | ' || FromCity || ' - ' || ToCity AS Info FROM Flights");
        cmbFlight.DisplayMember = "Info";
        cmbFlight.ValueMember = "FlightId";

        cmbPassenger.DataSource = Database.GetTable("SELECT PassengerId, FullName FROM Passengers");
        cmbPassenger.DisplayMember = "FullName";
        cmbPassenger.ValueMember = "PassengerId";
    }

    private void ShowAllTickets()
    {
        grid.DataSource = Database.GetTable(@"
SELECT Tickets.TicketId AS 'ID', Flights.FlightNumber AS 'Рейс', Flights.FromCity AS 'Звідки',
       Flights.ToCity AS 'Куди', Flights.DepartureDate AS 'Дата вильоту',
       Passengers.FullName AS 'Пасажир', Tickets.SeatNumber AS 'Місце',
       Tickets.SaleDate AS 'Дата продажу', Tickets.Status AS 'Статус', Flights.Price AS 'Ціна'
FROM Tickets
JOIN Flights ON Tickets.FlightId = Flights.FlightId
JOIN Passengers ON Tickets.PassengerId = Passengers.PassengerId
ORDER BY Tickets.TicketId DESC");
    }

    private void AddTicket()
    {
        if (cmbFlight.SelectedValue == null || cmbPassenger.SelectedValue == null || string.IsNullOrWhiteSpace(txtSeat.Text))
        {
            MessageBox.Show("Заповніть рейс, пасажира і місце.", "Помилка");
            return;
        }

        Database.Execute(@"INSERT INTO Tickets (FlightId, PassengerId, SeatNumber, SaleDate, Status)
                           VALUES (@flight, @passenger, @seat, @date, @status)",
            new SqliteParameter("@flight", cmbFlight.SelectedValue),
            new SqliteParameter("@passenger", cmbPassenger.SelectedValue),
            new SqliteParameter("@seat", txtSeat.Text.Trim()),
            new SqliteParameter("@date", DateTime.Now.ToString("yyyy-MM-dd")),
            new SqliteParameter("@status", cmbStatus.Text));

        txtSeat.Clear();
        ShowAllTickets();
        MessageBox.Show("Квиток додано до бази даних.");
    }

    private void SearchPassenger()
    {
        string key = txtSearch.Text.Trim().Replace("'", "''");
        grid.DataSource = Database.GetTable($@"
SELECT Flights.FlightNumber AS 'Рейс', Flights.ToCity AS 'Напрям', Passengers.FullName AS 'Пасажир',
       Passengers.Phone AS 'Телефон', Tickets.SeatNumber AS 'Місце', Tickets.Status AS 'Статус'
FROM Tickets
JOIN Flights ON Tickets.FlightId = Flights.FlightId
JOIN Passengers ON Tickets.PassengerId = Passengers.PassengerId
WHERE Passengers.FullName LIKE '%{key}%'");
    }

    private void ShowSoldTicketsReport()
    {
        grid.DataSource = Database.GetTable(@"
SELECT Flights.FlightNumber AS 'Рейс', Flights.FromCity || ' - ' || Flights.ToCity AS 'Маршрут',
       Passengers.FullName AS 'Пасажир', Tickets.SeatNumber AS 'Місце', Flights.Price AS 'Сума'
FROM Tickets
JOIN Flights ON Tickets.FlightId = Flights.FlightId
JOIN Passengers ON Tickets.PassengerId = Passengers.PassengerId
WHERE Tickets.Status = 'Продано'
ORDER BY Flights.FlightNumber");
    }

    private void ShowRevenueReport()
    {
        grid.DataSource = Database.GetTable(@"
SELECT Flights.FlightNumber AS 'Рейс', Flights.FromCity || ' - ' || Flights.ToCity AS 'Маршрут',
       COUNT(Tickets.TicketId) AS 'Кількість проданих квитків', SUM(Flights.Price) AS 'Загальна виручка'
FROM Tickets
JOIN Flights ON Tickets.FlightId = Flights.FlightId
WHERE Tickets.Status = 'Продано'
GROUP BY Flights.FlightId, Flights.FlightNumber, Flights.FromCity, Flights.ToCity
ORDER BY 'Загальна виручка' DESC");
    }

    private void SaveGridToCsv()
    {
        if (grid.Rows.Count == 0) return;
        using var dialog = new SaveFileDialog { Filter = "CSV файл|*.csv", FileName = "report.csv" };
        if (dialog.ShowDialog() != DialogResult.OK) return;

        var sb = new StringBuilder();
        var headers = grid.Columns.Cast<DataGridViewColumn>().Select(c => c.HeaderText);
        sb.AppendLine(string.Join(";", headers));

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow) continue;
            var cells = row.Cells.Cast<DataGridViewCell>().Select(c => Convert.ToString(c.Value)?.Replace(";", ","));
            sb.AppendLine(string.Join(";", cells));
        }

        File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show("Звіт збережено.");
    }
}
