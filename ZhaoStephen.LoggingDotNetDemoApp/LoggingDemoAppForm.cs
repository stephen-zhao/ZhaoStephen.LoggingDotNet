using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZhaoStephen.LoggingDotNet;

namespace ZhaoStephen.LoggingDotNetDemoApp
{
    public partial class LoggingDemoAppForm : Form
    {
        private Logger _logger;

        public LoggingDemoAppForm()
        {
            InitializeComponent();

            _logger = new Logger("AppFormLogger");
            //_logger.AddOutputGeneric(str => this.Invoke(new Action(() => textBox2.AppendText(str))), LogOrnamentLvl.FULL);
            _logger.AddOutputControl(textBox2, LogOrnamentLvl.INCREASED);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                string cmd = textBox1.Text;
                _logger.Info(cmd);
                if (!RunCommand(cmd))
                {
                    _logger.Error("Failed to run command '" + cmd + "'.");
                }
                textBox1.Clear();
            }
        }

        private bool RunCommand(string cmd)
        {
            string[] args = cmd.Split(' ');
            switch (args[0])
            {
                case "help":
                    textBox2.AppendText("usage:\r\n > ahh   - prints a WARN message.\r\n > shinu - prints a FATAL message.\r\n > debug - prints a DEBUG message.\r\n > exit  - closes the demo app.\r\n");
                    break;
                case "exit":
                    Close();
                    break;
                case "ahh":
                    _logger.Warn("careful, dude...");
                    break;
                case "shinu":
                    _logger.Fatal("Don't eat that mushroom, it's fatal.");
                    break;
                case "debug":
                    _logger.Debug("This is a debug message");
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
