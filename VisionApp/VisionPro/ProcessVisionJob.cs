using Cognex.VisionPro;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.ToolBlock;
using Cognex.VisionPro.ToolGroup;
using Cognex.VisionPro.Blob;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.CalibFix;
using System;
using System.Windows;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace VisionApp
{
    public class VisionJob
    {
        private string _pathToolBlock = "";
        private string resultRunJob = "";
        private CogToolBlockEditV2 toolBlockEdit = null;
        private CogToolGroupEditV2 toolGroupEdit = null;
        private CogImageFileTool imageFileTool = null;
        private CogImageFileEditV2 imageFileEdit = null;
        private CogDisplay cogDisplayMain = null;
        private CogPMAlignEditV2 pmAlignTool = null;
        private CogCalibCheckerboardEditV2 calibGribCBTool = null;
        private CogAcqFifoEditV2 acqFifoTool = null;
        private patternObject[] listPatterns = new patternObject[20];


        public VisionJob()
        {
            toolBlockEdit = new CogToolBlockEditV2();
            toolGroupEdit = new CogToolGroupEditV2();

            // Temp hiển thị tool Imagefile Select
            imageFileTool = new CogImageFileTool();
            imageFileEdit = new CogImageFileEditV2();
            imageFileEdit.Subject = imageFileTool;

            // Thiết lập Camera đầu vào
            acqFifoTool = new CogAcqFifoEditV2();
            acqFifoTool.Subject = new CogAcqFifoTool();


            // Load thư viện ảnh mặc định
            cogDisplayMain = new CogDisplay();
            ImageFileTool.Operator.Open(@"C:\Program Files\Cognex\VisionPro\Images\CheckCal.idb", CogImageFileModeConstants.Read);

            // Link ảnh đầu ra với ảnh đầu vào
            // Tắt tạm thời ảnh vào từ Imagefile
            // Load ảnh trực tiếp từ Camera
            //cogDisplayMain.DataBindings.Add("Image", ImageFileTool, "OutputImage", true);
            cogDisplayMain.DataBindings.Add("Image", AcqFifoTool.Subject, "OutputImage", true);

            // Tool Align
            pmAlignTool = new CogPMAlignEditV2();
            pmAlignTool.Subject = new CogPMAlignTool();
            //pmAlignTool.Subject.DataBindings.Add("InputImage", ImageFileTool, "OutputImage");
            pmAlignTool.Subject.DataBindings.Add("InputImage", AcqFifoTool.Subject, "OutputImage");

            // Cấu hình Tool Calib
            calibGribCBTool = new CogCalibCheckerboardEditV2();
            calibGribCBTool.Subject = new CogCalibCheckerboardTool();
            // Sửa đầu vào Tool Calib
            //calibGribCBTool.Subject.DataBindings.Add("InputImage", ImageFileTool, "OutputImage");
            calibGribCBTool.Subject.DataBindings.Add("InputImage", AcqFifoTool.Subject, "OutputImage");

            calibGribCBTool.Subject.Calibration.Changed += UpdateCalibImage;
            pmAlignTool.SubjectChanged += UpdateCalibImage;
            calibGribCBTool.SubjectChanged += UpdateImageSource;

            // 

        }

        private void UpdateCalibImage(object sender, EventArgs e)
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
                pmAlignTool.Subject.DataBindings.Add("InputImage", ImageFileTool, "OutputImage");
            }
        }

        private void UpdateImageSource(object sender, EventArgs e)
        {
            if (calibGribCBTool.Subject.DataBindings.Contains("InputImage")) calibGribCBTool.Subject.DataBindings.Remove("InputImage");
            calibGribCBTool.Subject.DataBindings.Add("InputImage", AcqFifoTool.Subject, "OutputImage");

            calibGribCBTool.Subject.Calibration.Changed += UpdateCalibImage;
        }

        public CogCalibCheckerboardEditV2 CalibGridCBTool
        {
            get { return calibGribCBTool; }
            set { calibGribCBTool = value; }
        }

        public CogAcqFifoEditV2 AcqFifoTool
        {
            get { return acqFifoTool; }
            set { acqFifoTool = value; }
        }


        /// <summary>
        /// Trả về hàm PMAlign
        /// </summary>
        public CogPMAlignEditV2 PMAlignTool
        {
            get { return pmAlignTool; }
            set { pmAlignTool = value; }
        }

        /// <summary>
        /// Trả về hàm hiển thị ảnh
        /// </summary>
        public CogDisplay CogDisplayMain
        {
            get { return cogDisplayMain; }
            set { cogDisplayMain = value; }
        }

        /// <summary>
        /// Trả về control ImageFileEdit tool
        /// </summary>
        public CogImageFileEditV2 ImageFileEdit
        {
            get { return imageFileEdit; }
            set { imageFileEdit = value; }
        }


        /// <summary>
        /// Trả về ImageFileTool
        /// </summary>
        public CogImageFileTool ImageFileTool
        {
            get { return imageFileTool; }
            set { imageFileTool = value; }
        }

        /// <summary>
        /// Đặt giá trị đường dẫn cho ToolBlock Vision
        /// </summary>
        public string PathToolBlock
        {
            set
            {
                _pathToolBlock = value;
                toolBlockEdit.Subject = new CogToolBlock();
                toolGroupEdit.Subject = CogSerializer.LoadObjectFromFile(_pathToolBlock) as CogToolBlock;
            }
            get
            {
                return _pathToolBlock;
            }
        }

        /// <summary>
        /// Trả về ToolBlock
        /// </summary>
        public CogToolBlockEditV2 ToolBlockEdit
        {
            get { return toolBlockEdit; }
            private set { }
        }

        /// <summary>
        /// Trả về ToolGroup
        /// </summary>
        public CogToolGroupEditV2 ToolGroupEdit
        {
            get { return toolGroupEdit; }
            private set { }
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
            if (toolBlockEdit.Subject != null) toolBlockEdit.Subject.Run();
            if (toolGroupEdit.Subject != null) toolGroupEdit.Subject.Run();
            if (ImageFileTool != null) ImageFileTool.Run();
            if (acqFifoTool.Subject != null) acqFifoTool.Subject.Run();
            if (!calibGribCBTool.Subject.Calibration.Calibrated) MessageBox.Show("Image Not Calibration!!!");
            else calibGribCBTool.Subject.Run();
            if (pmAlignTool.Subject != null) pmAlignTool.Subject.Run();
            if (pmAlignTool.Subject.Results.Count > 0)
            {
                temp = pmAlignTool.Subject.Results[0].GetPose();
            }
            if (temp != null)
            {
                foreach (var item in pmAlignTool.Subject.Results)
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
                foreach (var item in PMAlignTool.Subject.Results)
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

                return returnString;
            }
            
            return "Fail";
        }

        /// <summary>
        /// Load CameraJob Form Url
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
        /// Save CameraJob to Url
        /// </summary>
        /// <param name="url"></param>
        public void SaveJob(string url)
        {
            CogSerializer.SaveObjectToFile(AcqFifoTool.Subject as CogAcqFifoTool, url + "\\AqcTool.vpp");
            CogSerializer.SaveObjectToFile(CalibGridCBTool.Subject as CogCalibCheckerboardTool, url + "\\CalibTool.vpp");
            CogSerializer.SaveObjectToFile(PMAlignTool.Subject as CogPMAlignTool, url + "\\PMAlignTool.vpp");
            MessageBox.Show("Save Job Done! :)");
        }
    }
}
