using Loader;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

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
        }

        private async void Main_Load(object sender, EventArgs e)
        {
            userDataField.Items.Add($"Username: {Login.AuthSecureApp.user_data.username}");
            userDataField.Items.Add($"License: {Login.AuthSecureApp.user_data.subscriptions[0].key}"); // this can be used if the user used a license, username, and password for register. It'll display the license assigned to the user
            userDataField.Items.Add($"Expires: {Login.AuthSecureApp.user_data.subscriptions[0].expiration}"); // this has been changed from expiry to expiration
            userDataField.Items.Add($"Subscription: {Login.AuthSecureApp.user_data.subscriptions[0].subscription}");
            userDataField.Items.Add($"IP: {Login.AuthSecureApp.user_data.ip}");
            userDataField.Items.Add($"HWID: {Login.AuthSecureApp.user_data.hwid}");
            userDataField.Items.Add($"Creation Date: {Login.AuthSecureApp.user_data.CreationDate}"); // this has a capital "C" , if you use a lowercase "c" it won't convert unix
            userDataField.Items.Add($"Last Login: {Login.AuthSecureApp.user_data.LastLoginDate}"); // this has a capital "L", if you use a lowercase "l" it won't convert unix
            userDataField.Items.Add($"Time Left: {Login.AuthSecureApp.expirydaysleft()}");

           
        }

        private async void closeBtn_Click(object sender, EventArgs e)
        {
           // await Login.AuthSecureApp.logout(); 
            Environment.Exit(0);
        }

        private void minBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Maximized;
        }

        
    }
}