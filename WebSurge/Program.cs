﻿using System;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace WebSurge
{
    public static class Program
    {
        
        public static StressTestForm WebSurgeForm { get; set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string fileName = null;
            if (args != null && args.Length > 0)
                fileName = args[0];

            // Force config to apply im
            var obj = App.UserDataPath;

            var limit = ServicePointManager.DefaultConnectionLimit;
            if (ServicePointManager.DefaultConnectionLimit < 5)
                ServicePointManager.DefaultConnectionLimit = 50;

            //ServicePointManager.MaxServicePoints = 50;
            //ServicePointManager.MaxServicePointIdleTime = 100;

            // setting using config file switch
            //ServicePointManager.Expect100Continue = false;

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            if (App.Configuration.StressTester.IgnoreCertificateErrors)
            {
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            
            WebSurgeForm = new StressTestForm(fileName);            
            

            Application.ThreadException += Application_ThreadException;
            try
            {
                Application.Run(WebSurgeForm);
                WebSurgeForm = null;
            }
            catch (Exception ex)
            {
                Application_ThreadException(null, new ThreadExceptionEventArgs(ex));
            } 
        }

        
        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            var ex = e.Exception;
            App.Log(ex);

            var msg = string.Format("Yikes! Something went wrong...\r\n\r\n{0}\r\n" +
                "The error has been recorded and written to a log file and you can\r\n" +
                "review the details or report the error via Help | Show Error Log\r\n\r\n" +
                "Do you want to continue?",ex.Message);

            DialogResult res = MessageBox.Show(msg,App.Configuration.AppName + " Error",
                                                MessageBoxButtons.YesNo,MessageBoxIcon.Error);
            if (res == DialogResult.No)
                Application.Exit();
        } 
    }
}
