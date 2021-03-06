﻿#region Usings
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using Mono.Nat;
using System.Globalization;
#endregion

namespace NovetusLauncher
{
	#region LauncherForm - Extended
	public partial class LauncherFormExtended : Form
	{
		#region Private Variables
		private DiscordRPC.EventHandlers handlers;
		private List<TreeNode> CurrentNodeMatches = new List<TreeNode>();
		private int LastNodeIndex = 0;
		private string LastSearchText;
		#endregion

		#region Constructor
		public LauncherFormExtended()
		{
			_fieldsTreeCache = new TreeView();
            InitializeComponent();

			Size = new Size(745, 377);
			panel2.Size = new Size(646, 272);
		}
        #endregion

        #region UPnP
        public void InitUPnP()
		{
			if (GlobalVars.UserConfiguration.UPnP)
			{
				try
				{
					NetFuncs.InitUPnP(DeviceFound, DeviceLost);
					GlobalFuncs.ConsolePrint("UPnP: Service initialized", 3, richTextBox1);
				}
				catch (Exception ex)
				{
					GlobalFuncs.ConsolePrint("UPnP: Unable to initialize UPnP. Reason - " + ex.Message, 2, richTextBox1);
				}
			}
		}

		public void StartUPnP(INatDevice device, Protocol protocol, int port)
		{
			if (GlobalVars.UserConfiguration.UPnP)
			{
				try
				{
					NetFuncs.StartUPnP(device, protocol, port);
					string IP = (!string.IsNullOrWhiteSpace(GlobalVars.UserConfiguration.AlternateServerIP) ? GlobalVars.UserConfiguration.AlternateServerIP : device.GetExternalIP().ToString());
					GlobalFuncs.ConsolePrint("UPnP: Port " + port + " opened on '" + IP + "' (" + protocol.ToString() + ")", 3, richTextBox1);
				}
				catch (Exception ex)
				{
					GlobalFuncs.ConsolePrint("UPnP: Unable to open port mapping. Reason - " + ex.Message, 2, richTextBox1);
				}
			}
		}

		public void StopUPnP(INatDevice device, Protocol protocol, int port)
		{
			if (GlobalVars.UserConfiguration.UPnP)
			{
				try
				{
					NetFuncs.StopUPnP(device, protocol, port);
					string IP = (!string.IsNullOrWhiteSpace(GlobalVars.UserConfiguration.AlternateServerIP) ? GlobalVars.UserConfiguration.AlternateServerIP : device.GetExternalIP().ToString());
					GlobalFuncs.ConsolePrint("UPnP: Port " + port + " closed on '" + IP + "' (" + protocol.ToString() + ")", 3, richTextBox1);
				}
				catch (Exception ex)
				{
					GlobalFuncs.ConsolePrint("UPnP: Unable to close port mapping. Reason - " + ex.Message, 2, richTextBox1);
				}
			}
		}

		private void DeviceFound(object sender, DeviceEventArgs args)
		{
			try
			{
				INatDevice device = args.Device;
				string IP = (!string.IsNullOrWhiteSpace(GlobalVars.UserConfiguration.AlternateServerIP) ? GlobalVars.UserConfiguration.AlternateServerIP : device.GetExternalIP().ToString());
				GlobalFuncs.ConsolePrint("UPnP: Device '" + IP + "' registered.", 3, richTextBox1);
				StartUPnP(device, Protocol.Udp, GlobalVars.UserConfiguration.RobloxPort);
				StartUPnP(device, Protocol.Tcp, GlobalVars.UserConfiguration.RobloxPort);
				StartUPnP(device, Protocol.Udp, GlobalVars.UserConfiguration.WebServerPort);
				StartUPnP(device, Protocol.Tcp, GlobalVars.UserConfiguration.WebServerPort);
			}
			catch (Exception ex)
			{
				GlobalFuncs.ConsolePrint("UPnP: Unable to register device. Reason - " + ex.Message, 2, richTextBox1);
			}
		}

		private void DeviceLost(object sender, DeviceEventArgs args)
		{
			try
			{
				INatDevice device = args.Device;
				string IP = (!string.IsNullOrWhiteSpace(GlobalVars.UserConfiguration.AlternateServerIP) ? GlobalVars.UserConfiguration.AlternateServerIP : device.GetExternalIP().ToString());
				GlobalFuncs.ConsolePrint("UPnP: Device '" + IP + "' disconnected.", 3, richTextBox1);
				StopUPnP(device, Protocol.Udp, GlobalVars.UserConfiguration.RobloxPort);
				StopUPnP(device, Protocol.Tcp, GlobalVars.UserConfiguration.RobloxPort);
				StopUPnP(device, Protocol.Udp, GlobalVars.UserConfiguration.WebServerPort);
				StopUPnP(device, Protocol.Tcp, GlobalVars.UserConfiguration.WebServerPort);
			}
			catch (Exception ex)
			{
				GlobalFuncs.ConsolePrint("UPnP: Unable to disconnect device. Reason - " + ex.Message, 2, richTextBox1);
			}
		}
		#endregion

		#region Discord
		public void ReadyCallback()
		{
			GlobalFuncs.ConsolePrint("Discord RPC: Ready", 3, richTextBox1);
		}

		public void DisconnectedCallback(int errorCode, string message)
		{
			GlobalFuncs.ConsolePrint("Discord RPC: Disconnected. Reason - " + errorCode + ": " + message, 2, richTextBox1);
		}

		public void ErrorCallback(int errorCode, string message)
		{
			GlobalFuncs.ConsolePrint("Discord RPC: Error. Reason - " + errorCode + ": " + message, 2, richTextBox1);
		}

		public void JoinCallback(string secret)
		{
		}

		public void SpectateCallback(string secret)
		{
		}

		public void RequestCallback(DiscordRPC.JoinRequest request)
		{
		}

		void StartDiscord()
		{
			if (GlobalVars.UserConfiguration.DiscordPresence)
			{
				handlers = new DiscordRPC.EventHandlers();
				handlers.readyCallback = ReadyCallback;
				handlers.disconnectedCallback += DisconnectedCallback;
				handlers.errorCallback += ErrorCallback;
				handlers.joinCallback += JoinCallback;
				handlers.spectateCallback += SpectateCallback;
				handlers.requestCallback += RequestCallback;
				DiscordRPC.Initialize(GlobalVars.appid, ref handlers, true, "");

				GlobalFuncs.UpdateRichPresence(GlobalVars.LauncherState.InLauncher, "", true);
			}
		}
		#endregion

