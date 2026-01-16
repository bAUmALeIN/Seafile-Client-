using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    public partial class FrmLogin : Form
    {
        private AuthManager _authManager;
        private System.Windows.Forms.Timer _loginCheckTimer;


        public string FetchedToken { get; private set; }

        public FrmLogin()
        {
            InitializeComponent();

            _authManager = new AuthManager(webView21);
            _authManager.SetupWebViewEnvironment();
        }

        private async void FrmLogin_Load(object sender, EventArgs e)
        {
            await StartLoginFlow();
        }

        private async Task StartLoginFlow()
        {
            await _authManager.InitializeAsync();
            _authManager.LoadLoginPage();

            _loginCheckTimer = new System.Windows.Forms.Timer();
            _loginCheckTimer.Interval = 2000;
            _loginCheckTimer.Tick += LoginCheckTimer_Tick;
            _loginCheckTimer.Start();
        }

        private async void LoginCheckTimer_Tick(object sender, EventArgs e)
        {
            string token = await _authManager.TryExtractTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                _loginCheckTimer.Stop();
                FetchedToken = token;

                this.DialogResult = DialogResult.OK;

                this.Close();
            }
        }
    }
}