using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.CalibFix;
using System;
using System.Windows;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace VisionApp
{
    public class VisionAppObject : ObservableObject
    {
        // Khai báo đầu vào Image
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
        // Khai báo tool Calib
        private CogCalibCheckerboardEditV2 calibGribCBTool = null; 
        public CogCalibCheckerboardEditV2 CalibGridCBTool
        {
            get { return calibGribCBTool; }
            set { calibGribCBTool = value; }
        }
        // Khai báo tool Align
        private CogPMAlignEditV2 pmAlignToolEdit = null;
        public CogPMAlignEditV2 PMAlignToolEdit
        {
            get { return pmAlignToolEdit; }
            set { pmAlignToolEdit = value; }
        }
        // Khai báo tool Display 
        private CogDisplay cogDisplayMain = null;
        public CogDisplay CogDisplayMain
        {
            get { return cogDisplayMain; }
            set { cogDisplayMain = value; }
        }
        // Khai báo Pattern trả về mảng 20 phần tử
        private patternObject[] listPatterns = new patternObject[20];
        // Khai báo Event
        public delegate void outputString(string oString);
        public event outputString outputStringEvent;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Khởi tạo
        /// </summary>
        public VisionAppObject()
        {
            // Temp hiển thị tool Imagefile Select
            cogImageFileTool = new CogImageFileEditV2();
            cogImageFileTool.Subject = new CogImageFileTool();

            // Thiết lập Camera đầu vào
            cogAcqFifoEdit = new CogAcqFifoEditV2();
            cogAcqFifoEdit.Subject = new CogAcqFifoTool();


            // Load thư viện ảnh mặc định
            cogDisplayMain = new CogDisplay();
            cogImageFileTool.Subject.Operator.Open(@"C:\Program Files\Cognex\VisionPro\Images\CheckCal.idb", CogImageFileModeConstants.Read);

            // Link ảnh đầu ra với ảnh đầu vào
            // Tắt tạm thời ảnh vào từ Imagefile
            // Load ảnh trực tiếp từ Camera
            //cogDisplayMain.DataBindings.Add("Image", ImageFileTool, "OutputImage", true);
            cogDisplayMain.DataBindings.Add("Image", CogAcqFifoEdit.Subject, "OutputImage", true);

            // Tool Align
            pmAlignToolEdit = new CogPMAlignEditV2();
            pmAlignToolEdit.Subject = new CogPMAlignTool();
            //pmAlignTool.Subject.DataBindings.Add("InputImage", ImageFileTool, "OutputImage");
            pmAlignToolEdit.Subject.DataBindings.Add("InputImage", CogAcqFifoEdit.Subject, "OutputImage");

            // Cấu hình Tool Calib
            calibGribCBTool = new CogCalibCheckerboardEditV2();
            calibGribCBTool.Subject = new CogCalibCheckerboardTool();
            // Sửa đầu vào Tool Calib
            //calibGribCBTool.Subject.DataBindings.Add("InputImage", ImageFileTool, "OutputImage");
            calibGribCBTool.Subject.DataBindings.Add("InputImage", CogAcqFifoEdit.Subject, "OutputImage");

            calibGribCBTool.Subject.Calibration.Changed += UpdateCalibImage;
            pmAlignToolEdit.SubjectChanged += UpdateCalibImage;
            calibGribCBTool.SubjectChanged += UpdateImageSource;

            // 

        }

        /// <summary>
        /// Cập nhật Binding ảnh, cho các tool đằng sau tool Calib. 
        /// Nếu chưa calib lấy ảnh trực tiếp từ nguồn 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateCalibImage(object sender, EventArgs e)
        {
            if (calibGribCBTool.Subject.Calibration.Calibrated) // Nếu đã calib
            {
                if (pmAlignToolEdit.Subject.DataBindings.Contains("InputImage"))
                    pmAlignToolEdit.Subject.DataBindings.Remove("InputImage");
                pmAlignToolEdit.Subject.DataBindings.Add("InputImage", calibGribCBTool.Subject, "OutputImage");
            }
            else // Nếu chưa calib thì lấy ảnh trực tiếp từ nguồn
            {
                if (pmAlignToolEdit.Subject.DataBindings.Contains("InputImage"))
                    pmAlignToolEdit.Subject.DataBindings.Remove("InputImage");
                pmAlignToolEdit.Subject.DataBindings.Add("InputImage", cogImageFileTool.Subject, "OutputImage");
            }
        }

        /// <summary>
        /// Thay đổi binding ảnh đầu vào tool calib khi nguồn ảnh thay đổi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateImageSource(object sender, EventArgs e)
        {
            if (calibGribCBTool.Subject.DataBindings.Contains("InputImage")) calibGribCBTool.Subject.DataBindings.Remove("InputImage");
            calibGribCBTool.Subject.DataBindings.Add("InputImage", CogAcqFifoEdit.Subject, "OutputImage");
            calibGribCBTool.Subject.Calibration.Changed += UpdateCalibImage;
        }

        /// <summary>
        /// Cập nhật nguồn ảnh cho tool Align khi Mode Input Image thay đổi
        /// </summary>
        private void UpdateBindingInputImage()
        {
            pmAlignToolEdit.Subject.DataBindings.Remove("InputImage");
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
        /// Chạy lần lượt tất cả các Job
        /// Chuẩn bị Update: Chạy lần lượt Job theo Mode?
        /// Hiện tại sử dụng đầu vào Camera
        /// </summary>
        /// <returns></returns>
        public string RunJob()
        {
            string returnString = "";
            CogTransform2DLinear temp = null;
            if (cogImageFileTool.Subject != null) cogImageFileTool.Subject.Run();
            if (cogAcqFifoEdit.Subject != null) cogAcqFifoEdit.Subject.Run();
            if (!calibGribCBTool.Subject.Calibration.Calibrated) MessageBox.Show("Image Not Calibration!!!");
            else calibGribCBTool.Subject.Run();
            if (pmAlignToolEdit.Subject != null) pmAlignToolEdit.Subject.Run();
            if (pmAlignToolEdit.Subject.Results.Count > 0)
            {
                temp = pmAlignToolEdit.Subject.Results[0].GetPose();
            }
            if (temp != null)
            {
                foreach (var item in pmAlignToolEdit.Subject.Results)
                {
                    temp = (item as CogPMAlignResult).GetPose();
                    returnString += $"X : {temp.TranslationX.ToString("0.00")} - Y : {temp.TranslationY.ToString("0.00")} - Angle : {(temp.Rotation * 180 / Math.PI).ToString("0.00")}\r\n";
                }
                //return ($"X : {temp.TranslationX.ToString("0.00")} - Y : {temp.TranslationY.ToString("0.00")} - Angle : {(temp.Rotation * 180 / Math.PI).ToString("0.00")}");

                // Input pattern to Array
                listPatterns = new patternObject[20];
                for (int i = 0; i < listPatterns.Length; i++)
                {
                    listPatterns[i] = new patternObject();
                }

                int index = 0;
                foreach (var item in PMAlignToolEdit.Subject.Results)
                {
                    var tempResult = item as CogPMAlignResult;
                    listPatterns[index] = new patternObject { X = tempResult.GetPose().TranslationX, Y = tempResult.GetPose().TranslationY, Angle = tempResult.GetPose().Rotation * 180 / Math.PI };
                    index += 1;
                }

                listPatterns = ToolSupport.SortPatterns(listPatterns);

                // In ra màn hình list Pattern
                foreach (var item in listPatterns)
                {
                    Console.WriteLine($"X: {item.X} Y: {item.Y} Angle: {item.Angle}");
                }

                log.Info("Result Run Job :\r\n" + returnString);
                return returnString;
            }
            
            return "Fail";
        }

        /// <summary>
        /// Load CameraJob Form Url
        /// </summary>
        /// <param name="url"></param>
        public async Task LoadJob(string url)
        {
            if (!File.Exists(url + "\\AqcTool.vpp")) return;
            if (!File.Exists(url + "\\CalibTool.vpp")) return;
            if (!File.Exists(url + "\\PMAlignTool.vpp")) return;
            outputStringEvent?.Invoke("Loading " + url + "\\AqcTool.vpp");
            CogAcqFifoEdit.Subject = CogSerializer.LoadObjectFromFile(url + "\\AqcTool.vpp") as CogAcqFifoTool;
            outputStringEvent?.Invoke("Loading " + url + "\\CalibTool.vpp");
            CalibGridCBTool.Subject = CogSerializer.LoadObjectFromFile(url + "\\CalibTool.vpp") as CogCalibCheckerboardTool;
            outputStringEvent?.Invoke("Loading " + url + "\\PMAlignTool.vpp");
            PMAlignToolEdit.Subject = CogSerializer.LoadObjectFromFile(url + "\\PMAlignTool.vpp") as CogPMAlignTool;
            await Task.Delay(100);
        }

        /// <summary>
        /// Save CameraJob to Url
        /// </summary>
        /// <param name="url"></param>
        public void SaveJob(string url)
        {
            CogSerializer.SaveObjectToFile(CogAcqFifoEdit.Subject as CogAcqFifoTool, url + "\\AqcTool.vpp");
            CogSerializer.SaveObjectToFile(CalibGridCBTool.Subject as CogCalibCheckerboardTool, url + "\\CalibTool.vpp");
            CogSerializer.SaveObjectToFile(PMAlignToolEdit.Subject as CogPMAlignTool, url + "\\PMAlignTool.vpp");
            MessageBox.Show("Save Job Done! :)");
        }
    }
}