		#region Web Server
		//udp clients will connect to the web server alongside the game.
		void StartWebServer()
		{
			if (SecurityFuncs.IsElevated)
			{
				try
				{
					GlobalVars.WebServer = new SimpleHTTPServer(GlobalPaths.DataPath, GlobalVars.UserConfiguration.WebServerPort);
					GlobalFuncs.ConsolePrint("WebServer: Server is running on port: " + GlobalVars.WebServer.Port.ToString(), 3, richTextBox1);
				}
				catch (Exception ex)
				{
					GlobalFuncs.ConsolePrint("WebServer: Failed to launch WebServer. Some features may not function. (" + ex.Message + ")", 2, richTextBox1);
				}
			}
			else
			{
				GlobalFuncs.ConsolePrint("WebServer: Failed to launch WebServer. Some features may not function. (Did not run as Administrator)", 2, richTextBox1);
			}
		}

		void StopWebServer()
		{
			if (SecurityFuncs.IsElevated)
			{
				try
				{
					GlobalFuncs.ConsolePrint("WebServer: Server has stopped on port: " + GlobalVars.WebServer.Port.ToString(), 2, richTextBox1);
					GlobalVars.WebServer.Stop();
				}
				catch (Exception ex)
				{
					GlobalFuncs.ConsolePrint("WebServer: Failed to stop WebServer. Some features may not function. (" + ex.Message + ")", 2, richTextBox1);
				}
			}
			else
			{
				GlobalFuncs.ConsolePrint("WebServer: Failed to stop WebServer. Some features may not function. (Did not run as Administrator)", 2, richTextBox1);
			}
		}
		#endregion

		#region Form Events
		async void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (tabControl1.SelectedTab)
			{
				case TabPage pg2 when pg2 == tabControl1.TabPages["tabPage2"]:
					treeView1.Nodes.Clear();
					_fieldsTreeCache.Nodes.Clear();
					textBox4.Text = "";
					listBox2.Items.Clear();
					listBox3.Items.Clear();
					listBox4.Items.Clear();
					//since we are async, DO THESE first or we'll clear out random stuff.
					textBox3.Text = "Loading...";
					string IP = await SecurityFuncs.GetExternalIPAddressAsync();
					textBox3.Text = "";
					string[] lines1 = {
						SecurityFuncs.Base64Encode((!string.IsNullOrWhiteSpace(GlobalVars.UserConfiguration.AlternateServerIP) ? GlobalVars.UserConfiguration.AlternateServerIP : IP)),
						SecurityFuncs.Base64Encode(GlobalVars.UserConfiguration.RobloxPort.ToString()),
						SecurityFuncs.Base64Encode(GlobalVars.UserConfiguration.SelectedClient)
					};
					string URI = "novetus://" + SecurityFuncs.Base64Encode(string.Join("|", lines1), true);
					string[] lines2 = {
						SecurityFuncs.Base64Encode("localhost"),
						SecurityFuncs.Base64Encode(GlobalVars.UserConfiguration.RobloxPort.ToString()),
						SecurityFuncs.Base64Encode(GlobalVars.UserConfiguration.SelectedClient)
					};
					string URI2 = "novetus://" + SecurityFuncs.Base64Encode(string.Join("|", lines2), true);
					string[] text = {
					   "Client: " + GlobalVars.UserConfiguration.SelectedClient,
					   "IP: " + (!string.IsNullOrWhiteSpace(GlobalVars.UserConfiguration.AlternateServerIP) ? GlobalVars.UserConfiguration.AlternateServerIP : IP),
					   "Port: " + GlobalVars.UserConfiguration.RobloxPort.ToString(),
					   "Map: " + GlobalVars.UserConfiguration.Map,
					   "Players: " + GlobalVars.UserConfiguration.PlayerLimit,
					   "Version: Novetus " + GlobalVars.ProgramInformation.Version,
					   "Online URI Link:",
					   URI,
					   "Local URI Link:",
					   URI2,
					   GlobalVars.IsWebServerOn ? "Web Server URL:" : "",
					   GlobalVars.IsWebServerOn ? "http://" + (!string.IsNullOrWhiteSpace(GlobalVars.UserConfiguration.AlternateServerIP) ? GlobalVars.UserConfiguration.AlternateServerIP : IP) + ":" + GlobalVars.WebServer.Port.ToString() : "",
					   GlobalVars.IsWebServerOn ? "Local Web Server URL:" : "",
					   GlobalVars.IsWebServerOn ? "http://localhost:" + (GlobalVars.WebServer.Port.ToString()).ToString() : ""
					   };

					foreach (string str in text)
					{
						if (!string.IsNullOrWhiteSpace(str))
						{
							textBox3.AppendText(str + Environment.NewLine);
						}
					}
					textBox3.SelectionStart = 0;
					textBox3.ScrollToCaret();
					break;
				case TabPage pg4 when pg4 == tabControl1.TabPages["tabPage4"]:
					string mapdir = GlobalPaths.MapsDir;
					string[] fileexts = new string[] { ".rbxl", ".rbxlx" };
					TreeNodeHelper.ListDirectory(treeView1, mapdir, fileexts);
					TreeNodeHelper.CopyNodes(treeView1.Nodes, _fieldsTreeCache.Nodes);
					treeView1.SelectedNode = TreeNodeHelper.SearchTreeView(GlobalVars.UserConfiguration.Map, treeView1.Nodes);
					treeView1.Focus();
					textBox3.Text = "";
					listBox2.Items.Clear();
					listBox3.Items.Clear();
					listBox4.Items.Clear();
					break;
				case TabPage pg3 when pg3 == tabControl1.TabPages["tabPage3"]:
					string clientdir = GlobalPaths.ClientDir;
					DirectoryInfo dinfo = new DirectoryInfo(clientdir);
					DirectoryInfo[] Dirs = dinfo.GetDirectories();
					foreach (DirectoryInfo dir in Dirs)
					{
						listBox2.Items.Add(dir.Name);
					}
					listBox2.SelectedItem = GlobalVars.UserConfiguration.SelectedClient;
					treeView1.Nodes.Clear();
					_fieldsTreeCache.Nodes.Clear();
					textBox4.Text = "";
					textBox3.Text = "";
					listBox3.Items.Clear();
					listBox4.Items.Clear();
					break;
				case TabPage pg6 when pg6 == tabControl1.TabPages["tabPage6"]:
					string[] lines_server = File.ReadAllLines(GlobalPaths.ConfigDir + "\\servers.txt");
					string[] lines_ports = File.ReadAllLines(GlobalPaths.ConfigDir + "\\ports.txt");
					listBox3.Items.AddRange(lines_server);
					listBox4.Items.AddRange(lines_ports);
					treeView1.Nodes.Clear();
					_fieldsTreeCache.Nodes.Clear();
					textBox4.Text = "";
					textBox3.Text = "";
					listBox2.Items.Clear();
					break;
				default:
					treeView1.Nodes.Clear();
					_fieldsTreeCache.Nodes.Clear();
					textBox4.Text = "";
					textBox3.Text = "";
					listBox2.Items.Clear();
					listBox3.Items.Clear();
					listBox4.Items.Clear();
					break;
			}
		}

