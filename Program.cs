using System;
using System.Windows.Forms;
using DemoExam.Forms;

namespace DemoExam
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
