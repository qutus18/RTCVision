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
using wform = System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
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
        private VisionAppObject cogCamera01;
        private CameraIndex cameraIndex;
        private VisionAppObject[] listCameras = new VisionAppObject[4];
        private string currentJobUrl = @"E:\#Latus\JobRun\SVI_20182111_1136";
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public MainWindow()
        {
            InitializeComponent();
            // Khai bao hien thi
            DisplayInitial();
            // Khai bao Server TCP/IP
            VisionProInitial();
            // K
            SocketTCPInitial();

        }

        /// <summary>
        /// Khởi tạo AppVisionPro, khởi tạo đối tượng, đặt các hiển thị giao diện Tool;
        /// </summary>
        private void VisionProInitial()
        {
            cogCamera01 = new VisionAppObject();
            for (int i = 0; i < listCameras.Length; i++)
            {
                listCameras[i] = new VisionAppObject();
                listCameras[i].outputStringEvent += ProcessOutputString;
            }

            // Load Job
            LoadCurrentJobInitial();
            //cogCamera01.ToolBlockEdit.Subject.Ran += ShowResult;
        }

        private void ProcessOutputString(string oString)
        {
            logString.Value = oString + "\r\n" + logString.Value;
            //logString.Value = oString + "\r\n" + logString.Value;
        }

        /// <summary>
        /// Load lại chương trình các Camera theo đường dẫn lưu trong currentJobUrl
        /// </summary>
        private async void LoadCurrentJobInitial()
        {
            try
            {
                for (int i = 0; i < listCameras.Length; i++)
                {
                    await listCameras[i].LoadJob(currentJobUrl + $"\\Cam{i}");
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
            log.Info($"Receive string = {rev} from socket = {socket.LocalEndPoint.ToString()}");
            string tempS = $"Time: {DateTime.Now.ToLongTimeString()} - Receive cmd = {rev}, from client = {socket.RemoteEndPoint}\r\n";
            logString.Value = tempS + logString.Value;
            // Kiểm tra server busy để trả về cho Robot
            if (!isServerBusy)
            {
                string tempSend = "";
                // Get Taget Camera cmd
                int cameraID = GetCameraIDFromRevString(rev);
                // If cmd format OK - Running Camera JOB
                if (cameraID > -1)
                {
                    // Bật cờ báo Busy và tắt ở cuối chu trình
                    isServerBusy = true;
                    // Chạy Job Camera tương ứng
                    log.Info($"Run Camera Job - Camera : {cameraID}");
                    tempSend = listCameras[cameraID].RunJob();
                    tempSend = "camS_" + tempSend + "_camE\r\n";
                    byte[] bytesToSend = Encoding.UTF8.GetBytes(tempSend);
                    //await PutTaskDelay();
                    socket.Send(bytesToSend, 0, bytesToSend.Length, SocketFlags.None);
                    // Tắt cờ Busy
                    isServerBusy = false;
                }
            }
            else
            {
                log.Info("Server Busy - reply to Robot");
                byte[] bytesToSend = Encoding.UTF8.GetBytes("I'm very busy, see you later!");
                socket.Send(bytesToSend, 0, bytesToSend.Length, SocketFlags.None);
            }
        }

        /// <summary>
        /// Trả về ID của camera với string gửi vào chứa "CameraNo"
        /// </summary>
        /// <param name="rev"></param>
        /// <returns></returns>
        private int GetCameraIDFromRevString(string rev)
        {
            // Trả về mặc định -1
            int tempID = -1;
            // Nếu đúng định dạng thì trả về camera ID
            if (rev.IndexOf("CameraNo") >= 0)
            {
                try
                {
                    tempID = int.Parse(rev.Substring(rev.IndexOf("CameraNo") + "CameraNo".Length, 1));
                }
                catch
                {
                    log.Error("Can't get CameraID from TCP command!");
                }
            }
            return tempID;
        }

        /// <summary>
        /// Khởi tạo màn hình hiển thị
        /// </summary>
        private void DisplayInitial()
        {
            log.Info("Initial Display - StartUp");
            strViewButton = new StringValueObject("Show Log");
            logPanelHeight = new IntValueObject(20);
            intSettingButtonStage = new IntValueObject(1);
            logString = new StringValueObject("\r\n");
            cameraIndex = new CameraIndex();

            gridTotalMain.DataContext = logPanelHeight;
            btnViewLog.DataContext = strViewButton;
            txtLogBox.DataContext = logString;
            lblCameraIndex.DataContext = cameraIndex;

            tab1Column.Width = new GridLength(10000, GridUnitType.Star);
            tab2Column.Width = new GridLength(1, GridUnitType.Star);
            tab3Column.Width = new GridLength(1, GridUnitType.Star);
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
        /// Change Screen when select item on Option Menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Xử lý button Chuyển Camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            wfSettingPanel.Child = listCameras[cameraIndex.Value].CogAcqFifoEdit;
        }

        private void MenuItemFile_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            switch ((sender as MenuItem).Header)
            {
                case "Load":
                    var dlg = new wform.FolderBrowserDialog();
                    wform.DialogResult result = dlg.ShowDialog(this.GetIWin32Window());
                    if (result == wform.DialogResult.OK)
                    {
                        currentJobUrl = dlg.SelectedPath;
                        ReloadAllCameraJob();
                    }
                    break;
                case "Backup":
                    dlg = new wform.FolderBrowserDialog();
                    result = dlg.ShowDialog(this.GetIWin32Window());
                    if (result == wform.DialogResult.OK)
                    {
                        //if (Directory.GetDirectories(dlg.SelectedPath).Count() > 0)
                        //    MessageBox.Show("Not Empty Folder");
                        string saveUrl = dlg.SelectedPath;
                        BackupCurrentCameraJob(saveUrl);
                    }
                    break;
                case "New":
                    CreatNewProgram();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Creat new Program from template, and change current url to new URL
        /// </summary>
        private void CreatNewProgram()
        {
            string getFartherFolder = currentJobUrl.Substring(0, currentJobUrl.LastIndexOf('\\') + 1);
            string tempNewUrl = getFartherFolder + DateTime.Now.ToString("YYMMdd_hhmmss") + "_CameraJob";
            Directory.CreateDirectory(tempNewUrl);
            for (int i = 0; i < 4; i++)
            {
                // Copy Temlate to New Camera Program
                DirectoryCopy.CopyMethod(@"E:\#Latus\Template", tempNewUrl + $"\\Cam{i}", true);
            }
            currentJobUrl = tempNewUrl;
            log.Info($"Creat New Job at : {currentJobUrl}");
            Dispatcher.BeginInvoke((Action)(() => LoadCurrentJobInitial()));
            //LoadCurrentJobInitial();
        }

        /// <summary>
        /// Backup dữ liệu chương trình hiện tại ra Folder đã chọn
        /// Lưu dưới dạng thư mục con kèm ngày tháng năm
        /// </summary>
        /// <param name="saveUrl"></param>
        private void BackupCurrentCameraJob(string saveUrl)
        {
            if (currentJobUrl != saveUrl)
            {
                string tempSaveUrl = saveUrl + $"\\{DateTime.Now.ToString("BU_YYMMdd_hhmmss")}";
                log.Info("Start Copy Camera Program to Backup Folder");
                DirectoryCopy.CopyMethod(currentJobUrl, tempSaveUrl, true);
                log.Info($"Done Backup Program to {tempSaveUrl}");
            }
            else
            {
                MessageBox.Show("Can't Backup because: Selected BU Direction same as Current Job Folder");
            }
        }

        /// <summary>
        /// Load lại toàn bộ Job theo đường dẫn đã chọn
        /// </summary>
        private void ReloadAllCameraJob()
        {
            log.Info("Begin Load Backup Job");
            LoadCurrentJobInitial();
        }

        private void radioModeImagebtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as RadioButton).Content.ToString().IndexOf("0") > 0) listCameras[cameraIndex.Value].ImageInputMode = 0;
            else listCameras[cameraIndex.Value].ImageInputMode = 1;
            wfSettingPanel.Child = listCameras[cameraIndex.Value].ImageInputTool as System.Windows.Forms.Control;
        }

        /// <summary>
        /// Xử lý khi chọn button bên tab Setting Camera
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSettingSelect_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show((sender as Button).Name.ToString());
            switch ((sender as Button).Name)
            {
                case ("btnSettingCameraInitial"):
                    //MessageBox.Show((sender as Button).Name);
                    wfSettingPanel.Child = listCameras[cameraIndex.Value].CogAcqFifoEdit;
                    break;
                case ("btnSettingCalib"):
                    if (listCameras[cameraIndex.Value].CalibGridCBTool != null) wfSettingPanel.Child = listCameras[cameraIndex.Value].CalibGridCBTool;
                    break;
                case ("btnSettingAlign"):
                    if (listCameras[cameraIndex.Value].PMAlignToolEdit != null) wfSettingPanel.Child = listCameras[cameraIndex.Value].PMAlignToolEdit;
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
        /// Ẩn hiện cửa sổ Logging (Mở rộng/ thu nhỏ)
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