		void Button1Click(object sender, EventArgs e)
		{
            if (GlobalVars.LocalPlayMode)
            {
                GeneratePlayerID();
                GenerateTripcode();
            }
            else
            {
                WriteConfigValues();
            }

            StartClient();

            if (GlobalVars.UserConfiguration.CloseOnLaunch)
            {
                Visible = false;
            }
        }

        void Button2Click(object sender, EventArgs e)
		{
            WriteConfigValues();
            StartServer(false);

            if (GlobalVars.UserConfiguration.CloseOnLaunch)
            {
                Visible = false;
            }
        }

        void Button3Click(object sender, EventArgs e)
		{
            DialogResult result = MessageBox.Show("If you want to test out your place, you will have to save your place in Novetus's map folder, then launch your place in Play Solo.", "Novetus - Launch ROBLOX Studio", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.Cancel)
                return;

            WriteConfigValues();
            StartStudio(false);
            if (GlobalVars.UserConfiguration.CloseOnLaunch)
            {
                Visible = false;
            }
        }

        void Button18Click(object sender, EventArgs e)
        {
            WriteConfigValues();
            StartServer(true);

            if (GlobalVars.UserConfiguration.CloseOnLaunch)
            {
                Visible = false;
            }
        }

        void Button19Click(object sender, EventArgs e)
        {
            WriteConfigValues();
            StartSolo();

            if (GlobalVars.UserConfiguration.CloseOnLaunch)
            {
                Visible = false;
            }
        }

        private void button35_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("If you want to test out your place, you will have to save your place in Novetus's map folder, then launch your place in Play Solo.", "Novetus - Launch ROBLOX Studio", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.Cancel)
                return;

