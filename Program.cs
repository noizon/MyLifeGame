using System;
using System.Windows.Forms;
using MyLifeGame.UI;

namespace MyLifeGame
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MenuForm());   
        }
    }
}
