using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TSendKeys = System.Windows.Forms.SendKeys;
using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace BotDiscord
{
    public partial class MainWindow : Window
    {
        DispatcherTimer dispatcherTimer;
        string version = "1.1";

        public class Comando
        {
            public int rowid { get; set; }
            public string mensagem { get; set; }
            public int tempo { get; set; }
            public int contador { get; set; }
            public bool ativo { get; set; }
        }

        List<Comando> listaComandos;

        public MainWindow()
        {
            if(!newUpdate())
                InitializeComponent();
            else 
                Process.Start($"{Directory.GetCurrentDirectory()}\\UpdateExe.exe");        
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            listaComandos = new List<Comando>();

            LoadData();

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            dispatcherTimer.Start();
        }

        public bool newUpdate()
        {
            bool newUpdate = false;
            WebRequest request = WebRequest.Create("");
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            try
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);

                    string responseData = reader.ReadToEnd();

                    if (responseData != version)
                        newUpdate = true;

                    reader.Close();
                    dataStream.Close();
                    response.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                newUpdate = false;
            }

            return newUpdate;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var novoComando = new Comando()
            {
                rowid = 0,
                mensagem = "",
                tempo = 0,
                contador = 0,
                ativo = false
            };
            listaComandos.Add(novoComando);
            AdicionarControlos(novoComando);
        }

        private void btnStartStop_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            string[] splitName = btn.Name.Split('_');
            int indexBtn = Convert.ToInt32(splitName[1]);           

            var comando = listaComandos[indexBtn];
            if(!comando.ativo)
            {
                var txtComando = (TextBox)procurarControlosGrid($"txtComando_{indexBtn}");
                var txtTempo = (TextBox)procurarControlosGrid($"txtTempo_{indexBtn}");

                if (validarInputs(txtComando.Text, txtTempo.Text))
                {
                    comando.mensagem = txtComando.Text;
                    comando.tempo = Convert.ToInt32(txtTempo.Text);
                    SaveData(comando.rowid, comando.mensagem, comando.tempo);

                    comando.ativo = true;
                    btn.Content = "Parar";
                }
            }
            else
            {
                comando.ativo = false;
                comando.contador = comando.tempo;
                btn.Content = "Iniciar";
            }
            enableControls(indexBtn, comando.ativo);
        }
    
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            for (int index = 0; index < listaComandos.Count(); index++)
            {
                if(listaComandos[index].ativo)
                {
                    if(listaComandos[index].contador > 0)
                    {
                        var lblComando = (Label)procurarControlosGrid($"lblComando_{index}");
                        listaComandos[index].contador--;
                        lblComando.Content = $"Comando - {listaComandos[index].contador}s";
                    }
                    else
                    {
                        listaComandos[index].contador = listaComandos[index].tempo;
                        TSendKeys.SendWait(listaComandos[index].mensagem);
                        TSendKeys.SendWait("{ENTER}");
                    }
                }
            }
        }    

        private bool validarInputs(string comando, string tempo)
        {
            int inteiro;
            if (comando == "")
            {
                MessageBox.Show("Maluco, não vais estar a mandar comandos em branco né", "que nabo kkkk");
            }
            else if (tempo == "")
            {
                MessageBox.Show("Oh meu, se não escreveres um tempo como queres mandar mensagens até Setúbal?", "Errou parça");
            }
            else if (!int.TryParse(tempo, out inteiro))
            {
                MessageBox.Show("Epá, que eu saiba o tempo tem que ser um número kkk", "Deu erro zé");
            }
            else
            {
                return true;
            }
            return false;
        }

        private void LoadData()
        {
            using (var connection = new SqliteConnection("Data Source=comando.db"))
            {
                connection.Open();

                var command = connection.CreateCommand();
                //command.CommandText = "CREATE TABLE comando (comando VARCHAR(500), tempo INT)";
                //command.ExecuteNonQuery();
                command.CommandText = @"SELECT rowid, comando, tempo FROM comando";


                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var rowid = reader.GetString(0);
                        var textComando  = reader.GetString(1);
                        var tempo = reader.GetString(2);

                        var comando = new Comando()
                        {
                            rowid = Convert.ToInt32(rowid),
                            mensagem = textComando,
                            tempo = Convert.ToInt32(tempo),
                            contador = Convert.ToInt32(tempo),
                            ativo = false
                        };

                        listaComandos.Add(comando);

                        AdicionarControlos(comando);
                    }
                }
            }
        }

        private void SaveData(int rowid, string comando, int tempo)
        {
            using (var connection = new SqliteConnection("Data Source=comando.db"))
            {
                connection.Open();
                var command = connection.CreateCommand();

                var query = "";

                if(rowid == 0)
                {
                    query = @"INSERT INTO comando VALUES ($comando, $tempo)";
                }
                else
                {
                    query = @"UPDATE comando SET comando = $comando, tempo = $tempo WHERE rowid = $rowid";
                    command.Parameters.AddWithValue("$rowid", rowid);
                }
                
                command.CommandText = query;
                command.Parameters.AddWithValue("$comando", comando);
                command.Parameters.AddWithValue("$tempo", tempo);
                command.ExecuteNonQuery();

                if(rowid == 0)
                {
                    command.CommandText = "SELECT last_insert_rowid()";
                    Console.WriteLine(command.ExecuteScalar());
                }
            }
        }

        private void enableControls(int index, bool enable)
        {
            TextBox txtComando = (TextBox)procurarControlosGrid($"txtComando_{index}");
            TextBox txtTempo = (TextBox)procurarControlosGrid($"txtTempo_{index}");

            txtComando.IsEnabled = !enable;
            txtTempo.IsEnabled = !enable;
        }

        private void AdicionarControlos(Comando comando)
        {
            int tamanhoLista = listaComandos.Count();

            grd.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(25) });

            Label lblComando = new Label();
            lblComando.Content = "Comando";
            lblComando.Name = $"lblComando_{tamanhoLista - 1}";

            Grid.SetRow(lblComando, grd.RowDefinitions.Count() - 1);
            Grid.SetColumn(lblComando, 0);

            grd.Children.Add(lblComando);

            Label lblTempo = new Label();
            lblTempo.Content = "Tempo (s)";

            Grid.SetRow(lblTempo, grd.RowDefinitions.Count() - 1);
            Grid.SetColumn(lblTempo, 1);

            grd.Children.Add(lblTempo);

            grd.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(25) });

            TextBox txtComando = new TextBox();
            txtComando.Name = $"txtComando_{tamanhoLista -1}";
            txtComando.Text = comando.mensagem;
            txtComando.VerticalAlignment = VerticalAlignment.Center;
            txtComando.Height = 20;

            Grid.SetRow(txtComando, grd.RowDefinitions.Count() - 1);
            Grid.SetColumn(txtComando, 0);

            grd.Children.Add(txtComando);

            TextBox txtTempo = new TextBox();
            txtTempo.Name = $"txtTempo_{tamanhoLista -1}";
            txtTempo.Width = 55;
            txtTempo.Height = 20;
            txtTempo.Text = comando.tempo.ToString();
            txtTempo.VerticalAlignment = VerticalAlignment.Center;


            Grid.SetRow(txtTempo, grd.RowDefinitions.Count() - 1);
            Grid.SetColumn(txtTempo, 1);

            grd.Children.Add(txtTempo);

            Button btnIniciar = new Button();
            btnIniciar.Name = $"btnStartStop_{tamanhoLista -1}";
            btnIniciar.Content = "Iniciar";
            btnIniciar.Click += btnStartStop_Click;

            Grid.SetRow(btnIniciar, grd.RowDefinitions.Count() - 1);
            Grid.SetColumn(btnIniciar, 2);

            grd.Children.Add(btnIniciar);
        }

        private UIElement procurarControlosGrid(string nomeControlo)
        {
            var childrens = grd.Children;
            UIElement controlo = null;

            for (int index = 0; index < childrens.Count; index++)
            {
                if(childrens[index].GetType() == typeof(Label))
                {
                    var lbl = (Label)childrens[index];
                    if (nomeControlo == lbl.Name)
                    {
                        controlo = childrens[index];
                        index = childrens.Count;
                    }
                }
                else if(childrens[index].GetType() == typeof(TextBox))
                {
                    var txt = (TextBox)childrens[index];
                    if (nomeControlo == txt.Name)
                    {
                        controlo = childrens[index];
                        index = childrens.Count;
                    }
                }
            }
            return controlo;
        }
    }
}
