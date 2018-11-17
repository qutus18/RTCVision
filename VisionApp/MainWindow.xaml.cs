using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
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

namespace VisionApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StringValueObject strViewButton, logString;
        private DimentionObject logPanelHeight;
        private Thread socketThread;
        private List<Socket> tcpListSocketConnect = new List<Socket>();
        private Server myServer;
        private static Boolean isServerBusy = false;
        private VisionJob cogCamera01;

        public MainWindow()
        {
            InitializeComponent();
            // Khai bao hien thi
            DisplayInitial();
            // Khai bao Server TCP/IP
            VisionProInitial();
            SocketTCPInitial();

        }

        /// <summary>
        /// Khởi tạo AppVisionPro, khởi tạo đối tượng, đặt các hiển thị giao diện Tool;
        /// </summary>
        private void VisionProInitial()
        {
            cogCamera01 = new VisionJob();
            cogCamera01.PathToolBlock = @"E:\#Latus\PMAlignFixturingBlobRegionAndResults.vpp";
            wfCogDisplayMain1.Child = cogCamera01.ToolGroupEdit;
            wfCogDisplayMain2.Child = cogCamera01.ImageFileEdit;
            wfCogDisplayMain3.Child = cogCamera01.CalibGridCBTool;
            wfCogDisplayMain4.Child = cogCamera01.PMAlignTool;
            //cogCamera01.ToolBlockEdit.Subject.Ran += ShowResult;
        }

        private void ShowResult(object sender, EventArgs e)
        {
            Console.WriteLine("Ran!!");
        }

        /// <summary>
        /// Khởi tạo thread kết nối Socket Server
        /// </summary>
        private void SocketTCPInitial()
        {
            Server.eventReceiveString += ProcessClientCommand;
            socketThread = new Thread(socketServerListenThread);
            socketThread.IsBackground = true;
            socketThread.Name = "Socket TCP Thread";
            socketThread.Start();
        }

        /// <summary>
        /// Hàm này được tạo để gọi từ Thread client, mục đích thực hiện hàm ở MainThread
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static string ReturnMethod(ref Socket handler)
        {
            return $"You are {handler.RemoteEndPoint.ToString()}";
        }

        /// <summary>
        /// Thread chạy Socket Server để nhận dữ liệu từ các Client connect đến
        /// </summary>
        private void socketServerListenThread()
        {
            myServer = new Server();
            myServer.Start();
        }

        /// <summary>
        /// Wait a second
        /// </summary>
        /// <returns></returns>
        async Task PutTaskDelay()
        {
            await Task.Delay(100);
        }

        /// <summary>
        /// Xử lý lệnh nhận về từ Client
        /// </summary>
        /// <param name="rev"></param>
        /// <param name="socket"></param>
        private async void ProcessClientCommand(string rev, Socket socket)
        {
            string tempS = $"Time: {DateTime.Now.ToLongTimeString()} - Receive cmd = {rev}, from client = {socket.RemoteEndPoint}\r\n";
            logString.Value = tempS + logString.Value;
            if (!isServerBusy)
            {
                string tempSend = "";
                // Bật cờ báo Busy và tắt ở cuối chu trình
                isServerBusy = true;
                // Chạy Job Camera tương ứng
                tempSend = cogCamera01.RunJob();
                tempSend = "camS_" + tempSend + "_camE\r\n";
                byte[] bytesToSend = Encoding.UTF8.GetBytes(tempSend);
                //await PutTaskDelay();
                socket.Send(bytesToSend, 0, bytesToSend.Length, SocketFlags.None);
                // Tắt cờ Busy
                isServerBusy = false;
            }
            else
            {
                byte[] bytesToSend = Encoding.UTF8.GetBytes("I'm very busy, see you later!");
                socket.Send(bytesToSend, 0, bytesToSend.Length, SocketFlags.None);
            }
        }

        /// <summary>
        /// Khởi tạo màn hình hiển thị
        /// </summary>
        private void DisplayInitial()
        {
            strViewButton = new StringValueObject("Show Log");
            logPanelHeight = new DimentionObject(20);
            logString = new StringValueObject("\r\n");
            gridTotalMain.DataContext = logPanelHeight;
            btnViewLog.DataContext = strViewButton;
            txtLogBox.DataContext = logString;
        }

        /// <summary>
        /// Form Close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnMenuExit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
        private void lblExit_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Ẩn hiện cửa sổ Logging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnViewLog_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (strViewButton.Value.IndexOf("Show") >= 0)
            {
                strViewButton.Value = "Hide Log";
                logPanelHeight.Value = 200;
                txtLogBox.ScrollToHome();
            }
            else
            {
                strViewButton.Value = "Show Log";
                logPanelHeight.Value = 20;
                txtLogBox.ScrollToHome();
            }
        }


    }
}
