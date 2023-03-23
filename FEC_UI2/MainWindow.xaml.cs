using LiveCharts.Defaults;
using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices.ComTypes;
using System.Collections.ObjectModel;
using System.Reflection.Emit;
using System.ComponentModel;

namespace FEC_UI2
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        public ChartValues<float>[] Values { get; set; }

        public ObservableCollection<string>[] Labels { get; set; }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly int _delay = 1000;

        Queue[] DataQ = new Queue[5];

        private const string SERVER_ADDRESS = "192.168.1.124";
        private const int SERVER_PORT = 6000;

        private TcpClient client;
        private NetworkStream stream;

        int bytesRead = 0;
        string data = null;

        // split the data into rows and columns
        string[] rows = null;
        string[][] values = null;
        string[] cols = null;

        public MainWindow()
        {
            try
            {
                InitializeComponent();

                DataContext = this;

                Values = new ChartValues<float>[5];
                for (int i = 0; i < Values.Length; i++)
                {
                    Values[i] = new ChartValues<float>();
                }

                Labels = new ObservableCollection<string>[5];
                for (int i = 0; i < Labels.Length; i++)
                {
                    Labels[i] = new ObservableCollection<string>();
                }

                for (int i = 0; i < DataQ.Length; i++)
                {
                    DataQ[i] = new Queue();
                }

                Sync = new object();

                Task.Run(InputData);

                Task.Run(ReadData);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Environment.Exit(1); // terminate the program with error code 1
            }
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
            // 지역 변수 초기화
            byte[] buffer = new byte[1024];
            float[] floatValue = new float[5];

            for (int i = 0; i < floatValue.Length; i++)
            {
                floatValue[i] = 0.0f;
            }

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
                Environment.Exit(1); // terminate the program with error code 1
            }

            while (true)
            {
                try
                {
                    await Task.Delay(500);

                    // receive data from server
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    data = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (!client.Connected)
                    {
                        MessageBox.Show("Connection to server lost.");
                        Environment.Exit(1); // terminate the program with error code 1
                    }

                    // split the data into rows and columns
                    rows = data.Split('\n');
                    if (rows.Length > 1)
                    {
                        values = new string[rows.Length][];
                        for (int i = 1; i < rows.Length; i++)
                        {
                            cols = rows[i].Split('\t');
                            values[i] = cols;
                        }

                        lock (Sync)
                        {
                            for (int i = 1; i < values.Length; i++)
                            {
                                if (float.TryParse(values[i][8], out floatValue[i]))
                                {
                                    Console.WriteLine(floatValue[i]); // Output: 3.14
                                }
                                else
                                {
                                    Console.WriteLine("Cannot convert string to float");
                                }
                                DataQ[i].Enqueue((float)floatValue[i]);
                                Console.WriteLine(values[i][9]);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Connection to server lost.");
                        Environment.Exit(1); // terminate the program with error code 1
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Environment.Exit(1); // terminate the program with error code 1
                }                
            }
        }
 
        private async Task ReadData()
        {
            await Task.Delay(1000);
            DateTime originalDateTime;
            string newTimeString = null;
            string newDateString = null;

            lock (Sync)
            {
                if (!client.Connected)
                {
                    MessageBox.Show("Connection to server lost.");
                    Environment.Exit(1); // terminate the program with error code 1
                }

                try
                {
                    for (int i = 1; i < values.Length; i++)
                    {
                        if (DataQ[i].Count > 0)
                        {
                            // Dispatcher.Invoke() 메서드를 사용하여 UI 스레드에서 실행
                            Dispatcher.Invoke(() =>
                            {
                                int index = int.Parse(values[i][0].Replace("Channel ", ""));
                                if (index == 1)
                                {
                                    timeTextBox1.Text = values[i][7];
                                }
                                else if (index == 2)
                                {
                                    timeTextBox2.Text = values[i][7];
                                }
                                else if (index == 3)
                                {
                                    timeTextBox3.Text = values[i][7];
                                }
                                else if (index == 4)
                                {
                                    timeTextBox4.Text = values[i][7];
                                }
                                else
                                {

                                }
                            });
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    // handle the exception
                    Console.WriteLine("NullReferenceException: " + ex.Message);
                    Environment.Exit(1); // terminate the program with error code 1
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Environment.Exit(1); // terminate the program with error code 1
                }
            }

            while (true)
            {
                try
                {
                    await Task.Delay(_delay);

                    if (!client.Connected)
                    {
                        MessageBox.Show("Connection to server lost.");
                        Environment.Exit(1); // terminate the program with error code 1
                    }

                    lock (Sync)
                    {
                        for (int i = 1; i < values.Length; i++)
                        {
                            if (DataQ[i].Count > 0)
                            {
                                // Dispatcher.Invoke() 메서드를 사용하여 UI 스레드에서 실행
                                Dispatcher.Invoke(() =>
                                {
                                    Values[i].Add((float)DataQ[i].Dequeue());
                                    originalDateTime = DateTime.ParseExact(values[i][9], "yyyy-MM-dd HH:mm:ss", null);
                                    newTimeString = originalDateTime.ToString("HH:mm:ss");
                                    Labels[i].Add(newTimeString);

                                    if (Values[i].Count > 120)
                                    {
                                        Values[i].RemoveAt(0);
                                        Labels[i].RemoveAt(0);
                                    }

                                    int index = int.Parse(values[i][0].Replace("Channel ", ""));
                                    if (index == 1)
                                    {
                                        PreBerTextBlock1.Text = values[i][1];
                                        PreErrorsTextBlock1.Text = values[i][2];
                                        CorrectedTextBlock1.Text = values[i][3];
                                        PostBerTextBlock1.Text = values[i][4];
                                        MarginTextBlock1.Text = values[i][5];
                                        BitsTextBlock1.Text = values[i][6];                                     
                                    }
                                    else if (index == 2)
                                    {
                                        PreBerTextBlock2.Text = values[i][1];
                                        PreErrorsTextBlock2.Text = values[i][2];
                                        CorrectedTextBlock2.Text = values[i][3];
                                        PostBerTextBlock2.Text = values[i][4];
                                        MarginTextBlock2.Text = values[i][5];
                                        BitsTextBlock2.Text = values[i][6];
                                    }
                                    else if (index == 3)
                                    {
                                        PreBerTextBlock3.Text = values[i][1];
                                        PreErrorsTextBlock3.Text = values[i][2];
                                        CorrectedTextBlock3.Text = values[i][3];
                                        PostBerTextBlock3.Text = values[i][4];
                                        MarginTextBlock3.Text = values[i][5];
                                        BitsTextBlock3.Text = values[i][6];
                                    }
                                    else if (index == 4)
                                    {
                                        PreBerTextBlock4.Text = values[i][1];
                                        PreErrorsTextBlock4.Text = values[i][2];
                                        CorrectedTextBlock4.Text = values[i][3];
                                        PostBerTextBlock4.Text = values[i][4];
                                        MarginTextBlock4.Text = values[i][5];
                                        BitsTextBlock4.Text = values[i][6];
                                    }
                                    else
                                    {

                                    }

                                    newDateString = originalDateTime.ToString("yyyy-MM-dd");
                                    DateTextBlock.Text = newDateString;

                                    OnPropertyChanged(nameof(Labels));
                                });
                            }
                        }
                    }
                }
                catch (NullReferenceException ex)
                {
                    // handle the exception
                    Console.WriteLine("NullReferenceException: " + ex.Message);
                    Environment.Exit(1); // terminate the program with error code 1
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Environment.Exit(1); // terminate the program with error code 1
                }
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string timeValue = null;
            int channel = 0;
            Button clickedButton = sender as Button;

            if (clickedButton != null)
            {
                // Do something based on the clicked button
                if (clickedButton.Name == "SendButton1")
                {
                    // Get the value of the TextBox
                    timeValue = timeTextBox1.Text;
                    channel = 1;
                }
                else if (clickedButton.Name == "SendButton2")
                {
                    // Get the value of the TextBox
                    timeValue = timeTextBox2.Text;
                    channel = 2;
                }
                else if (clickedButton.Name == "SendButton3")
                {
                    // Get the value of the TextBox
                    timeValue = timeTextBox3.Text;
                    channel = 3;
                }
                else if (clickedButton.Name == "SendButton4")
                {
                    // Get the value of the TextBox
                    timeValue = timeTextBox4.Text;
                    channel = 4;
                }
            }

            // Convert channel and timeValue to bytes and combine them into a single array
            byte[] channelBytes = BitConverter.GetBytes(channel);
            byte[] timeBytes = Encoding.ASCII.GetBytes(timeValue);
            byte[] data = new byte[channelBytes.Length + timeBytes.Length];
            Array.Copy(channelBytes, 0, data, 0, channelBytes.Length);
            Array.Copy(timeBytes, 0, data, channelBytes.Length, timeBytes.Length);

            // Send the data
            stream.Write(data, 0, data.Length);

            MessageBox.Show(string.Format("Channel {0}의 데이터 전송 주기 수정이 완료되었습니다.", channel));
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Perform normal shutdown of the application
            Application.Current.Shutdown();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // close connection and socket
                stream?.Close();
                client?.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Environment.Exit(1); // terminate the program with error code 1
            }
        }
    }
}
