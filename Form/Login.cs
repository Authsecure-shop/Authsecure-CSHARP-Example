using Loader;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;

namespace AuthSecure
{
    public partial class Login : Form
    {

        public static api AuthSecureApp = new api
        (
            name: "", // App name
            ownerid: "", // Account ID
            secret: "", // App secret
            version: "" // Application version
        );



        public Login()
        {
            InitializeComponent();
            Drag.MakeDraggable(this);
            AuthSecureApp.InitApiAsync();
        }

        private async void loginBtn_Click_1(object sender, EventArgs e)
        {
            await AuthSecureApp.LoginAsync(usernameField.Text, passwordField.Text);
            if (AuthSecureApp.response.success)
            {

                Main main = new Main();
                main.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Status: " + AuthSecureApp.response.message);
            }
        }
        private async void registerBtn_Click(object sender, EventArgs e)
        {
            string email = this.emailField.Text;
            if (email == "Email (leave blank if none)")
            {   // default value
                email = null;
            }

            await AuthSecureApp.RegisterAsync(usernameField.Text, passwordField.Text, keyField.Text, email);
            if (AuthSecureApp.response.success)
            {
                Main main = new Main();
                main.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Status: " + AuthSecureApp.response.message);
            }
        }

        private void closeBtn_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void minBtn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        
    }
}
