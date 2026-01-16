using System;
using System.Windows.Forms;
using WinFormsApp3.Data;

namespace WinFormsApp3
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DBHelper dbHelper = new DBHelper();


            // ================================================================
            // DEBUG
            // ================================================================


            //dbHelper.DeleteToken();
            //AuthManager tempAuth = new AuthManager(null);
            //tempAuth.ClearBrowserCacheOnDisk();

            // ================================================================
            // DEBUG
            // ================================================================

            string token = dbHelper.GetToken();
            bool loginSuccessful = false;

            if (string.IsNullOrEmpty(token))
            {
                // --- Kein Token -> Login Fenster zeigen ---

                // using stellt sicher, dass FrmLogin danach komplett aus dem RAM fliegt
                using (FrmLogin loginForm = new FrmLogin())
                {
                   
                    if (loginForm.ShowDialog() == DialogResult.OK)
                    {

                        token = loginForm.FetchedToken;

                        dbHelper.SaveToken(token);

                        loginSuccessful = true;
                    }
                    else
                    {
       
                        return; 
                    }
                }
            }
            else
            {
                // --- Token in  DB ---
                loginSuccessful = true;
            }


            if (loginSuccessful && !string.IsNullOrEmpty(token))
            {
                Application.Run(new Form1(token));
                
            }
        }
    }
}