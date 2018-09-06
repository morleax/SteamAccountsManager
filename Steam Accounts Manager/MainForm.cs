using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;

namespace Steam_Accounts_Manager
{
    public partial class MainForm : Form
    {
        private MenuStrip Menu;
        private ListBox AccountsList;
        private ToolStripItem NewAccountBtn;
        private ToolStripItem RemoveAccountBtn;
        private Dictionary<string, string> AccountsData = new Dictionary<string, string>();

        public MainForm()
        {
            if (!File.Exists(SteamConfigPath))
            {
                MessageBox.Show("Unable to find Steam config file.");

                return;
            }

            InitializeComponent();

            InitializeMenuStrip();
            InitializeList();

            this.Resize += OnResize;
            this.Size = new Size(270, 370);
        }

        private void OnResize(object sender, EventArgs e)
        {
            AccountsList.Size = new Size(ClientRectangle.Width, ClientRectangle.Height - Menu.Size.Height);
        }

        private void LoadAccounts()
        {
            string config = File.ReadAllText(SteamConfigPath);

            var SteamIds = new Regex("\"\\d{17}\"").Matches(config);

            var AccountNames = new Regex("\"AccountName\"(\\s+)?\"(.+?)\"").Matches(config);
            var PersonaNames = new Regex("\"PersonaName\"(\\s+)?\"(.+?)\"").Matches(config);

            for (int k = 0; k < AccountNames.Count; k++)
            {
                string login = AccountNames[k].Groups[2].Value;

                AccountsList.Items.Add(login);
                AccountsData.Add(login, SteamIds[k].Value);

                if (AutoLoginUser == login)
                {
                    AccountsList.SelectedIndex = k;
                }
            }
        }

        private void InitializeMenuStrip()
        {
            Menu = new MenuStrip();

            NewAccountBtn = Menu.Items.Add("New");
            NewAccountBtn.Click += NewAccount;

            RemoveAccountBtn = Menu.Items.Add("Remove");
            RemoveAccountBtn.Enabled = false;
            RemoveAccountBtn.Click += RemoveAccount;

            this.Controls.Add(Menu);
        }

        private void InitializeList()
        {
            AccountsList = new ListBox
            {
                Margin = new Padding(0, 0, 0, 0),
                ItemHeight = 20,
                Location = new Point(0, Menu.Location.Y + Menu.Size.Height),
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 204),
            };

            AccountsList.DrawItem += OnDrawAccountsList;
            AccountsList.MouseDoubleClick += LoadSpecifiedAccount;
            AccountsList.SelectedIndexChanged += UpdateRemoveAccountBtnState;

            this.Controls.Add(AccountsList);

            LoadAccounts();
        }

        private void RemoveAccount(object sender, EventArgs e)
        {
            if (AccountsList.SelectedIndex != -1)
            {
                string config = File.ReadAllText(SteamConfigPath);
                string login = AccountsList.Items[AccountsList.SelectedIndex].ToString();
                string steam_id = AccountsData[login];
                var match = new Regex(steam_id + "\\s+?{(.+?)\\s+?}", RegexOptions.Singleline).Match(config);
                File.WriteAllText(SteamConfigPath, config.Replace(match.Value, ""));

                AccountsList.Items.RemoveAt(AccountsList.SelectedIndex);
            }
        }

        private void NewAccount(object sender, EventArgs e)
        {
            SetAccount("");
        }

        private void UpdateRemoveAccountBtnState(object sender, EventArgs e)
        {
            RemoveAccountBtn.Enabled = (sender as ListBox).SelectedIndex != -1;
        }

        private void OnDrawAccountsList(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }

            ListBox listBox = sender as ListBox;

            e.DrawBackground();
            Brush myBrush = Brushes.Black;

            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                myBrush = Brushes.White;
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(20, 20, 20)), e.Bounds);
            }
            else
            {
                e.Graphics.FillRectangle(Brushes.White, e.Bounds);
            }

            e.Graphics.DrawString(listBox.Items[e.Index].ToString(), e.Font, myBrush, e.Bounds);
        }

        private static void KillSteamProcesses()
        {
            List<string> processNames = new List<string>() {
                "Steam",
                "SteamService",
                "steamwebhelper",
            };

            foreach (string processName in processNames)
            {
                foreach (Process proc in Process.GetProcessesByName(processName))
                {
                    proc.Kill();
                }
            }
        }

        private void SetAccount(string login)
        {
            KillSteamProcesses();

            AutoLoginUser = login;

            Process.Start(SteamKey.GetValue("SteamExe").ToString());
        }

        private void LoadSpecifiedAccount(object sender, MouseEventArgs e)
        {
            ListBox listBox = sender as ListBox;

            if (listBox.SelectedIndex == -1)
            {
                return;
            }

            SetAccount(listBox.SelectedItem.ToString());
        }

        private string AutoLoginUser
        {
            get
            {
                return SteamKey.GetValue("AutoLoginUser").ToString();
            }
            set
            {
                SteamKey.SetValue("AutoLoginUser", value);
            }
        }

        private string SteamConfigPath
        {
            get
            {
                string SteamPath = SteamKey.GetValue("SteamPath").ToString();

                return SteamPath + "/config/loginusers.vdf";
            }
        }

        private RegistryKey SteamKey
        {
            get
            {
                return Registry.CurrentUser.OpenSubKey("SOFTWARE", true).OpenSubKey("Valve", true).OpenSubKey("Steam", true);
            }
        }
    }
}
