using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ZebraPrinterGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Username { get; set; }
        public string Password { get; set; }
        
        public delegate void ConnectToServer(
            Dispatcher dispatcher,
            string username,
            string password,
            Label serverHelperText,
            Label usernameHelperText,
            Label passwordHelperText
            );

        public ConnectToServer _connectToServer;

        
        public MainWindow(ConnectToServer connectToServer, string user, string serverAddress, string serverStatus)
        {
            
            _connectToServer = connectToServer;
            InitializeComponent();

            server.Text = serverAddress;
            username.Text = user;
            serverHelperText.Content = serverStatus;
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Username = username.Text;
            Password = password.Password;
            serverHelperText.Content = null;
            usernameHelperText.Content = null;
            passwordHelperText.Content = null;
            _connectToServer(Dispatcher, Username, Password, serverHelperText, usernameHelperText, passwordHelperText);            
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Dispatcher.InvokeShutdown();
        }
    }
}
