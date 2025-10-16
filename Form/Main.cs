using Loader;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace AuthSecure
{
    public partial class Main : Form
    {
        /*
        * 
        * WATCH THIS VIDEO TO SETUP APPLICATION: https://www.youtube.com
        * 
	     * READ HERE TO LEARN ABOUT AuthSecure FUNCTIONS https://github.com
		 *
        */

     
        public Main()
        {
            InitializeComponent();
            Drag.MakeDraggable(this);
           Login.AuthSecureApp.Init();
        }


        private async void Main_Load(object sender, EventArgs e)
        {
            userDataField.Items.Add($"Username: {Login.AuthSecureApp.user_data.username}");
            userDataField.Items.Add($"License: {Login.AuthSecureApp.user_data.subscriptions[0].key}"); // this can be used if the user used a license, username, and password for register. It'll display the license assigned to the user                                                                                                      
            userDataField.Items.Add($"Expires: {Login.AuthSecureApp.user_data.subscriptions[0].ExpirationDate}"); // Directly DateTime dikhane ke liye
            userDataField.Items.Add($"Subscription: {Login.AuthSecureApp.user_data.subscriptions[0].subscription}");
            userDataField.Items.Add($"IP: {Login.AuthSecureApp.user_data.ip}");
            userDataField.Items.Add($"HWID: {Login.AuthSecureApp.user_data.hwid}");
            userDataField.Items.Add($"Creation Date: {Login.AuthSecureApp.user_data.CreationDate}"); // this has a capital "C" , if you use a lowercase "c" it won't convert unix
            userDataField.Items.Add($"Last Login: {Login.AuthSecureApp.user_data.LastLoginDate}"); // this has a capital "L", if you use a lowercase "l" it won't convert unix
            userDataField.Items.Add($"Time Left: {Login.AuthSecureApp.expirydaysleft()}");
        }

        private async void closeBtn_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void minBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }

    
        private async void fetchGlobalVariableBtn_Click(object sender, EventArgs e)
        {
            string globalVal = await Login.AuthSecureApp.var(globalVariableField.Text);
            MessageBox.Show(globalVal);
            MessageBox.Show(Login.AuthSecureApp.response.message); // API response
        }

        
        private async void fetchUserVarBtn_Click(object sender, EventArgs e)
        {
            await Login.AuthSecureApp.getvar(varField.Text);
            MessageBox.Show(Login.AuthSecureApp.response_var.response.variable_data); // API response

        }

        private async void setUserVarBtn_Click(object sender, EventArgs e)
        {

            await Login.AuthSecureApp.setvar(varField.Text, varDataField.Text);
            MessageBox.Show(Login.AuthSecureApp.response_var.message); // API response

        }
    }
}