using System;
using System.Diagnostics;
using System.IO;
using System.Net;

using System.Windows;


namespace UpdateExe
{
    public partial class MainWindow : Window
    {   
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var processos = Process.GetProcessesByName("BotDiscord");
                if(processos != null)
                {
                    processos[0].Kill();
                    processos[0].WaitForExit();
                    update();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        bool opened = false;
        private void update()
        {
            using (var webClient = new WebClient())
            {
                try
                {
                    webClient.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                    {
                        progressBar.Value = e.ProgressPercentage;
                        if(e.ProgressPercentage == 100 && !opened)
                        {
                            opened = true;
                            Process.Start($"{Directory.GetCurrentDirectory()}\\BotDiscord.exe");
                            Application.Current.Shutdown();
                        }
                    };
                    webClient.DownloadFileAsync(new Uri(""), $"{Directory.GetCurrentDirectory()}\\BotDiscord.exe");
                }
                catch(WebException ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

    }
}
