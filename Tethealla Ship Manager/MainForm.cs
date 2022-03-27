/*
    Tethealla Ship Manager
    Copyright 2022 Michelle Powers

    Icon assets from the Tethealla project (https://pioneer2.net) were used in this project.
    This project is reliant on Tethealla, but is not affiliated in any way with its developers.
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace Tethealla_Ship_Settings_Editor
{   
    public partial class MainForm : Form
    {
        // Class globals for GM settings - GM_info stores GC# and rights level; rights_info stores the rights key assigned to each rights level.
        public static Dictionary<string, string> GM_info = new Dictionary<string, string>();
        public static Dictionary<string, string> rights_info = new Dictionary<string, string>();

        public MainForm()
        {
            InitializeComponent();
        }

        // Iterates through processes to check whether a specific one is running.
        private bool ProgramIsRunning(string FullPath)
        {
            string FilePath = Path.GetDirectoryName(FullPath);
            string FileName = Path.GetFileNameWithoutExtension(FullPath).ToLower();
            bool isRunning = false;

            Process[] pList = Process.GetProcessesByName(FileName);

            foreach (Process p in pList)
            {
                // A try-catch statement is needed here to catch exceptions due to weird Windows behavior, in case a program is closed while reading.
                try
                {
                    if (p.MainModule.FileName.StartsWith(FilePath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        isRunning = true; // Process was found!
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
            return isRunning;
        }       

        // Ship thread function. Automatically restart the ship if it closes for any reason, then update running status label appropriately.
        public void serverStart()
        {
            try
            {
                bool isRunning, shipUp;

                for ( ; ; )
                {
                    isRunning = ProgramIsRunning(@System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\ship_server.exe");

                    if (isRunning)
                    {
                        lnkDown.Invoke(new MethodInvoker(() => lnkDown.Hide()));
                        lnkRunning.Invoke(new MethodInvoker(() => lnkRunning.Show()));         
                        shipUp = false;             
                        IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                        IPEndPoint[] endPoints = properties.GetActiveTcpListeners();

                        // Check if the ship is listening, and update our status if so.
                        foreach (IPEndPoint e in endPoints)
                        {
                            if(e.Port == nmPort.Value)
                            {
                                shipUp = true;
                                lnkRunning.Text = "Running";
                                lnkRunning.LinkColor = Color.Green;
                                break;
                            }
                        }                    

                        if (!shipUp)
                        {
                            lnkRunning.Text = "Starting";
                            lnkRunning.LinkColor = Color.Navy;
                        }
                    }
                    else
                    {
                        lnkDown.Invoke(new MethodInvoker(() => lnkDown.Show()));
                        lnkRunning.Invoke(new MethodInvoker(() => lnkRunning.Hide()));
                        if (chkRestart.Checked == true)
                        {
                            // Restart the process and hide the console window!
                            Process ship_process = new Process();
                            ship_process.StartInfo.FileName = "ship_server.exe";
                            ship_process.StartInfo.UseShellExecute = true;
                            ship_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                            ship_process.Start();
                            lnkDown.Invoke(new MethodInvoker(() => lnkDown.Hide()));
                            lnkRunning.Invoke(new MethodInvoker(() => lnkRunning.Show()));
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch
            {
                return;
            }
        }      

        // Write the specified user preference flag for auto-restart to our config file.
        private void write_restart_flag(int flag)
        {
            using (FileStream fs = File.Open("ship_manager.ini", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter fileData = new StreamWriter(fs))
                {
                    fileData.WriteLine("# Preference for 'Restart Automatically' checkbox. 0 = disabled, 1 = enabled.");
                    fileData.Write(flag);
                }
                fs.Close();
            }
            return;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] shipSettings = new string[14];
            int restartFlag = 0;
            bool useDefaultSettings = false;

            // Start the ship status checking thread.
            Thread serverActiveThread = new Thread(serverStart);
            serverActiveThread.IsBackground = true;
            serverActiveThread.Start();        

            lnkRunning.Hide();

            // Read ship_manager.ini, ignoring comment lines.
            try
            {
                using (FileStream fs = File.Open("ship_manager.ini", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string tempLine = String.Empty;

                        while (!reader.EndOfStream)
                        {
                            tempLine = reader.ReadLine();
                            if (tempLine.Substring(0, 1) != "#")
                            {
                                restartFlag = int.Parse(tempLine);
                                break;
                            }
                        }
                    }
                    fs.Close();
                }
            }
            catch 
            {
                write_restart_flag(0); // Not found, use default of 0.
            }

            if (restartFlag == 1) chkRestart.Checked = true;
            else chkRestart.Checked = false;

            // Read ship.ini, ignoring comment lines.
            try
            {
                using (FileStream fs = File.Open("ship.ini", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        int idx = 0;
                        string tempLine = String.Empty;

                        while (!reader.EndOfStream)
                        {
                            tempLine = reader.ReadLine();
                            if (tempLine.Substring(0, 1) != "#")
                            {
                                shipSettings[idx] = tempLine;
                                idx++;
                            }
                            if (idx > 13) break;
                        }
                    }
                }
            }
            catch // ship.ini not found, use default settings.
            {
                useDefaultSettings = true;
            }

            if (!useDefaultSettings)
            {
                if (shipSettings[0] == "auto") // User's IP 
                {
                    txtIP.ReadOnly = true;
                    chkAuto.Checked = true;
                }
                else txtIP.Text = shipSettings[0];

                nmPort.Value = int.Parse(shipSettings[1]); // Ship Port

                nmBlocks.Value = int.Parse(shipSettings[2]); // Number of blocks

                nmMaxUsers.Value = int.Parse(shipSettings[3]); // Max # of users

                txtLoginIP.Text = shipSettings[4]; // Login IP

                txtShipName.Text = shipSettings[5]; // Ship name

                // Event setting
                switch (shipSettings[6])
                {
                    case "0":
                        cbEvent.Text = "None";
                        break;
                    case "1":
                        cbEvent.Text = "Christmas";
                        break;
                    case "2":
                        cbEvent.Text = "None";
                        break;
                    case "3":
                        cbEvent.Text = "Valentines";
                        break;
                    case "4":
                        cbEvent.Text = "Easter";
                        break;
                    case "5":
                        cbEvent.Text = "Halloween";
                        break;
                    case "6":
                        cbEvent.Text = "Sonic";
                        break;
                    case "7":
                        cbEvent.Text = "New Years";
                        break;
                    case "8":
                        cbEvent.Text = "Spring";
                        break;
                    case "9":
                        cbEvent.Text = "White Day";
                        break;
                    case "10":
                        cbEvent.Text = "Wedding";
                        break;
                    case "11":
                        cbEvent.Text = "Fall";
                        break;
                    case "12":
                        cbEvent.Text = "Casino music with flag";
                        break;
                    case "13":
                        cbEvent.Text = "Spring with flag";
                        break;
                    case "14":
                        cbEvent.Text = "Casino music only";
                        break;
                    default:
                        cbEvent.Text = "None";
                        break;
                }

                // Box drop rate settings
                nmWepDrop.Value = decimal.Parse(shipSettings[7]) / 1000;
                nmArmorDrop.Value = decimal.Parse(shipSettings[8]) / 1000;
                nmMagDrop.Value = decimal.Parse(shipSettings[9]) / 1000;
                nmToolDrop.Value = decimal.Parse(shipSettings[10]) / 1000;
                nmMesetaDrop.Value = decimal.Parse(shipSettings[11]) / 1000;

                // Exp multiplier
                nmExpRate.Value = decimal.Parse(shipSettings[12]) * 100;

                // NIGHTs skin toggle
                if (shipSettings[13] == "1")
                    chkNights.Checked = true;
                else chkNights.Checked = false;
            }

            // Read and parse localgms.ini, then update the GM listbox and rights level slider appropriately.
            try
            {
                using (FileStream fs = File.Open("localgms.ini", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (StreamReader reader = new StreamReader(fs))
                    {
                        string tempLine = String.Empty;

                        while (!reader.EndOfStream && tempLine != "[gmrights]")
                        {
                            tempLine = reader.ReadLine();

                            try
                            {
                                if (tempLine.Substring(0, 1) != "#" && tempLine.Substring(0, 1) != "[")
                                {
                                    string[] GM_info_temp = new string[2];
                                    GM_info_temp = tempLine.Split(',');
                                    lstGM.Items.Add(GM_info_temp[0]);
                                    GM_info.Add(GM_info_temp[0], GM_info_temp[1]);
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                        while (!reader.EndOfStream)
                        {
                            tempLine = reader.ReadLine();

                            try
                            {
                                if (tempLine.Substring(0, 1) != "#")
                                {
                                    string[] rights_info_temp = new string[2];
                                    rights_info_temp = tempLine.Split(',');
                                    rights_info.Add(rights_info_temp[0], rights_info_temp[1]);
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        for (int index = 0; index < GM_info.Count; index++)
                        {
                            var current = GM_info.ElementAt(index);

                            for (int index2 = 0; index2 < rights_info.Count; index2++)
                            {
                                var current_rightsinfo = rights_info.ElementAt(index2);
                                if (current.Value == current_rightsinfo.Key)
                                    GM_info[current.Key] = current_rightsinfo.Value;
                            }
                        }
                    }
                }
            }
            catch // localgms.ini not found; we can just return here, no need to do anything else.
            {
                return;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAuto.Checked)
                txtIP.ReadOnly = true; // If "Auto" is checked, there's no need to edit this!
            else txtIP.ReadOnly = false;
        }

        // "Save" button handler. Write back brief comment descriptions as well as our data, to keep the file clean in case of manual editing later.
        private void button1_Click(object sender, EventArgs e)
        {
            using (FileStream fs = File.Open("ship.ini", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter fileData = new StreamWriter(fs))
                {
                    fileData.WriteLine("# Your IP address. Use 'auto' to let the login server detect this automatically.");
                    if (chkAuto.Checked == true)
                        fileData.WriteLine("auto");
                    else fileData.WriteLine(txtIP.Text);

                    fileData.WriteLine("# The ship's main port. Additional ports will be used depending on the number of blocks.");
                    fileData.WriteLine(nmPort.Value);
                    fileData.WriteLine("# The number of blocks to set for this ship.");
                    fileData.WriteLine(nmBlocks.Value);
                    fileData.WriteLine("# The maximum number of users allowed on this ship.");
                    fileData.WriteLine(nmMaxUsers.Value);
                    fileData.WriteLine("# The IP address of the login server.");
                    fileData.WriteLine(txtLoginIP.Text);
                    fileData.WriteLine("# The name of the ship.");
                    fileData.WriteLine(txtShipName.Text);
                    fileData.WriteLine("# The ship event to use.");
                    if (cbEvent.SelectedIndex == 0 || cbEvent.SelectedIndex == 1)
                        fileData.WriteLine(cbEvent.SelectedIndex);
                    else fileData.WriteLine(cbEvent.SelectedIndex + 1);
                    fileData.WriteLine("# The box drop rate for weapons.");
                    fileData.WriteLine((int)(nmWepDrop.Value * 1000));
                    fileData.WriteLine("# The box drop rate for armor and shields.");
                    fileData.WriteLine((int)(nmArmorDrop.Value * 1000));
                    fileData.WriteLine("# The box drop rate for mags.");
                    fileData.WriteLine((int)(nmMagDrop.Value * 1000));
                    fileData.WriteLine("# The box drop rate for tool items.");
                    fileData.WriteLine((int)(nmToolDrop.Value * 1000));
                    fileData.WriteLine("# The box drop rate for meseta.");
                    fileData.WriteLine((int)(nmMesetaDrop.Value * 1000));
                    fileData.WriteLine("# The experience modifier.");
                    fileData.WriteLine((int)(nmExpRate.Value / 100));

                    fileData.WriteLine("# NIGHTs skin support (1 for on, 0 for off.)");
                    if (chkNights.Checked == true)
                        fileData.WriteLine("1");
                    else fileData.WriteLine("0");
                }
            }
            MessageBox.Show("Ship settings saved.", "Ship Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Minimize to system tray.
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                Hide();
                notifyIcon1.Visible = true;
            }
        }
        // Unhide the window if the tray icon is double-clicked.
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }

        // Start the ship (if it exists!)
        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                Process ship_process = new Process();
                ship_process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                ship_process.StartInfo.UseShellExecute = true;
                ship_process.StartInfo.FileName = "ship_server.exe";
                ship_process.Start();
                lnkDown.Hide();
                lnkRunning.Show();
            }
            catch
            {
                MessageBox.Show("The file 'ship_server.exe' was not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
   
        private void lstGM_SelectedIndexChanged(object sender, EventArgs e)
        { 
            // Hide all labels and reset the slider/rights level display.
            lblEvent.Hide();
            lblWarpme.Hide();
            lblDc.Hide();
            lblDcall.Hide();
            lblAnnounce.Hide();
            lblLevelup.Hide();
            lblUpdatelocalgms.Hide();
            lblStfu.Hide();
            lblWarpall.Hide();
            lblBan.Hide();
            lblIpban.Hide();
            lblHwban.Hide();
            lblUpdatemasks.Hide();

            trkRights.Value = 0;
            lblCommands.Text = "Allowed commands: Level " + trkRights.Value;

            if (lstGM.SelectedItem == null) return;
            string permission_value = GM_info[lstGM.SelectedItem.ToString()];
            int current_value = int.Parse(permission_value);

            /* Subtract from the rights value until it's 0, in order to find and display the rights level the selected GM has. */

            current_value -= 1;
            if (current_value >= 0)
            {
                lblEvent.Show();
                trkRights.Value = 1;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblEvent.Hide();
                return;
            }

            current_value -= 8;
            if (current_value >= 0)
            {
                lblWarpme.Show();
                trkRights.Value = 2;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblWarpme.Hide();
                return;
            }

            current_value -= 16;
            if (current_value >= 0)
            {
                lblDc.Show();
                trkRights.Value = 3;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblDc.Hide();
                return;
            }

            current_value -= 32;
            if (current_value >= 0)
            {
                lblDcall.Show();
                trkRights.Value = 4;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblDcall.Hide();
                return;
            }

            current_value -= 64;
            if (current_value >= 0)
            {
                lblAnnounce.Show();
                trkRights.Value = 5;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblAnnounce.Hide();
                return;
            }

            current_value -= 128;
            if (current_value >= 0)
            {
                lblLevelup.Show();
                trkRights.Value = 6;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblLevelup.Hide();
                return;
            }

            current_value -= 256;
            if (current_value >= 0)
            {
                lblUpdatelocalgms.Show();
                trkRights.Value = 7;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblUpdatelocalgms.Hide();
                return;
            }

            current_value -= 512;
            if (current_value >= 0)
            {
                lblStfu.Show();
                trkRights.Value = 8;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblStfu.Hide();
                return;
            }

            current_value -= 1024;
            if (current_value >= 0)
            {
                lblWarpall.Show();
                trkRights.Value = 9;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblWarpall.Hide();
                return;
            }

            current_value -= 2048;
            if (current_value >= 0)
            {
                lblBan.Show();
                trkRights.Value = 10;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblBan.Hide();
                return;
            }

            current_value -= 4096;
            if (current_value >= 0)
            {
                lblIpban.Show();
                trkRights.Value = 11;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblIpban.Hide();
                return;
            }

            current_value -= 4096;
            if (current_value >= 0)
            {
                lblHwban.Show();
                trkRights.Value = 12;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblHwban.Hide();
                return;
            }

            current_value -= 4096;
            if (current_value >= 0)
            {
                lblUpdatemasks.Show();
                trkRights.Value = 13;
                lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            }
            else
            {
                lblUpdatemasks.Hide();
                return;
            }
        }

        // Remove GM button handler.
        private void button2_Click(object sender, EventArgs e)
        {
            if (lstGM.SelectedItem == null) return;
            GM_info.Remove(lstGM.SelectedItem.ToString());
            lstGM.Items.Remove(lstGM.SelectedItem);
            lstGM_SelectedIndexChanged(sender, e);
        }

        // Add GM button handler.
        private void button3_Click(object sender, EventArgs e)
        {
            AddGM addGM = new AddGM();
            addGM.ShowDialog(this);
        }

        // Edit GM button handler.
        private void button1_Click_1(object sender, EventArgs e)
        {
            if (lstGM.SelectedItem != null)
            {
                EditGM editGM = new EditGM();
                editGM.ShowDialog(this);
            }
        }
    
        // The user changed a GM's rights level, so store it for when we need to save.
        private void trkRights_Scroll(object sender, EventArgs e)
        {
            lblCommands.Text = "Allowed commands: Level " + trkRights.Value;
            if (lstGM.SelectedItem == null) return;
            string gcNum = lstGM.SelectedItem.ToString();

            switch (trkRights.Value)
            {
                case 0:
                    GM_info[gcNum] = "0";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 1:
                    GM_info[gcNum] = "1";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 2:
                    GM_info[gcNum] = "9";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 3:
                    GM_info[gcNum] = "25";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 4:
                    GM_info[gcNum] = "57";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 5:
                    GM_info[gcNum] = "121";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 6:
                    GM_info[gcNum] = "249";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 7:
                    GM_info[gcNum] = "505";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 8:
                    GM_info[gcNum] = "1017";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 9:
                    GM_info[gcNum] = "2041";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 10:
                    GM_info[gcNum] = "4089";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 11:
                    GM_info[gcNum] = "8185";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 12:
                    GM_info[gcNum] = "12281";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
                case 13:
                    GM_info[gcNum] = "16377";
                    lstGM_SelectedIndexChanged(sender, e);
                    break;
            }
        }

        // "Save" button handler for GM settings. Write back comment descriptions/explanations here as well, for manual editing!
        private void button4_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> GM_info_new = new Dictionary<string, string>(GM_info);
            Dictionary<string, string> rights_info_new = new Dictionary<string, string>();
            bool found = false;
            int rights_idx = 0;

            for (int index = 0; index < GM_info_new.Count; index++) // Iterate through GC#s.
            {
                var current = GM_info_new.ElementAt(index);
                found = false;

                for (int index2 = 0; index2 < rights_info_new.Count; index2++) // Check each GC#'s rights level with our new rights key list.
                {
                    var current_rightsinfo = rights_info_new.ElementAt(index2);
                    if (current.Value == current_rightsinfo.Value) // Found! Write the appropriate key to our GC list.
                    {
                        GM_info_new[current.Key] = current_rightsinfo.Key;
                        found = true;
                    }
                }

                if (!found)
                {
                    rights_info_new.Add(rights_idx.ToString(), current.Value);
                    GM_info_new[current.Key] = rights_idx.ToString();
                    rights_idx++;
                }
            }

            using (FileStream fs = File.Open("localgms.ini", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter fileData = new StreamWriter(fs))
                {
                    fileData.WriteLine("# The local GM list for this ship. Use the format: gc#,rights level (explained below.)");
                    fileData.WriteLine("[localgms]");
                    for (int index = 0; index < GM_info_new.Count; index++)
                    {
                        var current = GM_info_new.ElementAt(index);
                        fileData.WriteLine(current.Key + "," + current.Value);
                    }

                    fileData.WriteLine("# Rights key; use the following format: rights level,rights key");
                    fileData.WriteLine("# The rights key is calculated by adding the values for all of the commands you want a GM with that");
                    fileData.WriteLine("# rights level to have access to:");
                    fileData.WriteLine("# /event          = 1");
                    fileData.WriteLine("# /warpme         = 8");
                    fileData.WriteLine("# /dc             = 16");
                    fileData.WriteLine("# /dcall          = 32");
                    fileData.WriteLine("# /annouce        = 64");
                    fileData.WriteLine("# /levelup        = 128");
                    fileData.WriteLine("# /updatelocalgms = 256");
                    fileData.WriteLine("# /stfu	       	  = 512");
                    fileData.WriteLine("# /warpall        = 1024");
                    fileData.WriteLine("# /ban            = 2048");
                    fileData.WriteLine("# /ipban          = 4096");
                    fileData.WriteLine("# /hwban          = 4096");
                    fileData.WriteLine("# /updatemasks    = 4096");
                    fileData.WriteLine("[gmrights]");
                    for (int index2 = 0; index2 < rights_info_new.Count; index2++)
                    {
                        var current_rightsinfo = rights_info_new.ElementAt(index2);
                        fileData.WriteLine(current_rightsinfo.Key + "," + current_rightsinfo.Value);
                    }
                }
            }
            MessageBox.Show("GM settings saved.", "GM Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
      
        // Ensure that the ship process closes along with our form!
        private void stop_ship_process()
        {
            string FullPath = @System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\ship_server.exe";
            string FilePath = Path.GetDirectoryName(FullPath);
            string FileName = Path.GetFileNameWithoutExtension(FullPath).ToLower();
            Process[] pList = Process.GetProcessesByName(FileName);

            foreach (Process p in pList)
            {
                // A try-catch statement is needed here to catch exceptions due to weird Windows behavior, in case a program is closed while reading.
                try
                {
                    if (p.MainModule.FileName.StartsWith(FilePath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        chkRestart.Checked = false;
                        p.Kill();
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
        }
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            int temp;

            if (chkRestart.Checked == true) temp = 1;
            else temp = 0;

            Hide();
            stop_ship_process();

            write_restart_flag(temp);
        }

        // Also close the ship process if the "Running" label is clicked.
        private void lnkStarted_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            stop_ship_process();
        }

        // Write the user's preference for "Restart Automatically" any time the checkbox is changed.
        private void chkRestart_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRestart.Checked == true) write_restart_flag(1);
            else write_restart_flag(0);
        }
    }
}
