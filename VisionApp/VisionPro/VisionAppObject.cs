using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.CalibFix;
using System;
using System.Windows;
using System.IO;

namespace VisionApp
{
    public class VisionJob : ObservableObject
    {
        // Khai báo đầu vào ảnh
        private CogImageFileEditV2 cogImageFileTool = null;
        public CogImageFileEditV2 CogImageFileTool
        {
            get { return cogImageFileTool; }
            set { cogImageFileTool = value; }
        }
        private CogAcqFifoEditV2 cogAcqFifoEdit = null;
        public CogAcqFifoEditV2 CogAcqFifoEdit
        {
            get { return cogAcqFifoEdit; }
            set { cogAcqFifoEdit = value; }
        }
        // Image Input Mode : 0 - Camera, 1 - Imagefile
        private int imageInputMode = 0;
        public int ImageInputMode
        {
            get { return imageInputMode; }
            set
            {
                imageInputMode = value;
                OnPropertyChanged("ImageInputMode");
                UpdateBindingInputImage();
            }
        }
        public object ImageInputTool
        {
            get
            {
                switch (imageInputMode)
                {
                    case 0:
                        return cogAcqFifoEdit;
                    case 1:
                        return cogImageFileTool;
                    default:
                        return cogAcqFifoEdit;
                }
            }
            private set { }
        }
        // Khai báo Tool Calib
        private CogCalibCheckerboardEditV2 calibGribCBTool = null;
        public CogCalibCheckerboardEditV2 CalibGridCBTool
        {
            get { return calibGribCBTool; }
            set { calibGribCBTool = value; }
        }
        // Khai báo Tool Align
        private CogPMAlignEditV2 pmAlignTool = null;
        public CogPMAlignEditV2 PMAlignTool
        {
            get { return pmAlignTool; }
            set { pmAlignTool = value; }
        }
        // Khai báo Tool Fixture
        private CogFixtureEditV2 cogFixtureTool;
        public CogFixtureEditV2 CogFixtureTool
        {
            get { return cogFixtureTool; }
            set { cogFixtureTool = value; }
        }
        // Khai báo Tool Inspection
        private CogPMAlignEditV2 pmInspectionTool;
        public CogPMAlignEditV2 PMInspectionTool
        {
            get { return pmInspectionTool; }
            set { pmInspectionTool = value; }
        }
        private int numberFullPanel; // Số lượng màn hình
        public int NumberFullPanel
        {
            get { return numberFullPanel; }
            set { numberFullPanel = value; OnPropertyChanged("NumberFullPanel"); }
        }
        private int numberStartPanel; // Vị trí đầu tiên Train
        public int NumberStartPanel
        {
            get { return numberStartPanel; }
            set { numberStartPanel = value; OnPropertyChanged("NumberStartPanel"); }
        }
        private int numberEndPanel; // Vị trí cuối cùng Train
        public int NumberEndPanel
        {
            get { return numberEndPanel; }
            set { numberEndPanel = value; OnPropertyChanged("NumberEndPanel"); }
        }
        /// <summary>
        ///  Khoảng cách và tọa độ Master
        /// </summary>
        private double xDistance;
        public double XDistance
        {
            get { return xDistance; }
            set { xDistance = value; }
        }
        private double yDistance;
        public double YDistance
        {
            get { return yDistance; }
            set { yDistance = value; }
        }
        private double xMaster0;
        public double XMaster0
        {
            get { return xMaster0; }
            set { xMaster0 = value; }
        }
        private double yMaster0;
        public double YMaster0
        {
            get { return yMaster0; }
            set { yMaster0 = value; }
        }

        // Khai báo Tool Display
        private CogDisplay cogDisplayMain = null;
        public CogDisplay CogDisplayMain
        {
            get { return cogDisplayMain; }
            set { cogDisplayMain = value; }
        }
        // Khai báo Pattern Result
        private patternObject[] listPatterns = new patternObject[20];
        private string lastSavedTime = "null";

