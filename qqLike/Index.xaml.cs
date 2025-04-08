using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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
using Microsoft.Win32;
using qqLike.Functional;
using qqLike.Model;
using qqLike.ViewModel;

namespace qqLike
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Index : Window
    {
        private bool expanded = false;
        public Socket Client { get; }
        private Thread recvThread;

        public int ContactPort
        {
            get
            {
                int res;
                int.TryParse(contactPort.Text, out res);
                return res;
            }
        }

        public Index()
        {
            InitializeComponent();
            try
            {
                Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Client.Connect("127.0.0.1", 8080);
                selfPort.Content = Client.LocalEndPoint.ToString();
                DataContext = new IndexViewModel(this);

                recvThread = new Thread(Receive);
                recvThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            userInfoCard.Visibility = Visibility.Collapsed;
            chatUI.Visibility = Visibility.Collapsed;
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ExpandOrRestore(object sender, RoutedEventArgs e)
        {
            if (!expanded)
            {
                this.WindowState = WindowState.Maximized;
                expandIcon.Source = new BitmapImage(new Uri("../Resource/Restore.png", UriKind.Relative));
                expanded = true;
            }
            else
            {
                this.WindowState = WindowState.Normal;
                expandIcon.Source = new BitmapImage(new Uri("../Resource/Expand.png", UriKind.Relative));
                expanded = false;
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Index_OnClosed(object? sender, EventArgs e)
        {
            if (!Client.Connected)
            {
                Client.Dispose();
                return;
            }

            Client.Shutdown(SocketShutdown.Both);
            Client.Disconnect(false);
            Thread.Sleep(10);
            Client.Close();
            recvThread.Join();
        }

        private void Index_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void Receive()
        {
            while (Client.Connected)
            {
                try
                {
                    if (Client.Available <= 0) continue;
                    byte[] buffer = new byte[1024 * 1024 * 10];

                    int recvBytes = Client.Receive(buffer);
                    ChatMessage message = JSON.Parse<ChatMessage>(Encoding.UTF8.GetString(buffer, 0, recvBytes));
                    if (message.Type == MessageType.Common.ToString())
                        UpdateUIInMT(() => chatContent.AppendText(
                            $"{message.Port} {message.Time.ToString("yyyy-MM-dd HH:mm:ss")}" +
                            $": {message.Content}\r\n"));
                    else if (message.Type == MessageType.File.ToString())
                    {
                        var result = MessageBox.Show("收到文件,是否接受", "文件", MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes)
                        {
                            SaveFileDialog dialog = new SaveFileDialog();
                            bool? whetherSave = dialog.ShowDialog();
                            if (whetherSave == null || !whetherSave.Value)
                                MessageBox.Show("取消保存文件");
                            else
                            {
                                var content = (JsonElement)message.Content;
                                var fileBytes = content.Deserialize<int[]>();
                                byte[] toWrite = new byte[fileBytes.Length];
                                for (int i = 0; i < fileBytes.Length; i++)
                                    toWrite[i] = (byte)fileBytes[i];
                                File.WriteAllBytes(dialog.FileName, toWrite);
                                UpdateUIInMT(() => chatContent.AppendText(
                                    $"{message.Port} {message.Time.ToString("yyyy-MM-dd HH:mm:ss")}: 文件\r\n"));
                            }
                        }
                        else
                            MessageBox.Show("已拒收");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + ex.StackTrace);
                    continue;
                }
            }
        }

        private void UpdateUIInMT(Action action)
        {
            Task.Run(() => { Application.Current.Dispatcher.Invoke(action); });
        }
    }
}