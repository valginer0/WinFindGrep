using System;
using System.Windows.Forms;
using WinFindGrep.Forms;

namespace WinFindGrep
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}