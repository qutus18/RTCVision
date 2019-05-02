using System;
using System.Collections.Generic;
using System.IO;
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
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using VisionApp.VisionPro;
using System.Collections.ObjectModel;

namespace VisionApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StringValueObject strViewButton, logString;
        private CameraIndex cameraIndex;
        private IntValueObject logPanelHeight, intSettingButtonStage;
        private Thread socketThread;
        private List<Socket> tcpListSocketConnect = new List<Socket>();
        private Server myServer;
        private static Boolean isServerBusy = false;
        private StringValueObject settingDisplayCameraInfo;
        private ICommand p_F3Command;
        private CameraVPro TestCamera = new CameraVPro();
        private ObservableCollection<CameraVPro> ListCameraVPro = new ObservableCollection<CameraVPro>();
        private System.Windows.Threading.DispatcherTimer TimerVision;
        private int numberCameraSign;

        public MainWindow()
        {
            InitializeComponent();
            // Khai bao hien thi
            DisplayInitial();
            // Khai bao Server TCP/IP
            VisionProInitial();
            // Khai báo Server Camera, nhận kết nối từ Robot
            SocketTCPInitial();
        }


        /// <summary>
        /// Khởi tạo AppVisionPro, khởi tạo đối tượng, đặt các hiển thị giao diện Tool;
        /// </summary>
        private void VisionProInitial()
        {
            // Khởi tạo List Camera 
            for (int i = 0; i < numberCameraSign; i++)
            {
                ListCameraVPro.Add(new CameraVPro());
            }
            wfCogDisplayMain1.Child = ListCameraVPro[0].CogDisplayOut;
            wfCogDisplayMain2.Child = ListCameraVPro[1].CogDisplayOut;
            cbbCameraList.ItemsSource = ListCameraVPro;

            //TestCamera.Load(0);
            TimerVision = new System.Windows.Threading.DispatcherTimer();
            TimerVision.Interval = new TimeSpan(0, 0, 10);
            TimerVision.Tick += LoadCamera;
            TimerVision.Start();
        }

        private void LoadCamera(object sender, EventArgs e)
        {
            // Load Camera Test
            TimerVision.Stop();
            //if (TestCamera.Loaded())
            //{
            //    TestCamera.Load(8);
            //}
            // Load List Camera
            for (int i = 0; i < numberCameraSign; i++)
            {
                ListCameraVPro[i].Load(i);
            }

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
        /// Xử lý lệnh nhận về từ Client
        /// </summary>
        /// <param name="rev"></param>
        /// <param name="socket"></param>
        private void ProcessClientCommand(string rev, Socket socket)
        {
            string tempS = $"Time: {DateTime.Now.ToLongTimeString()} - Receive cmd = {rev}, from client = {socket.RemoteEndPoint}\r\n";
            string toSend = ListCameraVPro[0].Command(rev) + " Received!\r\n";

            byte[] bytesToSend = Encoding.UTF8.GetBytes(toSend);
            socket.Send(bytesToSend);
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
            // Số lượng Camera Setting 
            numberCameraSign = Settings.Default.NumberCamera;

            tab1Column.Width = new GridLength(10000, GridUnitType.Star);
            tab2Column.Width = new GridLength(1, GridUnitType.Star);
            tab3Column.Width = new GridLength(1, GridUnitType.Star);

            // Mặc định hiển thị Tool Acq của Camera 0;
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
                case "_StartUp":
                    value1 = 0; value2 = 10; value3 = 0;
                    break;
                case "_Initial":
                    value1 = 0; value2 = 0; value3 = 10;
                    break;
                case "_Settings":
                    value1 = 0; value2 = 0; value3 = 10;
                    break;
                case "_RunTest":
                    ListCameraVPro[0].GetNormalAlign();
                    value1 = 0; value2 = 0; value3 = 10;
                    break;
                case "_LoadTest":
                    TestCamera.Load(0);
                    value1 = 10; value2 = 0; value3 = 0;
                    break;
                case "_ColorTest":
                    wfCogDisplayMain2.Child = TestCamera.CogDisplayOut;
                    value1 = 10; value2 = 0; value3 = 0;
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
        }

        private void BtnSettingSelect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show((sender as Button).Name.ToString());
            switch ((sender as Button).Name)
            {
                case ("btnSettingCameraInitial"):
                    //MessageBox.Show((sender as Button).Name);
                    wfSettingPanel.Child = ListCameraVPro[0].CogAcqFifoEdit as System.Windows.Forms.Control;
                    ChangeSettingsSmallGrid("Camera");
                    break;
                case ("btnSettingCalib"):
                    wfSettingPanel.Child = ListCameraVPro[0].CogCalibGrid as System.Windows.Forms.Control;
                    break;
                case ("btnSettingAlign"):
                    wfSettingPanel.Child = ListCameraVPro[0].CogPMAlign as System.Windows.Forms.Control;
                    ChangeSettingsSmallGrid("Align");
                    break;
                case ("btnSettingInspection"):
                    //MessageBox.Show((sender as Button).Name);
                    ChangeSettingsSmallGrid("Inspection");
                    break;
                case ("btnSettingFinish"):
                    //Chuyển màn hình
                    showControlTab(0);
                    //MessageBox.Show((sender as Button).Name);
                    wfSettingPanel.Child = null;
                    break;
                case ("btnSaveJob"):
                    // Lưu chương trình Camera hiện tại theo đường dẫn trong currentJobUrl
                    if (MessageBox.Show($"Confirm save current job camera {cameraIndex.Value}?", "Question", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        ListCameraVPro[0].Save(0);
                    }
                    break;
                default:
                    break;
            }
        }

        private void MenuItemRun_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListCameraVPro[0].GetNormalAlign();
        }

        private void radioModeImagebtn_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void BtnTrainInspectionSettings_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void MenuItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ListCameraVPro[0].TrainPattern();
        }

        private void FormMainClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            wfCogDisplayMain1.Child = null;
            Server.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TimerVision.Start();
        }

        private void BtnHomeMain_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            showControlTab(0);
        }

        /// <summary>
        /// Đổi tab Grid Settings nhỏ theo nút nhấn, lựa chọn từ tên grid truyền vào
        /// </summary>
        /// <param name="v"></param>
        private void ChangeSettingsSmallGrid(string name)
        {
            foreach (var item in SettingsSmallGrid.Children)
            {
                if ((item as Grid).Name.IndexOf(name) > 0) Grid.SetRow(item as Grid, 0);
                else Grid.SetRow(item as Grid, 1);
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
