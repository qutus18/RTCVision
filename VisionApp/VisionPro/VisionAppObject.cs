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
        /// <summary>
        /// Trả về control ImageFileEdit tool
        /// </summary>
        public CogImageFileEditV2 ImageFileEdit
        {
            get { return cogImageFileTool; }
            set { cogImageFileTool = value; }
        }
        private CogAcqFifoEditV2 cogAcqFifoEdit = null;
        /// <summary>
        /// Trả về tool Acquition
        /// </summary>
        public CogAcqFifoEditV2 AcqFifoTool
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
        /// <summary>
        /// Trả về tool Calib
        /// </summary>
        public CogCalibCheckerboardEditV2 CalibGridCBTool
        {
            get { return calibGribCBTool; }
            set { calibGribCBTool = value; }
        }
        // Khai báo Tool Align
        private CogPMAlignEditV2 pmAlignTool = null;
        /// <summary>
        /// Trả về tool PMAlign
        /// </summary>
        public CogPMAlignEditV2 PMAlignTool
        {
            get { return pmAlignTool; }
            set { pmAlignTool = value; }
        }
        // Khai báo Tool Display
        private CogDisplay cogDisplayMain = null;
        /// <summary>
        /// Trả về tool Display
        /// </summary>
        public CogDisplay CogDisplayMain
        {
            get { return cogDisplayMain; }
            set { cogDisplayMain = value; }
        }
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

            /// Link ảnh đầu ra với ảnh đầu vào
            /// => Cần update thành Link với đầu vào Tool Align
            cogDisplayMain.DataBindings.Add("Image", AcqFifoTool.Subject, "OutputImage", true);

            /// Khai báo tool Align. Mặc định link đầu vào ảnh với Tool Acq
            pmAlignTool = new CogPMAlignEditV2();
            pmAlignTool.Subject = new CogPMAlignTool();
            pmAlignTool.Subject.DataBindings.Add("InputImage", AcqFifoTool.Subject, "OutputImage");

            // Khai báo tool Calib. Đầu vào ảnh từ Tool Acq
            calibGribCBTool = new CogCalibCheckerboardEditV2();
            calibGribCBTool.Subject = new CogCalibCheckerboardTool();
            calibGribCBTool.Subject.DataBindings.Add("InputImage", AcqFifoTool.Subject, "OutputImage");

            // Khai báo Event xử lý sự thay đổi của các Tool
            calibGribCBTool.Subject.Calibration.Changed += UpdatePMAlignImageSource;
            pmAlignTool.SubjectChanged += UpdatePMAlignImageSource;
            calibGribCBTool.SubjectChanged += UpdateImageSource;
        }

        /// <summary>
        /// Cập nhật đầu vào ảnh của Tool Align khi có sự thay đổi của Tool Calib ở trước nó
        /// Mặc định khi chưa Calib, đầu vào lấy trực tiếp từ Tool Acq
        /// Sau khi Calib, đầu vào lấy từ Tool Calib
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdatePMAlignImageSource(object sender, EventArgs e)
        {
            if (calibGribCBTool.Subject.Calibration.Calibrated) // Nếu đã calib
            {
                if (pmAlignTool.Subject.DataBindings.Contains("InputImage"))
                    pmAlignTool.Subject.DataBindings.Remove("InputImage");
                pmAlignTool.Subject.DataBindings.Add("InputImage", calibGribCBTool.Subject, "OutputImage");
            }
            else // Nếu chưa calib thì lấy ảnh trực tiếp từ nguồn
            {
                if (pmAlignTool.Subject.DataBindings.Contains("InputImage"))
                    pmAlignTool.Subject.DataBindings.Remove("InputImage");
                pmAlignTool.Subject.DataBindings.Add("InputImage", AcqFifoTool.Subject, "OutputImage");
            }
            // Cập nhật Binding ảnh đầu ra Display
            //cogDisplayMain.DataBindings.Add("Image", AcqFifoTool.Subject, "OutputImage", true);
        }

        /// <summary>
        /// Cập nhật đầu vào ảnh của Tool Calib khi thay đổi Tool.
        /// Đồng thời cập nhật lại Event của Tool 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateImageSource(object sender, EventArgs e)
        {
            if (calibGribCBTool.Subject.DataBindings.Contains("InputImage")) calibGribCBTool.Subject.DataBindings.Remove("InputImage");
            calibGribCBTool.Subject.DataBindings.Add("InputImage", AcqFifoTool.Subject, "OutputImage");
            // Event khi thay đổi chế độ Calib
            calibGribCBTool.Subject.Calibration.Changed += UpdatePMAlignImageSource;
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

            if (ImageFileEdit.Subject != null) ImageFileEdit.Subject.Run();
            if (cogAcqFifoEdit.Subject != null) cogAcqFifoEdit.Subject.Run();
            if (!calibGribCBTool.Subject.Calibration.Calibrated) MessageBox.Show("Image Not Calibration!!!");
            else calibGribCBTool.Subject.Run();
            if (pmAlignTool.Subject != null) pmAlignTool.Subject.Run();

            /// Xử lý kết quả trả về
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
                    listPatterns[index] = new patternObject { X = tempResult.GetPose().TranslationX, Y = tempResult.GetPose().TranslationY, Angle = tempResult.GetPose().Rotation * 180 / Math.PI };
                    index += 1;
                }
                listPatterns = ToolSupport.SortPatterns(listPatterns);
                foreach (var item in listPatterns)
                {
                    Console.WriteLine($"X: {item.X} Y: {item.Y} Angle: {item.Angle}");
                }
                #endregion

                return returnString;
            }
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
            if (!File.Exists(url + "\\AqcTool.vpp")) return;
            if (!File.Exists(url + "\\CalibTool.vpp")) return;
            if (!File.Exists(url + "\\PMAlignTool.vpp")) return;
            AcqFifoTool.Subject = CogSerializer.LoadObjectFromFile(url + "\\AqcTool.vpp") as CogAcqFifoTool;
            CalibGridCBTool.Subject = CogSerializer.LoadObjectFromFile(url + "\\CalibTool.vpp") as CogCalibCheckerboardTool;
            PMAlignTool.Subject = CogSerializer.LoadObjectFromFile(url + "\\PMAlignTool.vpp") as CogPMAlignTool;
        }

        /// <summary>
        /// Lưu chương trình Camera hiện tại theo đường dẫn được nhập vào
        /// Thông báo Message đã hoàn thành xong
        /// </summary>
        /// <param name="url"></param>
        public void SaveJob(string url)
        {
            CogSerializer.SaveObjectToFile(AcqFifoTool.Subject as CogAcqFifoTool, url + "\\AqcTool.vpp");
            CogSerializer.SaveObjectToFile(CalibGridCBTool.Subject as CogCalibCheckerboardTool, url + "\\CalibTool.vpp");
            CogSerializer.SaveObjectToFile(PMAlignTool.Subject as CogPMAlignTool, url + "\\PMAlignTool.vpp");
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
        /// Cập nhật nguồn ảnh cho tool Align khi Mode Input Image thay đổi
        /// </summary>
        private void UpdateBindingInputImage()
        {
            // Remove and Update
            switch (imageInputMode)
            {
                case 0:
                    break;
                case 1:
                    break;
                default:
                    break;
            }
        }
    }
}