        /// <summary>
        /// Khởi tạo Camera
        /// - Khởi tạo các Tool camera
        /// - Binding Image từ tool này sang tool khác
        /// - Khai báo các Event xử lý trong khi hoạt động
        /// </summary>
        public VisionJob()
        {
            // Khai báo Tool đầu vào ảnh từ máy tính
            cogImageFileTool = new CogImageFileEditV2();
            cogImageFileTool.Subject = new CogImageFileTool();

            // Load thư viện ảnh mặc định
            cogDisplayMain = new CogDisplay();
            cogImageFileTool.Subject.Operator.Open(@"C:\Program Files\Cognex\VisionPro\Images\CheckCal.idb", CogImageFileModeConstants.Read);

            // Thiết lập Camera đầu vào
            cogAcqFifoEdit = new CogAcqFifoEditV2();
            cogAcqFifoEdit.Subject = new CogAcqFifoTool();

            /// Khai báo tool Align. Mặc định link đầu vào ảnh với Tool Acq
            pmAlignTool = new CogPMAlignEditV2();
            pmAlignTool.Subject = new CogPMAlignTool();

            // Khai báo tool Fixture
            cogFixtureTool = new CogFixtureEditV2();
            cogFixtureTool.Subject = new CogFixtureTool();

            // Khai báo tool Inspection tìm vị trí màn hình
            pmInspectionTool = new CogPMAlignEditV2();
            pmInspectionTool.Subject = new CogPMAlignTool();
            // Load Setup Trained
            xDistance = Settings.Default.XDistance;
            yDistance = Settings.Default.YDistance;
            xMaster0 = Settings.Default.XMaster0;
            yMaster0 = Settings.Default.YMaster0;

            // Khai báo tool Calib. Đầu vào ảnh từ Tool Acq.
            calibGribCBTool = new CogCalibCheckerboardEditV2();
            calibGribCBTool.Subject = new CogCalibCheckerboardTool();

            // Khai báo Binding Image
            UpdateBindingImage(cogAcqFifoEdit, EventArgs.Empty);

            // Khai báo Event xử lý sự thay đổi của các Tool
            cogAcqFifoEdit.SubjectChanged += UpdateBindingImage;
            cogImageFileTool.SubjectChanged += UpdateBindingImage;
            pmAlignTool.SubjectChanged += UpdateBindingImage;
            pmAlignTool.SubjectChanged += UpdateEventSubjectAlignTool;
            pmAlignTool.Subject.Ran += AutoRunFixtureTool;
            calibGribCBTool.SubjectChanged += UpdateBindingImage;
            cogFixtureTool.SubjectChanged += UpdateBindingImage;
            pmInspectionTool.SubjectChanged += UpdateBindingImage;
        }

        /// <summary>
        /// Cập nhật Event khi Tool thay đổi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateEventSubjectAlignTool(object sender, EventArgs e)
        {
            pmAlignTool.Subject.Ran += AutoRunFixtureTool;
        }

        /// <summary>
        /// Tự động chạy Tool Fixture theo Tool Align
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoRunFixtureTool(object sender, EventArgs e)
        {
            if (pmAlignTool.Subject.Results.Count > 0)
            {
                cogFixtureTool.Subject.RunParams.UnfixturedFromFixturedTransform = pmAlignTool.Subject.Results[0].GetPose();
                cogFixtureTool.Subject.Run();
            }
        }

