using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MapSharpLib;


namespace NodeApp
{
    /// <summary>
    /// Description of MainForm.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly Dictionary<int, MrNetworkNode> _nodes;

        public MainForm()
        {
            //
            // The InitializeComponent() call is required for Windows Forms designer support.
            //
            InitializeComponent();

            _nodes = new Dictionary<int, MrNetworkNode>();

            //
            // TODO: Add constructor code after the InitializeComponent() call.
            //
        }

        private void Button1Click(object sender, EventArgs e)
        {
            int port = (int) numericUpDown1.Value++;
            if (_nodes.ContainsKey(port))
            {
                textBox1.AppendText("Node Already Exists at port " + port + "\n");
                return;
            }

            string[] nodelines = textBox2.Lines;
            string[] newNodeLines = new string[nodelines.Length + 1];
            Array.Copy(nodelines, newNodeLines, nodelines.Length);
            newNodeLines[nodelines.Length] = port.ToString();
            textBox2.Lines = newNodeLines;
            var mrNetworkNode = new MrNetworkNode(port);

            mrNetworkNode.AddListener((arg) =>
            {
                textBox1.BeginInvoke((MethodInvoker) (() => { textBox1.AppendText(arg + "\n"); }));
            });
            _nodes.Add(port, mrNetworkNode);

            textBox1.AppendText("Node Started at port " + port + "\n");
        }

        private void Button2Click(object sender, EventArgs e)
        {
            int port = (int) numericUpDown1.Value--;
            if (!_nodes.ContainsKey(port))
            {
                textBox1.AppendText("Node doesn't exist at port " + port + "\n");
                return;
            }

            string[] nodelines = textBox2.Lines;
            string[] newNodeLines = new string[nodelines.Length - 1];
            string portS = port.ToString();
            int index = 0;
            foreach (string t in nodelines)
            {
                if (!t.Equals(portS))
                    newNodeLines[index++] = t;
            }

            textBox2.Lines = newNodeLines;


            _nodes[port].Stop();
            _nodes.Remove(port);

            textBox1.AppendText("Node Stopped at port " + port + "\n");
        }

        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (KeyValuePair<int, MrNetworkNode> kvp in _nodes)
                kvp.Value.Stop();
        }
    }
}