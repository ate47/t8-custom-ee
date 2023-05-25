using System.Diagnostics;

namespace T8CustomEE
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private static bool Injected = false;
        private void button1_Click(object sender, EventArgs e)
        {
            string message = "";
            if (Injected)
            {
                if (Library.Reverse(ref message))
                {
                    button1.Text = "Inject mod";
                    Injected = false;
                }
            }
            else
            {
                if (Library.Inject(ref message))
                {
                    button1.Text = "Reverse mod";
                    Injected = true;
                }
            }
            textBox.Lines = message.Split("\n");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var ps = new ProcessStartInfo("https://www.github.com/shiversoftdev/t7-compiler")
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
        }
    }
}