        // Update Binding Image - Khi có thay đổi tool thì cập nhật Binding toàn bộ
        private void UpdateBindingImage(object sender, EventArgs e)
        {
            // Xóa Binding cũ nếu có
            calibGribCBTool.Subject.DataBindings.Clear();
            pmAlignTool.Subject.DataBindings.Clear();
            cogFixtureTool.Subject.DataBindings.Clear();
            pmInspectionTool.Subject.DataBindings.Clear();
            // Cập nhật Binding mới
            switch (imageInputMode)
             {
                case 0:
                    calibGribCBTool.Subject.DataBindings.Add("InputImage", (ImageInputTool as CogAcqFifoEditV2).Subject, "OutputImage");
                    break;
                case 1:
                    calibGribCBTool.Subject.DataBindings.Add("InputImage", (ImageInputTool as CogImageFileEditV2).Subject, "OutputImage");
                    break;
                default:
                    break;
            } // Update Tool Calib
            switch (calibGribCBTool.Subject.Calibration.Calibrated)
            {
                case true:
                    pmAlignTool.Subject.DataBindings.Add("InputImage", calibGribCBTool.Subject, "OutputImage");
                    cogFixtureTool.Subject.DataBindings.Add("InputImage", calibGribCBTool.Subject, "OutputImage");
                    break;
                case false:
                    pmAlignTool.Subject.DataBindings.Add("InputImage", calibGribCBTool.Subject, "InputImage");
                    cogFixtureTool.Subject.DataBindings.Add("InputImage", calibGribCBTool.Subject, "InputImage");
                    break;
            }
            pmInspectionTool.Subject.DataBindings.Add("InputImage", cogFixtureTool.Subject, "OutputImage");
        }

        /// <summary>
        /// Chạy lần lượt tất cả các Job
        /// Trả về kết quả tọa độ pattern tìm thấy
        /// Trả về Fail nếu không tìm được giá trị nào
        /// </summary>
        /// <returns></returns>
        public string RunJob()
        {
            // Chuỗi trả về của hàm
            string returnString = "";
            CogTransform2DLinear temp = null;

            // Chạy lần lượt các Tool từ đầu xuống cuối

            cogImageFileTool.Subject.Run();
            cogAcqFifoEdit.Subject.Run();
            if (!calibGribCBTool.Subject.Calibration.Calibrated) MessageBox.Show("Image Not Calibration!!!");
            else calibGribCBTool.Subject.Run();
            pmAlignTool.Subject.Run();
            /// Thêm kết quả tool Align vào đầu vào Tool Fixture
            //cogFixtureTool.Subject.Run();
            pmInspectionTool.Subject.Run();

            /// Xử lý kết quả trả về từ tool Align
            /// Hiện tại đang gửi tất cả các tọa độ ra cho Robot
            if (pmAlignTool.Subject.Results.Count > 0)
            {
                // Tổng hợp kết quả trả về, nối vào returnString
                foreach (var item in pmAlignTool.Subject.Results)
                {
                    temp = (item as CogPMAlignResult).GetPose();
                    returnString += $"X : {temp.TranslationX.ToString("0.00")} - Y : {temp.TranslationY.ToString("0.00")} - Angle : {(temp.Rotation * 180 / Math.PI).ToString("0.00")}\r\n";
                }

                /// Nạp các giá trị pattern tìm thấy vào mảng dữ liệu 
                /// 1. Khởi tạo mảng dữ liệu 20 phần tử
                /// 2. Thêm vào mảng
                /// 3. Sắp xếp mảng theo chiều giảm của tọa độ Pattern
                /// 4. In ra Console các giá trị của mảng
                #region Nạp giá trị pattern vào mảng, in ra màn hình
                listPatterns = new patternObject[20];
                for (int i = 0; i < listPatterns.Length; i++)
                {
                    listPatterns[i] = new patternObject();
                }
                int index = 0;
                foreach (var item in PMAlignTool.Subject.Results)
                {
                    var tempResult = item as CogPMAlignResult;
                    if (index < 20) listPatterns[index] = new patternObject { X = tempResult.GetPose().TranslationX, Y = tempResult.GetPose().TranslationY, Angle = tempResult.GetPose().Rotation * 180 / Math.PI };
                    index += 1;
                }
                listPatterns = ToolSupport.SortPatterns(listPatterns);
                foreach (var item in listPatterns)
                {
                    Console.WriteLine($"X: {item.X} Y: {item.Y} Angle: {item.Angle}");
                }
                #endregion
                // Test End Panel
                CalculateEndPanelPosition();
                return returnString;
            }
            /// Xử lý kết quả trả về Tool Inspection
            /// 
            // Nếu không tìm thấy thì trả về Fail
            return "Fail";
        }

