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
using System.Windows.Forms.Integration;
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
        private IntValueObject logPanelHeight, intSettingButtonStage;
        private Thread socketThread;
        private List<Socket> tcpListSocketConnect = new List<Socket>();
        private Server myServer;
        private static Boolean isServerBusy = false;
        private VisionJob cogCamera01;
        private CameraIndex cameraIndex;
        private VisionJob[] listCameras = new VisionJob[4];
        private string currentJobUrl = @"E:\#Latus\JobRun\SVI_20182111_1136";
        private StringValueObject settingDisplayCameraInfo;

        public MainWindow()
        {
            InitializeComponent();
            // Khai bao hien thi
            DisplayInitial();
            // Khai bao Server TCP/IP
            VisionProInitial();
            //
            SocketTCPInitial();

        }

        /// <summary>
        /// Khởi tạo AppVisionPro, khởi tạo đối tượng, đặt các hiển thị giao diện Tool;
        /// </summary>
        private void VisionProInitial()
        {
            cogCamera01 = new VisionJob();
            for (int i = 0; i < listCameras.Length; i++)
            {
                listCameras[i] = new VisionJob();
            }

            // Load Job
            LoadCurrentJobInitial();

            wfCogDisplayMain1.Child = listCameras[0].CogDisplayMain;
            wfCogDisplayMain2.Child = listCameras[1].CogDisplayMain;
            wfCogDisplayMain3.Child = listCameras[2].CogDisplayMain;
            wfCogDisplayMain4.Child = listCameras[3].CogDisplayMain;
            //cogCamera01.ToolBlockEdit.Subject.Ran += ShowResult;
        }

        /// <summary>
        /// Load lại chương trình các Camera theo đường dẫn lưu trong currentJobUrl
        /// </summary>
        private void LoadCurrentJobInitial()
        {
            try
            {
                for (int i = 0; i < listCameras.Length; i++)
                {
                    listCameras[i].LoadJob(currentJobUrl + $"\\Cam{i}");
                }
            }
            catch { MessageBox.Show("Fail to Load Job! :("); }
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

            // Kiểm tra Camera tương ứng
            int tempCameraCmdIndex = -1;
            string strTrig = "TriggerCamera";
            if (rev.IndexOf(strTrig) >= 0)
            {
                try
                {
                    string tempGet = rev.Substring(rev.IndexOf(strTrig) + strTrig.Length, 1);
                    tempCameraCmdIndex = int.Parse(tempGet);
                }
                catch
                {
                    tempCameraCmdIndex = -1;
                }
            }

            logString.Value = tempS + logString.Value;
            if (!isServerBusy)
            {
                string tempSend = "";
                // Bật cờ báo Busy và tắt ở cuối chu trình
                isServerBusy = true;
                // Chạy Job Camera tương ứng
                if ((tempCameraCmdIndex > -1) && (tempCameraCmdIndex < 4)) tempSend = listCameras[tempCameraCmdIndex].RunJob();
                else
                {
                    tempSend = "Error Cmd Camera!0";
                }
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
            logPanelHeight = new IntValueObject(20);
            intSettingButtonStage = new IntValueObject(1);
            logString = new StringValueObject("\r\n");
            cameraIndex = new CameraIndex();
            settingDisplayCameraInfo = new StringValueObject("");

            // Cài đặt các giá trị Context Binding
            // Chiều cao Box Log
            gridTotalMain.DataContext = logPanelHeight;
            // Nút Hide/View Log
            btnViewLog.DataContext = strViewButton;
            // Hộp hiển thị Log
            txtLogBox.DataContext = logString;
            // Số thứ tự Camera Setting hiện tại
            lblCameraIndex.DataContext = cameraIndex;

            tab1Column.Width = new GridLength(10000, GridUnitType.Star);
            tab2Column.Width = new GridLength(1, GridUnitType.Star);
            tab3Column.Width = new GridLength(1, GridUnitType.Star);

            // Mặc định hiển thị Tool Acq của Camera 0;
            wfSettingPanel.ChildChanged += UpdateDislayCameraTool;
            if (listCameras[0] != null) wfSettingPanel.Child = listCameras[0].AcqFifoTool;
        }

        /// <summary>
        /// Cập nhật hiển thị khi thay đổi nội dung của hiển thị Setting (chuyển Tool Camera)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateDislayCameraTool(object sender, ChildChangedEventArgs e)
        {
            // Cập nhật hiển thị của Label Info
            settingDisplayCameraInfo.Value = listCameras[cameraIndex.Value].GetInfo();
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

        private void MenuItemSetting_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            int value1 = 0;
            int value2 = 0;
            int value3 = 0;
            switch ((sender as MenuItem).Header)
            {
                case "Running":
                    value1 = 10; value2 = 0; value3 = 0;
                    break;
                case "StartUp":
                    value1 = 0; value2 = 10; value3 = 0;
                    break;
                case "Initial":
                    value1 = 0; value2 = 0; value3 = 10;
                    break;
                case "Settings":
                    value1 = 0; value2 = 0; value3 = 10;
                    break;
                default:
                    MessageBox.Show("Wrong Setting! Select Menu Switch");
                    break;
            }
            tab1Column.Width = new GridLength(value1, GridUnitType.Star);
            tab2Column.Width = new GridLength(value2, GridUnitType.Star);
            tab3Column.Width = new GridLength(value3, GridUnitType.Star);
        }

        /// <summary>
        /// Hiển thị control Tab tương ứng 0, 1, 2
        /// </summary>
        /// <param name="tabIndex"></param>
        private void showControlTab(int tabIndex)
        {
            int value1 = 0;
            int value2 = 0;
            int value3 = 0;
            switch (tabIndex)
            {
                case 0:
                    value1 = 10; value2 = 0; value3 = 0;
                    break;
                case 1:
                    value1 = 0; value2 = 10; value3 = 0;
                    break;
                case 2:
                    value1 = 0; value2 = 0; value3 = 10;
                    break;
                default:
                    MessageBox.Show("Wrong Setting! Select Menu Switch");
                    break;
            }
            tab1Column.Width = new GridLength(value1, GridUnitType.Star);
            tab2Column.Width = new GridLength(value2, GridUnitType.Star);
            tab3Column.Width = new GridLength(value3, GridUnitType.Star);
        }

        private void LabelNextBackCamera_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Label).Content.ToString() == ">")
            {
                cameraIndex.Value += 1;
                if (cameraIndex.Value == 4) cameraIndex.Value = 0;
            }
            else
            {
                cameraIndex.Value -= 1;
                if (cameraIndex.Value == -1) cameraIndex.Value = 3;
            }

            // Load Default View
            wfSettingPanel.Child = listCameras[cameraIndex.Value].AcqFifoTool;
        }

        private void radioModeImagebtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as RadioButton).Content.ToString().IndexOf("0") > 0) listCameras[cameraIndex.Value].ImageInputMode = 0;
            else listCameras[cameraIndex.Value].ImageInputMode = 1;
            wfSettingPanel.Child = listCameras[cameraIndex.Value].ImageInputTool as System.Windows.Forms.Control;
        }

        private void BtnSettingSelect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show((sender as Button).Name.ToString());
            switch ((sender as Button).Name)
            {
                case ("btnSettingCameraInitial"):
                    //MessageBox.Show((sender as Button).Name);
                    wfSettingPanel.Child = listCameras[cameraIndex.Value].AcqFifoTool;
                    break;
                case ("btnSettingCalib"):
                    if (listCameras[cameraIndex.Value].CalibGridCBTool != null) wfSettingPanel.Child = listCameras[cameraIndex.Value].CalibGridCBTool;
                    break;
                case ("btnSettingAlign"):
                    if (listCameras[cameraIndex.Value].PMAlignTool != null) wfSettingPanel.Child = listCameras[cameraIndex.Value].PMAlignTool;
                    break;
                case ("btnSettingInspection"):
                    //MessageBox.Show((sender as Button).Name);
                    wfSettingPanel.Child = null;
                    break;
                case ("btnSettingFinish"):
                    //Chuyển màn hình
                    showControlTab(0);
                    //MessageBox.Show((sender as Button).Name);
                    wfSettingPanel.Child = null;
                    break;
                case ("btnSaveJob"):
                    // Lưu chương trình Camera hiện tại theo đường dẫn trong currentJobUrl
                    listCameras[cameraIndex.Value].SaveJob(currentJobUrl + $"\\Cam{cameraIndex.Value}");
                    break;
                default:
                    break;
            }
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
