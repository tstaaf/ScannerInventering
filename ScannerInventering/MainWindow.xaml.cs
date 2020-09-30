using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Text;
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
using ScannerInventering.Classes;

namespace ScannerInventering
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Artikel> artiklar = new List<Artikel>();
        bool start;
        bool AsyncStarted = false;
        public MainWindow()
        {
            InitializeComponent();
            stopBtn.IsEnabled = false;
            prodDataGrid.ItemsSource = artiklar;
        }

        async void ScannerConnect()
        {
            AsyncStarted = true;
            start = true;
            //string IP = "127.0.0.1";
            string IP = "10.46.56.38";

            //int Port = 51000;

            //string IP = Properties.Settings.Default.ScannerIP;
            int Port = Properties.Settings.Default.ScannerPort;
            TcpClient client = new TcpClient();

            await client.ConnectAsync(IP, Port);
            Console.WriteLine("Client connected.");
            NetworkStream stream = client.GetStream();

            while (start)
            {
                try
                {
                    Console.WriteLine("Waiting for article scan..");

                    byte[] data = new byte[256];
                    string response = string.Empty;
                    int bytes = await stream.ReadAsync(data, 0, data.Length);
                    response = Encoding.Default.GetString(data, 0, bytes);
                    if (response.Contains("Welcome"))
                    {
                        bytes = await stream.ReadAsync(data, 0, data.Length);
                        response = Encoding.Default.GetString(data, 0, bytes);
                    }
                    response = response.Replace("\u0002", "");
                    response = response.Replace("\u0003", "");
                    var resSplit = response.Split(new[] { (char)32 }, 2);
                    Console.WriteLine("Recieved: {0}", response);
                    statusBar.Text = "Artikel: " + response;
                    
                    stream = client.GetStream();
                    Console.WriteLine("Waiting for quantity scan..");

                    byte[] data2 = new byte[256];
                    string response2 = string.Empty;
                    int bytes2 = await stream.ReadAsync(data2, 0, data2.Length);
                    response2 = Encoding.ASCII.GetString(data2, 0, bytes2);
                    if (response2.Contains("Welcome"))
                    {
                        bytes2 = await stream.ReadAsync(data2, 0, data2.Length);
                        response2 = Encoding.Default.GetString(data2, 0, bytes2);
                    }

                    Console.WriteLine("Recieved: {0}", response2);
                    statusBar.Text = "Antal: " + response2;
                    if (response2.Contains(" "))
                    {
                        response2 = "";
                    }
                    response2 = response2.Replace("\u0002", "");
                    response2 = response2.Replace("\u0003", "");
                    try
                    {
                        Artikel artikel = new Artikel
                        {
                            Artikelnummer = resSplit[0],
                            Artikelnamn = resSplit[1],
                            Antal = response2
                        };
                        Console.WriteLine("Adding article.");
                        artiklar.Add(artikel);
                        prodDataGrid.ItemsSource = null;
                        prodDataGrid.ItemsSource = artiklar;
                    }
                    catch (Exception err)
                    {
                        statusBar.Text = "Fel vid tilläggning av artikel.";
                        SystemSounds.Beep.Play();
                    }

                    prodDataGrid.ScrollIntoView(prodDataGrid.Items.GetItemAt(prodDataGrid.Items.Count-1));
                    Console.WriteLine("Cycle done, closing stream.");

                }
                catch (Exception err)
                {
                    statusBar.Text = "Fel med skanner. Tryck på start för att fortsätta.";
                    start = false;
                    SystemSounds.Hand.Play();
                }

            }
            AsyncStarted = false;
            stream.Close();
            client.Dispose();
            startBtn.IsEnabled = true;
            stopBtn.IsEnabled = false;

        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AsyncStarted==false)
            {
                ScannerConnect();
                startBtn.IsEnabled = false;
                stopBtn.IsEnabled = true;
            }
            else
            {
                statusBar.Text = "Skanna in artikel för att möjliggöra återstart.";
                SystemSounds.Hand.Play();
            }
        }

        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            start = false;
            statusBar.Text = "Slutför en sista skanning innan du klickar på \"Start\" på nytt.";
            startBtn.IsEnabled = true;
            stopBtn.IsEnabled = false;
        }

        private void doneBtn_Click(object sender, RoutedEventArgs e)
        {
            var filename = DateTime.Now.ToString("MMddyyyy-HHmmss") + ".txt";
            var filepath = Properties.Settings.Default.InventFilePath + filename;
            try
            {
                using (StreamWriter w = File.CreateText(filepath))
                {
                    w.WriteLine("01");
                    w.WriteLine("#12219;" + signTextBox.Text);
                    w.WriteLine("#12283;" + lagerTextBox.Text);
                    w.WriteLine("11");
                    foreach (var art in artiklar)
                    {
                        w.WriteLine("#12401;" + art.Artikelnummer);
                        w.WriteLine("#12441;" + art.Antal);
                        w.WriteLine("11");
                    }
                };
                MessageBox.Show("Fil sparad på plats: " + filepath + ".");
            }
            catch(Exception err)
            {
                MessageBox.Show(err.Message, "Fel vid sparning av fil");
                SystemSounds.Hand.Play();
            }
            
            
        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                artiklar.RemoveAt(prodDataGrid.SelectedIndex);
                prodDataGrid.ItemsSource = null;
                prodDataGrid.ItemsSource = artiklar;
            }
            catch(Exception err)
            {
                MessageBox.Show(err.Message);
                SystemSounds.Hand.Play();
            }
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings setwin = new Settings();
            setwin.Show();
        }
    }
}
