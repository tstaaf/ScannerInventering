using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public MainWindow()
        {
            InitializeComponent();
            stopBtn.IsEnabled = false;
            prodDataGrid.ItemsSource = artiklar;
        }

        //async void AsyncConnect()
        //{
        //    string IP = "127.0.0.1";
        //    int Port = 9100;
        //    int timeout = 5000;

        //    using (var client = new TcpClient())
        //        using(var reader = new StreamReader(stream))
        //    {
        //        await client.ConnectAsync(IP, Port);
        //        var stream = client.GetStream();
        //        stream.ReadTimeout = timeout;

        //        string response = await reader.ReadLineAsync();
        //        Console.WriteLine(response);
        //    }
        //}

        async void ScannerConnect()
        {
            try
            {
                start = true;
                string IP = "10.46.56.38";
                
                int Port = 51000;

                //string IP = Properties.Settings.Default.ScannerIP;
                //int Port = 9100;

                
                while (start)
                {
                    TcpClient client = new TcpClient();
                    await client.ConnectAsync(IP, Port);

                    NetworkStream stream = client.GetStream();
                    Console.WriteLine("Waiting for scan..");

                    byte[] data = new byte[256];
                    string response = string.Empty;
                    int bytes = await stream.ReadAsync(data, 0, data.Length);
                    response = Encoding.ASCII.GetString(data, 0, bytes);
                    if (response.Contains("Welcome"))
                    {
                        bytes =  await stream.ReadAsync(data, 0, data.Length);
                        response = Encoding.ASCII.GetString(data, 0, bytes);
                    }
                    var resSplit = response.Split(new[] { (char)32 }, 2);
                    Console.WriteLine("Recieved: {0}", response);

                    NetworkStream stream2 = client.GetStream();
                    Console.WriteLine("Waiting for scan..");

                    byte[] data2 = new byte[256];
                    string response2 = string.Empty;
                    int bytes2 = await stream.ReadAsync(data2, 0, data2.Length);
                    response2 = Encoding.ASCII.GetString(data2, 0, bytes2);

                    Console.WriteLine("Recieved: {0}", response2);

                    Artikel artikel = new Artikel
                    {
                        Artikelnummer = resSplit[0],
                        Artikelnamn = resSplit[1],
                        Antal = int.Parse(response2)
                    };

                    artiklar.Add(artikel);
                    prodDataGrid.ItemsSource = null;
                    prodDataGrid.ItemsSource = artiklar;
                    stream.Close();
                    client.Close();
                }
                
                
            }
            catch (Exception err)
            {
                MessageBox.Show("Fel med skanner: " + (char)10 + err.Message, "Något gick fel, klicka på \"Start\" för att fortsätta.");
            }
        }

        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            ScannerConnect();
            startBtn.IsEnabled = false;
            stopBtn.IsEnabled = true;
            //AsyncConnect();
        }

        private void stopBtn_Click(object sender, RoutedEventArgs e)
        {
            start = false;
            MessageBox.Show("Slutför en sista skanning innan du klickar på \"Start\" på nytt.", "Varning", MessageBoxButton.OK, MessageBoxImage.Warning);
            startBtn.IsEnabled = true;
            stopBtn.IsEnabled = false;
        }

        private void doneBtn_Click(object sender, RoutedEventArgs e)
        {
            var filepath = Properties.Settings.Default.InventFilePath;
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
            }
        }

        private void settingsBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings setwin = new Settings();
            setwin.Show();
        }
    }
}
