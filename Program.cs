using System;
using System.Windows.Forms;

namespace LR25_AviaTickets;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Database.Initialize();
        Application.Run(new MainForm());
    }
}