            WriteConfigValues();
            StartStudio(true);
            if (GlobalVars.UserConfiguration.CloseOnLaunch)
            {
                Visible = false;
            }
        }

        void MainFormLoad(object sender, EventArgs e)
		{
            Text = "Novetus " + GlobalVars.ProgramInformation.Version;
    		GlobalFuncs.ConsolePrint("Novetus version " + GlobalVars.ProgramInformation.Version + " loaded. Initializing config.", 4, richTextBox1);
            GlobalFuncs.ConsolePrint("Novetus path: " + GlobalPaths.BasePath, 4, richTextBox1);

            if (File.Exists(GlobalPaths.RootPath + "\\changelog.txt"))
			{
    			richTextBox2.Text = File.ReadAllText(GlobalPaths.RootPath + "\\changelog.txt");
    		}
    		else
    		{
    			GlobalFuncs.ConsolePrint("ERROR - " + GlobalPaths.RootPath + "\\changelog.txt not found.", 2, richTextBox1);
    		}

            if (File.Exists(GlobalPaths.RootPath + "\\README-AND-CREDITS.TXT"))
            {
                richTextBox3.Text = File.ReadAllText(GlobalPaths.RootPath + "\\README-AND-CREDITS.TXT");
            }
            else
            {
                GlobalFuncs.ConsolePrint("ERROR - " + GlobalPaths.RootPath + "\\README-AND-CREDITS.TXT not found.", 2, richTextBox1);
            }

            if (!File.Exists(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigName))
			{
				GlobalFuncs.ConsolePrint("WARNING - " + GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigName + " not found. Creating one with default values.", 5, richTextBox1);
				WriteConfigValues();
			}
			if (!File.Exists(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigNameCustomization))
			{
				GlobalFuncs.ConsolePrint("WARNING - " + GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigNameCustomization + " not found. Creating one with default values.", 5, richTextBox1);
				WriteCustomizationValues();
			}
			if (!File.Exists(GlobalPaths.ConfigDir + "\\servers.txt"))
			{
				GlobalFuncs.ConsolePrint("WARNING - " + GlobalPaths.ConfigDir + "\\servers.txt not found. Creating empty file.", 5, richTextBox1);
				File.Create(GlobalPaths.ConfigDir + "\\servers.txt").Dispose();
			}
			if (!File.Exists(GlobalPaths.ConfigDir + "\\ports.txt"))
			{
				GlobalFuncs.ConsolePrint("WARNING - " + GlobalPaths.ConfigDir + "\\ports.txt not found. Creating empty file.", 5, richTextBox1);
				File.Create(GlobalPaths.ConfigDir + "\\ports.txt").Dispose();
			}

			GlobalFuncs.CreateAssetCacheDirectories();

			label8.Text = Application.ProductVersion;
    		LocalVars.important = SecurityFuncs.GenerateMD5(Assembly.GetExecutingAssembly().Location);
            label11.Text = GlobalVars.ProgramInformation.Version;
    		
    		label12.Text = SplashReader.GetSplash();
            LocalVars.prevsplash = label12.Text;

            ReadConfigValues(true);
    		InitUPnP();
    		StartDiscord();
			if (GlobalVars.UserConfiguration.WebServer)
			{
				StartWebServer();
			}
		}

        void MainFormClose(object sender, CancelEventArgs e)
        {
            if (!GlobalVars.LocalPlayMode)
            {
                WriteConfigValues();
            }
            if (GlobalVars.UserConfiguration.DiscordPresence)
            {
                DiscordRPC.Shutdown();
            }
			if (GlobalVars.IsWebServerOn)
			{
				StopWebServer();
			}
        }

		void ReadConfigValues(bool initial = false)
		{
			GlobalFuncs.Config(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigName, false);

			checkBox1.Checked = GlobalVars.UserConfiguration.CloseOnLaunch;
            textBox5.Text = GlobalVars.UserConfiguration.UserID.ToString();
            label18.Text = GlobalVars.UserConfiguration.PlayerTripcode.ToString();
            numericUpDown3.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.PlayerLimit);
            textBox2.Text = GlobalVars.UserConfiguration.PlayerName;
			label26.Text = GlobalVars.UserConfiguration.SelectedClient;
			label28.Text = GlobalVars.UserConfiguration.Map;
			treeView1.SelectedNode = TreeNodeHelper.SearchTreeView(GlobalVars.UserConfiguration.Map, treeView1.Nodes);
            treeView1.Focus();
            numericUpDown1.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.RobloxPort);
			numericUpDown2.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.RobloxPort);
			label37.Text = GlobalVars.IP;
			label38.Text = GlobalVars.UserConfiguration.RobloxPort.ToString();
			checkBox2.Checked = GlobalVars.UserConfiguration.DiscordPresence;
			checkBox5.Checked = GlobalVars.UserConfiguration.ReShade;
			checkBox6.Checked = GlobalVars.UserConfiguration.ReShadeFPSDisplay;
			checkBox7.Checked = GlobalVars.UserConfiguration.ReShadePerformanceMode;
			checkBox4.Checked = GlobalVars.UserConfiguration.UPnP;

			if (SecurityFuncs.IsElevated)
			{
				checkBox8.Enabled = true;
				checkBox8.Checked = GlobalVars.UserConfiguration.WebServer;
			}
			else
            {
				checkBox8.Enabled = false;
			}

			switch (GlobalVars.UserConfiguration.GraphicsMode)
			{
				case Settings.GraphicsOptions.Mode.OpenGL:
					comboBox1.SelectedIndex = 1;
					break;
				case Settings.GraphicsOptions.Mode.DirectX:
					comboBox1.SelectedIndex = 2;
					break;
				default:
					comboBox1.SelectedIndex = 0;
					break;
			}

			switch (GlobalVars.UserConfiguration.QualityLevel)
			{
				case Settings.GraphicsOptions.Level.VeryLow:
					comboBox2.SelectedIndex = 1;
					break;
				case Settings.GraphicsOptions.Level.Low:
					comboBox2.SelectedIndex = 2;
					break;
				case Settings.GraphicsOptions.Level.Medium:
					comboBox2.SelectedIndex = 3;
					break;
				case Settings.GraphicsOptions.Level.High:
					comboBox2.SelectedIndex = 4;
					break;
				case Settings.GraphicsOptions.Level.Ultra:
					comboBox2.SelectedIndex = 5;
					break;
				case Settings.GraphicsOptions.Level.Custom:
					comboBox2.SelectedIndex = 6;
					break;
				default:
					comboBox2.SelectedIndex = 0;
					break;
			}

			switch (GlobalVars.UserConfiguration.LauncherStyle)
			{
				case Settings.UIOptions.Style.Compact:
					comboBox3.SelectedIndex = 1;
					break;
				case Settings.UIOptions.Style.Extended:
				default:
					comboBox3.SelectedIndex = 0;
					break;
			}

			GlobalFuncs.ConsolePrint("Config loaded.", 3, richTextBox1);
			ReadClientValues(initial);
		}
		
		void WriteConfigValues()
		{
			GlobalFuncs.Config(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigName, true);
			GlobalFuncs.ReadClientValues(richTextBox1);
			GlobalFuncs.ConsolePrint("Config Saved.", 3, richTextBox1);
		}

		void WriteCustomizationValues()
		{
			GlobalFuncs.Customization(GlobalPaths.ConfigDir + "\\" + GlobalPaths.ConfigNameCustomization, true);
			GlobalFuncs.ConsolePrint("Config Saved.", 3, richTextBox1);
		}

		void ReadClientValues(bool initial = false)
		{
			GlobalFuncs.ReadClientValues(richTextBox1, initial);

			switch (GlobalVars.SelectedClientInfo.UsesPlayerName)
			{
				case true:
					textBox2.Enabled = true;
					break;
				case false:
					textBox2.Enabled = false;
					break;
			}

			switch (GlobalVars.SelectedClientInfo.UsesID)
			{
				case true:
					textBox5.Enabled = true;
					button4.Enabled = true;
					if (GlobalVars.IP.Equals("localhost"))
					{
						checkBox3.Enabled = true;
					}
					break;
				case false:
					textBox5.Enabled = false;
					button4.Enabled = false;
					checkBox3.Enabled = false;
					GlobalVars.LocalPlayMode = false;
					break;
			}

			if (!string.IsNullOrWhiteSpace(GlobalVars.SelectedClientInfo.Warning))
			{
				label30.Text = GlobalVars.SelectedClientInfo.Warning;
				label30.Visible = true;
			}
			else
			{
				label30.Visible = false;
			}

			textBox6.Text = GlobalVars.SelectedClientInfo.Description;
			label26.Text = GlobalVars.UserConfiguration.SelectedClient;
		}

		void GeneratePlayerID()
		{
			GlobalFuncs.GeneratePlayerID();
			textBox5.Text = Convert.ToString(GlobalVars.UserConfiguration.UserID);
		}

        void GenerateTripcode()
        {
            GlobalFuncs.GenerateTripcode();
            label18.Text = GlobalVars.UserConfiguration.PlayerTripcode;
        }
		
		void TextBox1TextChanged(object sender, EventArgs e)
		{
			GlobalVars.IP = textBox1.Text;
			checkBox3.Enabled = false;
			GlobalVars.LocalPlayMode = false;
			label37.Text = GlobalVars.IP;
		}
		
		void CheckBox1CheckedChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.CloseOnLaunch = checkBox1.Checked;
		}
		
		void Button4Click(object sender, EventArgs e)
		{
			GeneratePlayerID();
		}
		
		void Button5Click(object sender, EventArgs e)
		{
			WriteConfigValues();
			MessageBox.Show("Config Saved!");
		}
		
		void TextBox2TextChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.PlayerName = textBox2.Text;
		}
		
		void ListBox2SelectedIndexChanged(object sender, EventArgs e)
		{
			string ourselectedclient = GlobalVars.UserConfiguration.SelectedClient;
			GlobalVars.UserConfiguration.SelectedClient = listBox2.SelectedItem.ToString();
			if (!ourselectedclient.Equals(GlobalVars.UserConfiguration.SelectedClient))
			{
				ReadClientValues(true);
			}
			else
			{
				ReadClientValues();
			}
			GlobalFuncs.UpdateRichPresence(GlobalVars.LauncherState.InLauncher, "");

			FormCollection fc = Application.OpenForms;

			foreach (Form frm in fc)
			{
				//iterate through
				if (frm.Name == "CustomGraphicsOptions")
				{
					frm.Close();
					break;
				}
			}
		}
		
		void CheckBox3CheckedChanged(object sender, EventArgs e)
		{
			GlobalVars.LocalPlayMode = checkBox3.Checked;
		}
		
		void TextBox5TextChanged(object sender, EventArgs e)
		{
			int parsedValue;
			if (int.TryParse(textBox5.Text, out parsedValue))
			{
				if (textBox5.Text.Equals(""))
				{
					GlobalVars.UserConfiguration.UserID = 0;
				}
				else
				{
					GlobalVars.UserConfiguration.UserID = Convert.ToInt32(textBox5.Text);
				}
			}
			else
			{
				GlobalVars.UserConfiguration.UserID = 0;
			}
		}
		
		void Button8Click(object sender, EventArgs e)
		{
			CharacterCustomizationExtended ccustom = new CharacterCustomizationExtended();
			ccustom.Show();
		}
		
		void Button9Click(object sender, EventArgs e)
		{
			ResetConfigValues();
			MessageBox.Show("Config Reset!");
		}
		
		void ListBox3SelectedIndexChanged(object sender, EventArgs e)
		{
			GlobalVars.IP = listBox3.SelectedItem.ToString();
			textBox1.Text = GlobalVars.IP;
			checkBox3.Enabled = false;
			GlobalVars.LocalPlayMode = false;
			label37.Text = GlobalVars.IP;
		}
		
		void ListBox4SelectedIndexChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.RobloxPort = Convert.ToInt32(listBox4.SelectedItem.ToString());
			numericUpDown1.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.RobloxPort);
			numericUpDown2.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.RobloxPort);
		}
		
		void Button10Click(object sender, EventArgs e)
		{
			File.AppendAllText(GlobalPaths.ConfigDir + "\\servers.txt", GlobalVars.IP + Environment.NewLine);
		}
		
		void Button11Click(object sender, EventArgs e)
		{
			File.AppendAllText(GlobalPaths.ConfigDir + "\\ports.txt", GlobalVars.UserConfiguration.RobloxPort + Environment.NewLine);
		}
		
		void Button12Click(object sender, EventArgs e)
		{
			if (listBox3.SelectedIndex >= 0)
			{
				TextLineRemover.RemoveTextLines(new List<string> { listBox3.SelectedItem.ToString() }, GlobalPaths.ConfigDir + "\\servers.txt", GlobalPaths.ConfigDir + "\\servers.tmp");
				listBox3.Items.Clear();
				string[] lines_server = File.ReadAllLines(GlobalPaths.ConfigDir + "\\servers.txt");
				listBox3.Items.AddRange(lines_server);
			}
		}
		
		void Button13Click(object sender, EventArgs e)
		{
			if (listBox4.SelectedIndex >= 0)
			{
				TextLineRemover.RemoveTextLines(new List<string> { listBox4.SelectedItem.ToString() }, GlobalPaths.ConfigDir + "\\ports.txt", GlobalPaths.ConfigDir + "\\ports.tmp");
				listBox4.Items.Clear();
				string[] lines_ports = File.ReadAllLines(GlobalPaths.ConfigDir + "\\ports.txt");
				listBox4.Items.AddRange(lines_ports);
			}
		}
		
		void Button14Click(object sender, EventArgs e)
		{
			File.Create(GlobalPaths.ConfigDir + "\\servers.txt").Dispose();
			listBox3.Items.Clear();
			string[] lines_server = File.ReadAllLines(GlobalPaths.ConfigDir + "\\servers.txt");
			listBox3.Items.AddRange(lines_server);
		}
		
		void Button15Click(object sender, EventArgs e)
		{
			File.Create(GlobalPaths.ConfigDir + "\\ports.txt").Dispose();
			listBox4.Items.Clear();
			string[] lines_ports = File.ReadAllLines(GlobalPaths.ConfigDir + "\\ports.txt");
			listBox4.Items.AddRange(lines_ports);
		}
		
		void Button16Click(object sender, EventArgs e)
		{
			File.AppendAllText(GlobalPaths.ConfigDir + "\\servers.txt", GlobalVars.IP + Environment.NewLine);
			listBox3.Items.Clear();
			string[] lines_server = File.ReadAllLines(GlobalPaths.ConfigDir + "\\servers.txt");
			listBox3.Items.AddRange(lines_server);			
		}
		
		void Button17Click(object sender, EventArgs e)
		{
			File.AppendAllText(GlobalPaths.ConfigDir + "\\ports.txt", GlobalVars.UserConfiguration.RobloxPort + Environment.NewLine);
			listBox4.Items.Clear();
			string[] lines_ports = File.ReadAllLines(GlobalPaths.ConfigDir + "\\ports.txt");
			listBox4.Items.AddRange(lines_ports);
		}
		
		
		
		void richTextBox1_KeyDown(object sender, KeyEventArgs e)
        {
			//Command proxy
            
            int totalLines = richTextBox1.Lines.Length;
            if (totalLines > 0)
            {
				string lastLine = richTextBox1.Lines[totalLines - 1];
            
            	if (e.KeyCode == Keys.Enter)
            	{
            		richTextBox1.AppendText(Environment.NewLine);
            		ConsoleProcessCommands(lastLine);
            		e.Handled = true;
            	}
            }
            
            if ( e.Modifiers == Keys.Control )
			{
				switch(e.KeyCode)
				{
				case Keys.X:
				case Keys.Z:
					e.Handled = true;
					break;
				default:
					break;
				}
			}
        }

		void ResetConfigValues()
		{
			//https://stackoverflow.com/questions/9029351/close-all-open-forms-except-the-main-menu-in-c-sharp
			List<Form> openForms = new List<Form>();

			foreach (Form f in Application.OpenForms)
				openForms.Add(f);

			foreach (Form f in openForms)
			{
				if (f.Name != "LauncherFormExtended")
					f.Close();
			}

			GlobalFuncs.ResetConfigValues();
			WriteConfigValues();
			ReadConfigValues();
		}

		void StartClient()
		{
			GlobalFuncs.LaunchRBXClient(ScriptType.Client, false, true, new EventHandler(ClientExited), richTextBox1);
		}

		void StartSolo()
		{
			GlobalFuncs.LaunchRBXClient(ScriptType.Solo, false, false, new EventHandler(ClientExited), richTextBox1);
		}

		void StartServer(bool no3d)
		{
			GlobalFuncs.LaunchRBXClient(ScriptType.Server, no3d, false, new EventHandler(ServerExited), richTextBox1);
		}

		void StartStudio(bool nomap)
		{
			GlobalFuncs.LaunchRBXClient(ScriptType.Studio, false, nomap, new EventHandler(ClientExited), richTextBox1);
		}

		void StartEasterEgg()
		{
			GlobalFuncs.LaunchRBXClient(ScriptType.EasterEgg, false, false, new EventHandler(EasterEggExited), richTextBox1);
		}

		void ClientExited(object sender, EventArgs e)
		{
			GlobalFuncs.UpdateRichPresence(GlobalVars.LauncherState.InLauncher, "");
			if (GlobalVars.UserConfiguration.CloseOnLaunch)
			{
				Visible = true;
			}
		}

		void ServerExited(object sender, EventArgs e)
		{
			if (GlobalVars.UserConfiguration.CloseOnLaunch)
			{
				Visible = true;
			}
		}

		void EasterEggExited(object sender, EventArgs e)
		{
			GlobalFuncs.UpdateRichPresence(GlobalVars.LauncherState.InLauncher, "");
			label12.Text = LocalVars.prevsplash;
			if (GlobalVars.UserConfiguration.CloseOnLaunch)
			{
				Visible = true;
			}
		}

		void ConsoleProcessCommands(string cmd)
		{
			switch(cmd)
            {
				case string server3d when string.Compare(server3d, "server 3d", true, CultureInfo.InvariantCulture) == 0:
					StartServer(false);
					break;
				case string serverno3d when string.Compare(serverno3d, "server no3d", true, CultureInfo.InvariantCulture) == 0:
					StartServer(false);
					break;
				case string client when string.Compare(client, "client", true, CultureInfo.InvariantCulture) == 0:
					StartClient();
					break;
				case string solo when string.Compare(solo, "solo", true, CultureInfo.InvariantCulture) == 0:
					StartSolo();
					break;
				case string studiomap when string.Compare(studiomap, "studio map", true, CultureInfo.InvariantCulture) == 0:
					StartStudio(false);
					break;
				case string studionomap when string.Compare(studionomap, "studio nomap", true, CultureInfo.InvariantCulture) == 0:
					StartStudio(true);
					break;
				case string configsave when string.Compare(configsave, "config save", true, CultureInfo.InvariantCulture) == 0:
					WriteConfigValues();
					break;
				case string configload when string.Compare(configload, "config load", true, CultureInfo.InvariantCulture) == 0:
					ReadConfigValues();
					break;
				case string configreset when string.Compare(configreset, "config reset", true, CultureInfo.InvariantCulture) == 0:
					ResetConfigValues();
					break;
				case string help when string.Compare(help, "help", true, CultureInfo.InvariantCulture) == 0:
					ConsoleHelp();
					break;
				case string sdk when string.Compare(sdk, "sdk", true, CultureInfo.InvariantCulture) == 0:
					LoadLauncher();
					break;
				case string webserverstart when string.Compare(webserverstart, "webserver start", true, CultureInfo.InvariantCulture) == 0:
					if (!GlobalVars.IsWebServerOn)
					{
						StartWebServer();
					}
					else
					{
						GlobalFuncs.ConsolePrint("WebServer: There is already a web server on.", 2, richTextBox1);
					}
					break;
				case string webserverstop when string.Compare(webserverstop, "webserver stop", true, CultureInfo.InvariantCulture) == 0:
					if (GlobalVars.IsWebServerOn)
					{
						StopWebServer();
					}
					else
					{
						GlobalFuncs.ConsolePrint("WebServer: There is no web server on.", 2, richTextBox1);
					}
					break;
				case string webserverrestart when string.Compare(webserverrestart, "webserver restart", true, CultureInfo.InvariantCulture) == 0:
					try
					{
						GlobalFuncs.ConsolePrint("WebServer: Restarting...", 4, richTextBox1);
						StopWebServer();
						StartWebServer();
					}
					catch (Exception ex)
					{
						GlobalFuncs.ConsolePrint("WebServer: Cannot restart web server. (" + ex.Message + ")", 2, richTextBox1);
					}
					break;
				case string important when string.Compare(important, LocalVars.important, true, CultureInfo.InvariantCulture) == 0:
					GlobalVars.AdminMode = true;
					GlobalFuncs.ConsolePrint("ADMIN MODE ENABLED.", 4, richTextBox1);
					GlobalFuncs.ConsolePrint("YOU ARE GOD.", 2, richTextBox1);
					break;
				default:
					GlobalFuncs.ConsolePrint("ERROR 3 - Command is either not registered or valid", 2, richTextBox1);
					break;
            }
		}

        void LoadLauncher()
        {
            NovetusSDK im = new NovetusSDK();
            im.Show();
            GlobalFuncs.ConsolePrint("Novetus SDK Launcher Loaded.", 4, richTextBox1);
        }

        void ConsoleHelp()
		{
			GlobalFuncs.ConsolePrint("Help:", 3, richTextBox1);
			GlobalFuncs.ConsolePrint("---------", 1, richTextBox1);
			GlobalFuncs.ConsolePrint("= client | Launches client with launcher settings", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= solo | Launches client in Play Solo mode with launcher settings", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= server 3d | Launches server with launcher settings", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= server no3d | Launches server in NoGraphics mode with launcher settings", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= studio map | Launches Roblox Studio with the selected map", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= studio nomap | Launches Roblox Studio without the selected map", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= sdk | Launches the Novetus SDK Launcher", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("---------", 1, richTextBox1);
			GlobalFuncs.ConsolePrint("= config save | Saves the config file", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= config load | Reloads the config file", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= config reset | Resets the config file", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("---------", 1, richTextBox1);
			GlobalFuncs.ConsolePrint("= webserver restart | Restarts the web server", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= webserver stop | Stops a web server if there is one on.", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("= webserver start | Starts a web server if there isn't one on yet.", 4, richTextBox1);
			GlobalFuncs.ConsolePrint("---------", 1, richTextBox1);
		}
		
		void Button21Click(object sender, EventArgs e)
		{
			if (SecurityFuncs.IsElevated)
			{
				try
      			{
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = GlobalPaths.ClientDir + @"\\" + GlobalVars.ProgramInformation.RegisterClient1 + @"\\RobloxApp_studio.exe";
                    startInfo.Arguments = "/regserver";
                    startInfo.Verb = "runas";
                    process.StartInfo = startInfo;
                    process.Start();

                    Process process2 = new Process();
                    ProcessStartInfo startInfo2 = new ProcessStartInfo();
                    startInfo2.FileName = GlobalPaths.ClientDir + @"\\" + GlobalVars.ProgramInformation.RegisterClient2 + @"\\RobloxApp_studio.exe";
                    startInfo2.Arguments = "/regserver";
                    startInfo2.Verb = "runas";
                    process2.StartInfo = startInfo2;
                    process2.Start();

                    GlobalFuncs.ConsolePrint("UserAgent Library successfully installed and registered!", 3, richTextBox1);
					MessageBox.Show("UserAgent Library successfully installed and registered!", "Novetus - Register UserAgent Library", MessageBoxButtons.OK, MessageBoxIcon.Information);
      			}
      			catch (Exception ex)
                {
        			GlobalFuncs.ConsolePrint("ERROR - Failed to register. (" + ex.Message + ")", 2, richTextBox1);
					MessageBox.Show("Failed to register. (Error: " + ex.Message + ")","Novetus - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      			}
			}
			else
			{
				GlobalFuncs.ConsolePrint("ERROR - Failed to register. (Did not run as Administrator)", 2, richTextBox1);
				MessageBox.Show("Failed to register. (Error: Did not run as Administrator)","Novetus - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
		
		void NumericUpDown1ValueChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.RobloxPort = Convert.ToInt32(numericUpDown1.Value);
			numericUpDown2.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.RobloxPort);
			label38.Text = GlobalVars.UserConfiguration.RobloxPort.ToString();
		}
		
		void NumericUpDown2ValueChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.RobloxPort = Convert.ToInt32(numericUpDown2.Value);
			numericUpDown1.Value = Convert.ToDecimal(GlobalVars.UserConfiguration.RobloxPort);
			label38.Text = GlobalVars.UserConfiguration.RobloxPort.ToString();
		}
		
		void NumericUpDown3ValueChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.PlayerLimit = Convert.ToInt32(numericUpDown3.Value);
		}
		
		void Button7Click(object sender, EventArgs e)
		{
			numericUpDown1.Value = Convert.ToDecimal(GlobalVars.DefaultRobloxPort);
			numericUpDown2.Value = Convert.ToDecimal(GlobalVars.DefaultRobloxPort);
			GlobalVars.UserConfiguration.RobloxPort = GlobalVars.DefaultRobloxPort;
		}
		
		void Button23Click(object sender, EventArgs e)
		{
			File.AppendAllText(GlobalPaths.ConfigDir + "\\ports.txt", GlobalVars.UserConfiguration.RobloxPort + Environment.NewLine);
		}
		
		void Button22Click(object sender, EventArgs e)
		{
			numericUpDown1.Value = Convert.ToDecimal(GlobalVars.DefaultRobloxPort);
			numericUpDown2.Value = Convert.ToDecimal(GlobalVars.DefaultRobloxPort);
			GlobalVars.UserConfiguration.RobloxPort = GlobalVars.DefaultRobloxPort;
		}
		
		void TreeView1AfterSelect(object sender, TreeViewEventArgs e)
		{
			if (treeView1.SelectedNode.Nodes.Count == 0)
			{
				GlobalVars.UserConfiguration.Map = treeView1.SelectedNode.Text.ToString();
                GlobalVars.UserConfiguration.MapPathSnip = treeView1.SelectedNode.FullPath.ToString().Replace(@"\", @"\\");
                GlobalVars.UserConfiguration.MapPath = GlobalPaths.BasePath + @"\\" + GlobalVars.UserConfiguration.MapPathSnip;
				label28.Text = GlobalVars.UserConfiguration.Map;

                if (File.Exists(GlobalPaths.RootPath + @"\\" + treeView1.SelectedNode.FullPath.ToString().Replace(".rbxl", "").Replace(".rbxlx", "") + "_desc.txt"))
                {
                    textBox4.Text = File.ReadAllText(GlobalPaths.RootPath + @"\\" + treeView1.SelectedNode.FullPath.ToString().Replace(".rbxl", "").Replace(".rbxlx", "") + "_desc.txt");
                }
                else
                {
                    textBox4.Text = treeView1.SelectedNode.Text.ToString();
                }
            }
		}
		
		void Button6Click(object sender, EventArgs e)
		{
			Process.Start("explorer.exe", GlobalPaths.MapsDir.Replace(@"\\",@"\"));
		}
		
		void CheckBox4CheckedChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.UPnP = checkBox4.Checked;
		}

		void CheckBox4Click(object sender, EventArgs e)
		{
			switch (checkBox4.Checked)
			{
				case false:
					MessageBox.Show("Novetus will now restart.", "Novetus - UPnP", MessageBoxButtons.OK, MessageBoxIcon.Information);
					break;
				default:
					MessageBox.Show("Novetus will now restart." + Environment.NewLine + "Make sure to check if your router has UPnP functionality enabled. Please note that some routers may not support UPnP, and some ISPs will block the UPnP protocol. This may not work for all users.", "Novetus - UPnP", MessageBoxButtons.OK, MessageBoxIcon.Information);
					break;
			}

			WriteConfigValues();
			Application.Restart();
		}

		void Button24Click(object sender, EventArgs e)
		{
			treeView1.Nodes.Clear();
			_fieldsTreeCache.Nodes.Clear();
        	string mapdir = GlobalPaths.MapsDir;
			string[] fileexts = new string[] { ".rbxl", ".rbxlx" };
			TreeNodeHelper.ListDirectory(treeView1, mapdir, fileexts);
			TreeNodeHelper.CopyNodes(treeView1.Nodes,_fieldsTreeCache.Nodes);
			treeView1.SelectedNode = TreeNodeHelper.SearchTreeView(GlobalVars.UserConfiguration.Map, treeView1.Nodes);
			treeView1.Focus();
            if (File.Exists(GlobalPaths.RootPath + @"\\" + treeView1.SelectedNode.FullPath.ToString().Replace(".rbxl", "").Replace(".rbxlx", "") + "_desc.txt"))
            {
                textBox4.Text = File.ReadAllText(GlobalPaths.RootPath + @"\\" + treeView1.SelectedNode.FullPath.ToString().Replace(".rbxl", "").Replace(".rbxlx", "") + "_desc.txt");
            }
            else
            {
                textBox4.Text = treeView1.SelectedNode.Text.ToString();
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            AddonLoader addon = new AddonLoader();
            addon.setFileListDisplay(10);
            try
            {
                addon.LoadAddon();
                if (!string.IsNullOrWhiteSpace(addon.getInstallOutcome()))
                {
                    GlobalFuncs.ConsolePrint("AddonLoader - " + addon.getInstallOutcome(), 3, richTextBox1);
                }
            }
            catch (Exception)
            {
                if (!string.IsNullOrWhiteSpace(addon.getInstallOutcome()))
                {
                    GlobalFuncs.ConsolePrint("AddonLoader - " + addon.getInstallOutcome(), 2, richTextBox1);
                }
            }

            if (!string.IsNullOrWhiteSpace(addon.getInstallOutcome()))
            {
                MessageBox.Show(addon.getInstallOutcome());
            }
        }

        private void button26_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(GlobalPaths.AssetCacheDir))
            {
                Directory.Delete(GlobalPaths.AssetCacheDir, true);
                GlobalFuncs.ConsolePrint("Asset cache cleared!", 3, richTextBox1);
                MessageBox.Show("Asset cache cleared!");
            }
            else
            {
                MessageBox.Show("There is no asset cache to clear.");
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
			GlobalVars.UserConfiguration.DiscordPresence = checkBox2.Checked;
		}

		void CheckBox2Click(object sender, EventArgs e)
		{
			switch (checkBox2.Checked)
			{
				case false:
					MessageBox.Show("Novetus will now restart.", "Novetus - Discord Rich Presence", MessageBoxButtons.OK, MessageBoxIcon.Information);
					break;
				default:
					MessageBox.Show("Novetus will now restart." + Environment.NewLine + "Make sure the Discord app is open so this change can take effect.", "Novetus - Discord Rich Presence", MessageBoxButtons.OK, MessageBoxIcon.Information);
					break;
			}

			WriteConfigValues();
			Application.Restart();
		}

		private void button27_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage1;
        }

        private void button20_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage2;
        }

        private void button28_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage3;
        }

        private void button29_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage4;
        }

        private void button30_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage6;
        }

        private void button31_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage7;
        }

        private void button32_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage8;
        }

        private void button33_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage5;
        }

        private void button34_Click(object sender, EventArgs e)
        {
            LoadLauncher();
        }

        private void label8_Click(object sender, EventArgs e)
        {
            if (LocalVars.Clicks < 10)
            {
                LocalVars.Clicks += 1;

				switch(LocalVars.Clicks)
                {
					case 1:
						label12.Text = "Hi " + GlobalVars.UserConfiguration.PlayerName + "!";
						break;
					case 3:
						label12.Text = "How are you doing today?";
						break;
					case 6:
						label12.Text = "I just wanted to say something.";
						break;
					case 9:
						label12.Text = "Just wait a little on the last click, OK?";
						break;
					case 10:
						label12.Text = "Thank you. <3";
						WriteConfigValues();
						StartEasterEgg();

						if (GlobalVars.UserConfiguration.CloseOnLaunch)
						{
							Visible = false;
						}
						break;
					default:
						break;
                }
            }
        }

		private void checkBox5_CheckedChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.ReShade = checkBox5.Checked;
		}

		private void checkBox6_CheckedChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.ReShadeFPSDisplay = checkBox6.Checked;
		}

		private void checkBox7_CheckedChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.ReShadePerformanceMode = checkBox7.Checked;
		}

		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
			switch (comboBox1.SelectedIndex)
			{
				case 1:
					GlobalVars.UserConfiguration.GraphicsMode = Settings.GraphicsOptions.Mode.OpenGL;
					break;
				case 2:
					GlobalVars.UserConfiguration.GraphicsMode = Settings.GraphicsOptions.Mode.DirectX;
					break;
				default:
					GlobalVars.UserConfiguration.GraphicsMode = Settings.GraphicsOptions.Mode.Automatic;
					break;
			}

			GlobalFuncs.ReadClientValues(richTextBox1);
		}

		private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (comboBox2.SelectedIndex)
			{
				case 1:
					GlobalVars.UserConfiguration.QualityLevel = Settings.GraphicsOptions.Level.VeryLow;
					break;
				case 2:
					GlobalVars.UserConfiguration.QualityLevel = Settings.GraphicsOptions.Level.Low;
					break;
				case 3:
					GlobalVars.UserConfiguration.QualityLevel = Settings.GraphicsOptions.Level.Medium;
					break;
				case 4:
					GlobalVars.UserConfiguration.QualityLevel = Settings.GraphicsOptions.Level.High;
					break;
				case 5:
					GlobalVars.UserConfiguration.QualityLevel = Settings.GraphicsOptions.Level.Ultra;
					break;
				case 6:
					GlobalVars.UserConfiguration.QualityLevel = Settings.GraphicsOptions.Level.Custom;
					break;
				default:
					GlobalVars.UserConfiguration.QualityLevel = Settings.GraphicsOptions.Level.Automatic;
					break;
			}

			GlobalFuncs.ReadClientValues(richTextBox1);

			if (comboBox2.SelectedIndex != 6)
			{
				//https://stackoverflow.com/questions/9029351/close-all-open-forms-except-the-main-menu-in-c-sharp

				FormCollection fc = Application.OpenForms;

				foreach (Form frm in fc)
				{
					//iterate through
					if (frm.Name == "CustomGraphicsOptions")
					{
						frm.Close();
						break;
					}
				}
			}
		}

		private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (comboBox3.SelectedIndex)
			{
				case 1:
					GlobalVars.UserConfiguration.LauncherStyle = Settings.UIOptions.Style.Compact;
					WriteConfigValues();
					Application.Restart();
					break;
				default:
					break;
			}
		}

		// FINALLY. https://stackoverflow.com/questions/11530643/treeview-search

		private void SearchButton_Click(object sender, EventArgs e)
		{
			string searchText = SearchBar.Text;

			if (string.IsNullOrWhiteSpace(searchText))
			{
				return;
			};

			try
			{
				if (LastSearchText != searchText)
				{
					//It's a new Search
					CurrentNodeMatches.Clear();
					LastSearchText = searchText;
					LastNodeIndex = 0;
					SearchNodes(searchText, treeView1.Nodes[0]);
				}

				if (LastNodeIndex >= 0 && CurrentNodeMatches.Count > 0 && LastNodeIndex < CurrentNodeMatches.Count)
				{
					TreeNode selectedNode = CurrentNodeMatches[LastNodeIndex];
					LastNodeIndex++;
					treeView1.SelectedNode = selectedNode;
					treeView1.SelectedNode.Expand();
					treeView1.Select();
				}
				else
				{
					//It's a new Search
					CurrentNodeMatches.Clear();
					LastSearchText = searchText;
					LastNodeIndex = 0;
					SearchNodes(searchText, treeView1.Nodes[0]);
					TreeNode selectedNode = CurrentNodeMatches[LastNodeIndex];
					LastNodeIndex++;
					treeView1.SelectedNode = selectedNode;
					treeView1.SelectedNode.Expand();
					treeView1.Select();
				}
			}
			catch (Exception)
			{
				MessageBox.Show("The map '" + searchText + "' cannot be found. Please try another term.");
			}
		}

		private void button36_Click(object sender, EventArgs e)
		{
			if (comboBox2.SelectedIndex == 6)
			{
				CustomGraphicsOptions opt = new CustomGraphicsOptions();
				opt.Show();
			}
			else
			{
				MessageBox.Show("You do not have the 'Custom' option selected. Please select it before continuing.");
			}
		}

		private void checkBox8_CheckedChanged(object sender, EventArgs e)
		{
			GlobalVars.UserConfiguration.WebServer = checkBox8.Checked;
		}

		void CheckBox8Click(object sender, EventArgs e)
		{
			switch (checkBox8.Checked)
			{
				case false:
					MessageBox.Show("Novetus will now restart.", "Novetus - UPnP", MessageBoxButtons.OK, MessageBoxIcon.Information);
					break;
				default:
					MessageBox.Show("Novetus will now restart." + Environment.NewLine + "Make sure to check if your router has UPnP functionality enabled. Please note that some routers may not support UPnP, and some ISPs will block the UPnP protocol. This may not work for all users.", "Novetus - UPnP", MessageBoxButtons.OK, MessageBoxIcon.Information);
					break;
			}

			WriteConfigValues();
			Application.Restart();
		}
		#endregion

		#region Functions
		private void SearchNodes(string SearchText, TreeNode StartNode)
		{
			while (StartNode != null)
			{
				if (StartNode.Text.ToLower().Contains(SearchText.ToLower()))
				{
					CurrentNodeMatches.Add(StartNode);
				};
				if (StartNode.Nodes.Count != 0)
				{
					SearchNodes(SearchText, StartNode.Nodes[0]);//Recursive Search 
				};
				StartNode = StartNode.NextNode;
			};

		}
		#endregion
	}
    #endregion
}
