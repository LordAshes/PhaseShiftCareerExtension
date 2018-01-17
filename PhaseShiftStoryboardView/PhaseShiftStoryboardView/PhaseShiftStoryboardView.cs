using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;

namespace PhaseShiftStoryboardView
{
    public partial class PhaseShiftStoryboardView : Form
    {
        private ChromiumWebBrowser _browser = null;

        public PhaseShiftStoryboardView(string[] args)
        {
            InitializeComponent();
            InitializeWebBrowser(args);
        }

        public void InitializeWebBrowser(string[] args)
        {
            string URL = Environment.CurrentDirectory + "\\" + args[0].ToUpper().Replace(".INI", ".HTML");
            if (System.IO.File.Exists(URL))
            {
                URL = URL + "?";
                for (int a = 1; a < args.Length; a++)
                {
                    URL = URL + "Param" + a + "=" + args[a] + ";";
                }
            }

            CefSettings settings = new CefSettings();
            Cef.Initialize(settings);

            _browser = new ChromiumWebBrowser(URL);

            this.Controls.Add(_browser);

            _browser.Dock = DockStyle.Fill;
        }
    }
}