        /// <summary>
        /// Hàm load các tool của Camera theo đường dẫn nhập vào
        /// Đường dẫn có dạng ...\\Cam'x' với Camx tương ứng là chương trình Cam số x
        /// </summary>
        /// <param name="url"></param>
        public void LoadJob(string url)
        {
            if (File.Exists(url + "\\AqcTool.vpp")) CogAcqFifoEdit.Subject = CogSerializer.LoadObjectFromFile(url + "\\AqcTool.vpp") as CogAcqFifoTool;
            if (File.Exists(url + "\\CalibTool.vpp")) CalibGridCBTool.Subject = CogSerializer.LoadObjectFromFile(url + "\\CalibTool.vpp") as CogCalibCheckerboardTool;
            if (File.Exists(url + "\\PMAlignTool.vpp")) PMAlignTool.Subject = CogSerializer.LoadObjectFromFile(url + "\\PMAlignTool.vpp") as CogPMAlignTool;
            if (File.Exists(url + "\\PMInspectionTool.vpp")) PMInspectionTool.Subject = CogSerializer.LoadObjectFromFile(url + "\\PMInspectionTool.vpp") as CogPMAlignTool;
        }

        /// <summary>
        /// Lưu chương trình Camera hiện tại theo đường dẫn được nhập vào
        /// Thông báo Message đã hoàn thành xong
        /// </summary>
        /// <param name="url"></param>
        public void SaveJob(string url)
        {
            CogSerializer.SaveObjectToFile(CogAcqFifoEdit.Subject as CogAcqFifoTool, url + "\\AqcTool.vpp");
            CogSerializer.SaveObjectToFile(CalibGridCBTool.Subject as CogCalibCheckerboardTool, url + "\\CalibTool.vpp");
            CogSerializer.SaveObjectToFile(PMAlignTool.Subject as CogPMAlignTool, url + "\\PMAlignTool.vpp");
            CogSerializer.SaveObjectToFile(PMInspectionTool.Subject as CogPMAlignTool, url + "\\PMInspectionTool.vpp");
            Console.WriteLine("Save Job Done! :)");
            lastSavedTime = DateTime.Now.ToLongDateString() + "-" + DateTime.Now.ToLongTimeString();
        }

        /// <summary>
        /// Cập nhật trạng thái Job trả về dạng String
        /// </summary>
        /// <returns></returns>
        public string GetInfo()
        {
            // Thông tin Calib
            string tempOutputString = "";
            if (CalibGridCBTool.Subject.Calibration.Calibrated)
                tempOutputString += "Calibration : True\r\n";
            else tempOutputString += "Calibration : False\r\n";
            // Thông tin Align
            if (PMAlignTool.Subject.Pattern.Trained)
                tempOutputString += "PMAlign : True\r\n";
            else tempOutputString += "PMAlign : False\r\n";
            // Thông tin Save lần cuối
            tempOutputString += "Last Saved : " + lastSavedTime + "\r\n";

            return tempOutputString;
        }

