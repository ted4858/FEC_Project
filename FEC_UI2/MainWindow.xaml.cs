using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;

namespace FEC_UI2
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public ChartValues<float> Values { get; set; }

        private readonly Random _r = new Random();
        private readonly int _delay = 1000;

        Queue DataQ = new Queue();

        private const string SERVER_ADDRESS = "192.168.1.122";
        private const int SERVER_PORT = 6000;

        private TcpClient client;
        private NetworkStream stream;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;

            Values = new ChartValues<float>();

            Sync = new object();

            Task.Run(InputData);

            Task.Run(ReadData);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            int result;
            if (!int.TryParse(e.Text, out result))
            {
                e.Handled = true; // 입력한 문자열을 처리하지 않음
            }
        }

        public object Sync { get; }
 

        private async Task InputData()
        {
            byte[] buffer = new byte[1024];

            try
            {
                // connect to server
                client = new TcpClient();
                await client.ConnectAsync(SERVER_ADDRESS, SERVER_PORT);
                stream = client.GetStream();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            while (true)
            {
                await Task.Delay(500);

                // receive data from server
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                float floatValue;

                lock (Sync)
                {
                    if (float.TryParse(data, out floatValue))
                    {
                        Console.WriteLine(floatValue); // Output: 3.14
                    }
                    else
                    {
                        Console.WriteLine("Cannot convert string to float");
                    }
                    DataQ.Enqueue((float)floatValue);
                }
            }
        }
 
        private async Task ReadData()
        {
            await Task.Delay(1000);
 
            while (true)
            {
                await Task.Delay(_delay);
                lock (Sync)
                {
                    if (DataQ.Count > 0)
                    {
                        // Dispatcher.Invoke() 메서드를 사용하여 UI 스레드에서 실행
                        Dispatcher.Invoke(() =>
                        {
                            Values.Add((float)DataQ.Dequeue());
                            if (Values.Count > 120)
                            {
                                Values.RemoveAt(0);
                            }
                        });
                    }
                }
            }
        }

        public string TimeFormatter(TimeSpan time)
        {
            return DateTime.Now.ToString("yyyy/MM/dd h:mm:ss tt");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // close connection and socket
            stream?.Close();
            client?.Close();
        }
    }
}
