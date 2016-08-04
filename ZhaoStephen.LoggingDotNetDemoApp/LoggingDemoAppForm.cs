using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
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
            _logger.AddOutputControl(textBox2, LogOrnamentLvl.INCREASED);
            _logger.AddOutputWriter(Console.Out, LogOrnamentLvl.FULL);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                string cmd = textBox1.Text;
                textBox2.AppendText(" > " + cmd + Environment.NewLine);
                if (!TryRunCommand(cmd))
                {
                    _logger.Error("Failed to run command '" + cmd + "'. Enter 'help' for usage.");
                }
                textBox1.Clear();
                e.Handled = true;
            }
        }

        private bool TryRunCommand(string cmd)
        {
            string[] args = cmd.Split(' ');
            switch (args[0])
            {
                case "help":
                    if (args.Length == 1)
                        textBox2.AppendText(String.Join(Environment.NewLine,
                            "usage:",
                            " > please - prints an INFO message.",
                            " > ahh    - prints a WARN message.",
                            " > damson - prints an ERROR message.",
                            " > shinu  - prints a FATAL message.",
                            " > debug  - prints a DEBUG message.",
                            " > exit   - closes the demo app." + Environment.NewLine));
                    break;
                case "exit":
                    Close();
                    break;
                case "please":
                    if (args.Length == 1)
                        _logger.Info("For your information, using the abbreviated FYI sometimes sounds passive aggressive.");
                    break;
                case "ahh":
                    if (args.Length == 1)
                        _logger.Warn("careful, dude...");
                    break;
                case "damson":
                    if (args.Length == 1)
                        _logger.Error("Damn son, it's not spelt damson!!");
                    break;
                case "shinu":
                    if (args.Length == 1)
                        _logger.Fatal("Don't eat that mushroom, it's fatal.");
                    break;
                case "debug":
                    if (args.Length == 1)
                        _logger.Debug("This is a debug message.");
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
