/*
 * Created by SharpDevelop.
 * User: Administrator
 * Date: 6/16/2009
 * Time: 4:09 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
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
		Dictionary<int, MrNetworkNode> Nodes;
		
		public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			Nodes = new Dictionary<int, MrNetworkNode>();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		
		void Button1Click(object sender, EventArgs e)
		{
			int port = (int)numericUpDown1.Value++;
			if(Nodes.ContainsKey(port))
			{
				textBox1.AppendText("Node Already Exists at port " + port + "\n");
				return;
			}
			
			string[] nodelines = textBox2.Lines;
			string[] newNodeLines = new string[nodelines.Length+1];
			Array.Copy(nodelines,newNodeLines,nodelines.Length);
			newNodeLines[nodelines.Length] = port.ToString();
			textBox2.Lines = newNodeLines;
			
			Nodes.Add(port, new MrNetworkNode(port));
			
			
			textBox1.AppendText("Node Started at port " + port + "\n");
		}
		
		void Button2Click(object sender, EventArgs e)
		{
			int port = (int)numericUpDown1.Value--;
			if(!Nodes.ContainsKey(port))
			{
				textBox1.AppendText("Node Doesn't Exist at port " + port + "\n");
				return;
			}
			
			string[] nodelines = textBox2.Lines;
			string[] newNodeLines = new string[nodelines.Length-1];
			string portS = port.ToString();
			int index = 0;
			foreach (string t in nodelines)
			{
				if(!t.Equals(portS))
					newNodeLines[index++] = t;
			}
			textBox2.Lines = newNodeLines;
			
			
			Nodes[port].Stop();
			Nodes.Remove(port);
			
			textBox1.AppendText("Node Stopped at port " + port + "\n");
		}
		
		void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			foreach(KeyValuePair<int, MrNetworkNode> kvp in Nodes)
				kvp.Value.Stop();
		}
	}
}