        /// <summary>
        /// Cập nhật nguồn ảnh cho tool Calib khi Mode Input Image thay đổi
        /// </summary>
        private void UpdateBindingInputImage()
        {
            calibGribCBTool.Subject.DataBindings.Clear();
            // Remove and Update
            switch (imageInputMode)
            {
                case 0:
                    calibGribCBTool.Subject.DataBindings.Add("InputImage", (ImageInputTool as CogAcqFifoEditV2).Subject, "OutputImage");
                    break;
                case 1:
                    calibGribCBTool.Subject.DataBindings.Add("InputImage", (ImageInputTool as CogImageFileEditV2).Subject, "OutputImage");
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Train Inspection
        /// </summary>
        public void TrainInspectionPanel()
        {
            if (pmInspectionTool.Subject.Results.Count > 0)
            {
                /// Nạp các giá trị pattern tìm thấy vào mảng dữ liệu 
                /// 1. Khởi tạo mảng dữ liệu 20 phần tử
                /// 2. Thêm vào mảng
                /// 3. Sắp xếp mảng theo chiều giảm của tọa độ Pattern
                /// 4. In ra Console các giá trị của mảng
                listPatterns = new patternObject[20];
                for (int i = 0; i < listPatterns.Length; i++)
                {
                    listPatterns[i] = new patternObject();
                }
                int index = 0;
                foreach (var item in pmInspectionTool.Subject.Results)
                {
                    var tempResult = item as CogPMAlignResult;
                    listPatterns[index] = new patternObject { X = tempResult.GetPose().TranslationX, Y = tempResult.GetPose().TranslationY, Angle = tempResult.GetPose().Rotation * 180 / Math.PI };
                    index += 1;
                }
                listPatterns = ToolSupport.SortPatterns(listPatterns);
                // Calculate Train
                double tempXDistance, tempYDistance, tempXStart, tempYStart, tempXStop, tempYStop, tempXMaster0, tempYMaster0;
                tempXStart = listPatterns[0].X;
                tempYStart = listPatterns[0].Y;
                // Find Stop Point 
                int tempEndPoint = ToolSupport.EndPoint(listPatterns);
                tempXStop = listPatterns[tempEndPoint].X;
                tempYStop = listPatterns[tempEndPoint].Y;
                // Calculate Distance 2 Panel
                tempXDistance = ((tempXStop - tempXStart) / (numberEndPanel - numberStartPanel));
                tempYDistance = ((tempYStop - tempYStart) / (numberEndPanel - numberStartPanel));
                // Calculate Master0 Panel
                tempXMaster0 = tempXStart - numberStartPanel * tempXDistance;
                tempYMaster0 = tempYStart - numberStartPanel * tempYDistance;
                // Finish 
                XMaster0 = tempXMaster0;
                YMaster0 = tempYMaster0;
                XDistance = tempXDistance;
                YDistance = tempYDistance;
                // Save to Settings
                Settings.Default.XMaster0 = XMaster0;
                Settings.Default.YMaster0 = YMaster0;
                Settings.Default.XDistance = XDistance;
                Settings.Default.YDistance = YDistance;
                Settings.Default.Save();
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// Tính toán vị trí Panel cuối cùng
        /// </summary>
        public int CalculateEndPanelPosition()
        {
            if (pmInspectionTool.Subject.Results.Count > 0)
            {
                /// Nạp các giá trị pattern tìm thấy vào mảng dữ liệu 
                /// 1. Khởi tạo mảng dữ liệu 20 phần tử
                /// 2. Thêm vào mảng
                /// 3. Sắp xếp mảng theo chiều giảm của tọa độ Pattern
                /// 4. In ra Console các giá trị của mảng
                listPatterns = new patternObject[20];
                for (int i = 0; i < listPatterns.Length; i++)
                {
                    listPatterns[i] = new patternObject();
                }
                int index = 0;
                foreach (var item in pmInspectionTool.Subject.Results)
                {
                    var tempResult = item as CogPMAlignResult;
                    listPatterns[index] = new patternObject { X = tempResult.GetPose().TranslationX, Y = tempResult.GetPose().TranslationY, Angle = tempResult.GetPose().Rotation * 180 / Math.PI };
                    index += 1;
                }
                listPatterns = ToolSupport.SortPatterns(listPatterns);
                // Find Stop Point 
                int tempEndPoint = ToolSupport.EndPoint(listPatterns);
                double tempNegative = -10;
                double tempPositive = 10;
                // Find End Panel Position
                int tempPanel = -1;
                for (int i = 0; i < 20; i++)
                {
                    double tempX = XMaster0 + XDistance * i;
                    double tempY = YMaster0 + YDistance * i;
                    if ((listPatterns[tempEndPoint].X - tempX) > tempNegative)
                        if ((listPatterns[tempEndPoint].X - tempX) < tempPositive)
                            //if ((listPatterns[tempEndPoint].Y - tempY) > tempNegative)
                            //    if ((listPatterns[tempEndPoint].Y - tempY) > tempNegative)
                                {
                                    tempPanel = i;
                                    break;
                                }
                }
                MessageBox.Show($"Position Panel is : {tempPanel}");
                return tempPanel;
            }
            else
            {
                return -1;
            }
        }
    }
}
