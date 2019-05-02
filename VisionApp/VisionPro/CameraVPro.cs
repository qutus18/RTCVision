using Cognex.VisionPro;

using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.CalibFix;
using System;
using System.Windows;
using System.IO;
using System.Runtime.Serialization;
using System.Drawing;
using System.Threading;
using System.Windows.Threading;

namespace VisionApp.VisionPro
{
    public class CameraVPro : ObservableObject
    {
        #region Khai báo
        public CogAcqFifoEditV2 CogAcqFifoEdit { get; set; }
        public CogCalibCheckerboardEditV2 CogCalibGrid { get; set; }
        public CogPMAlignEditV2 CogPMAlign { get; set; }
        public AutoCalibTool RTCAutoCalibTool { get; set; }
        public InspectionTool RTCInspectionTool { get; set; }
        public CogDisplay CogDisplayOut { get; set; }
        public bool AutoCalibRunning { get; set; }
        private int currentCameraIndex;
        public int CurrentCameraIndex
        {
            get { return currentCameraIndex; }
            set
            {
                currentCameraIndex = value;
                OnPropertyChanged("CurrentCameraIndex");
            }
        }
        #endregion

        public CameraVPro()
        {
            // Thiết lập Camera đầu vào
            CogAcqFifoEdit = new CogAcqFifoEditV2();
            CogAcqFifoEdit.Subject = new CogAcqFifoTool();

            // Khai báo tool Calib. Đầu vào ảnh từ Tool Acq.
            CogCalibGrid = new CogCalibCheckerboardEditV2();
            CogCalibGrid.Subject = new CogCalibCheckerboardTool();

            // Khai báo hiển thị đầu ra
            CogDisplayOut = new CogDisplay();



            // Khai báo tool Align. Mặc định link đầu vào ảnh với Tool Acq
            CogPMAlign = new CogPMAlignEditV2();
            CogPMAlign.Subject = new CogPMAlignTool();

            // Khai báo Tool AutoCalib
            RTCAutoCalibTool = new AutoCalibTool();
            // Khởi tạo Autocalib
            AutoCalibRunning = false;

            // Khởi tạo CameraIndex
            currentCameraIndex = -1;

        }

        /// <summary>
        /// Xử lý Command nhận vào từ cổng TCP
        /// </summary>
        /// <param name="cmd"></param>
        public string Command(string cmd)
        {
            Console.WriteLine(cmd);
            switch (Helper.GetCommandId(cmd))
            {
                case "HEB":
                    string result = CheckConditionStartAutoCalib();
                    if (result == "OK")
                    {
                        RTCAutoCalibTool.ResetReceiveData();
                        AutoCalibRunning = true;
                    }
                    else
                    {
                        MessageBox.Show("Không đủ điều kiện chạy!\r\n" + result);
                        return "HE,0";
                    }
                    break;
                case "HE":
                    PointWithTheta tempCamPoint = GetNormalAlign();
                    PointWithTheta tempRobotPoint = Helper.GetRobotPointFromCmd(cmd);
                    if (tempCamPoint != null) RTCAutoCalibTool.AddPoint(tempCamPoint, tempRobotPoint);
                    else return "HE,0";
                    break;
                case "HEE":
                    if (RTCAutoCalibTool.NumberPoints < 11)
                    {
                        MessageBox.Show("Not Enough Calib Point! N = " + RTCAutoCalibTool.NumberPoints.ToString());
                        return "HE,0";
                    }
                    if (RTCAutoCalibTool.Calculate())
                    {
                        MessageBox.Show("AutoCalib Done!");
                        AutoCalibRunning = false;
                    }
                    else
                    {
                        MessageBox.Show("AutoCalib Fail!");
                        AutoCalibRunning = false;
                        return "HE,0";
                    }
                    break;
                case "TT":
                    var tempPointTT = GetNormalAlign();
                    if (tempPointTT != null)
                    {
                        RTCAutoCalibTool.CalTTTransMatrix(cmd, tempPointTT);
                    }
                    return "TT,1";
                case "XT":
                    string tempMessage;
                    PointWithTheta tempPoint = CalculateAlignRB();
                    if (tempPoint != null)
                    {
                        tempMessage = Helper.CreatXTMessage(tempPoint);
                    }
                    else return "XT,0";
                    return tempMessage;
                default:
                    break;
            }
            return "HE,1";
        }

        /// <summary>
        /// Kiểm tra Camera load xong
        /// </summary>
        /// <returns></returns>
        public bool Loaded()
        {
            return (CogAcqFifoEdit.Created && CogCalibGrid.Created && CogPMAlign.Created && CogDisplayOut.Created);
        }

        public bool TrainPattern()
        {
            CogAcqFifoEdit.Subject.Run();
            // Chụp ảnh, gửi ảnh sang Tool Calib
            if (CogAcqFifoEdit.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogAcqFifoEdit.Subject.RunStatus.Exception.Message);
                return false;
            }
            else
            {
                CogCalibGrid.Subject.InputImage = CogAcqFifoEdit.Subject.OutputImage;
            }
            CogCalibGrid.Subject.Run();
            // Calib xong gửi ảnh qua Tool Align
            if (CogCalibGrid.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogCalibGrid.Subject.RunStatus.Exception.Message);
                return false;
            }
            else
            {
                CogPMAlign.Subject.InputImage = CogCalibGrid.Subject.OutputImage;
            }
            CogPMAlign.Subject.Pattern.TrainImage = CogCalibGrid.Subject.OutputImage;
            try
            {
                CogPMAlign.Subject.Pattern.Train();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
            return true;
        }

        /// <summary>
        /// Chạy Tool Align, trả về tọa độ + góc (PointWithTheta)
        /// </summary>
        /// <returns></returns>
        public PointWithTheta GetNormalAlign()
        {
            CogAcqFifoEdit.Subject.Run();
            // Chụp ảnh, gửi ảnh sang Tool Calib
            if (CogAcqFifoEdit.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogAcqFifoEdit.Subject.RunStatus.Exception.Message);
                return null;
            }
            else
            {
                CogCalibGrid.Subject.InputImage = CogAcqFifoEdit.Subject.OutputImage;
            }
            CogCalibGrid.Subject.Run();
            // Calib xong gửi ảnh qua Tool Align
            if (CogCalibGrid.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogCalibGrid.Subject.RunStatus.Exception.Message);
                return null;
            }
            else
            {
                CogPMAlign.Subject.InputImage = CogCalibGrid.Subject.OutputImage;
            }
            CogPMAlign.Subject.Run();
            // Chạy xong Tool Align trả tọa độ + góc ra đầu ra
            if (CogPMAlign.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogPMAlign.Subject.RunStatus.Exception.Message);
                return null;
            }
            else
            {
                if (CogPMAlign.Subject.Results.Count > 0)
                {
                    float tempX = (float)CogPMAlign.Subject.Results[0].GetPose().TranslationX;
                    float tempY = (float)CogPMAlign.Subject.Results[0].GetPose().TranslationY;
                    float tempTheta = (float)CogPMAlign.Subject.Results[0].GetPose().Rotation;
                    // Trả hiển thị ra CogDisplay
                    App.Current.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        CogDisplayOut.Image = CogAcqFifoEdit.Subject.OutputImage;
                        CogDisplayOut.StaticGraphics.Add(CogPMAlign.Subject.Results[0].CreateResultGraphics(CogPMAlignResultGraphicConstants.CoordinateAxes), "");
                    }));
                    Console.WriteLine($"{tempX}, {tempY}, {tempTheta}");
                    return new PointWithTheta(tempX, tempY, tempTheta);
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Kiểm tra điều kiện bắt đầu chạy Autocalib
        /// 1. Đã khai báo Camera
        /// 2. Đã Calib Grid
        /// 3. Đã train xong Pattern
        /// </summary>
        /// <returns></returns>
        private string CheckConditionStartAutoCalib()
        {
            return "OK";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public PointWithTheta CalculateAlignRB()
        {
            PointWithTheta outputAlign = null;

            CogAcqFifoEdit.Subject.Run();
            // Chụp ảnh, gửi ảnh sang Tool Calib
            if (CogAcqFifoEdit.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogAcqFifoEdit.Subject.RunStatus.Exception.Message);
                return null;
            }
            else
            {
                CogCalibGrid.Subject.InputImage = CogAcqFifoEdit.Subject.OutputImage;
            }
            CogCalibGrid.Subject.Run();
            // Calib xong gửi ảnh qua Tool Align
            if (CogCalibGrid.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogCalibGrid.Subject.RunStatus.Exception.Message);
                return null;
            }
            else
            {
                CogPMAlign.Subject.InputImage = CogCalibGrid.Subject.OutputImage;
            }
            CogPMAlign.Subject.Run();
            // Chạy xong Tool Align trả tọa độ + góc ra đầu ra
            if (CogPMAlign.Subject.RunStatus.Result != CogToolResultConstants.Accept)
            {
                MessageBox.Show(CogPMAlign.Subject.RunStatus.Exception.Message);
                return null;
            }
            else
            {
                DisplayGraphic();
                try
                {
                    float tempX = (float)CogPMAlign.Subject.Results[0].GetPose().TranslationX;
                    float tempY = (float)CogPMAlign.Subject.Results[0].GetPose().TranslationY;
                    float tempTheta = (float)CogPMAlign.Subject.Results[0].GetPose().Rotation;
                    // Trả hiển thị ra CogDisplay
                    //CogDisplayOut.Image = CogAcqFifoEdit.Subject.OutputImage;
                    //CogDisplayOut.StaticGraphics.Add(CogPMAlign.Subject.Results[0].CreateResultGraphics(CogPMAlignResultGraphicConstants.CoordinateAxes), "");
                    outputAlign = new PointWithTheta(tempX, tempY, tempTheta);
                }
                catch
                {
                    outputAlign = null;
                }
            }
            if (outputAlign != null)
            {
                return RTCAutoCalibTool.Trans(outputAlign);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        private void DisplayGraphic()
        {
            App.Current.Dispatcher.BeginInvoke(new Action(delegate
            {
                float alignX = -1;
                float alignY = -1;
                float rotation = -1;
                CogDisplayOut.Image = CogAcqFifoEdit.Subject.OutputImage;
                CogDisplayOut.StaticGraphics.Clear();
                //
                if (CogPMAlign.Subject.Results.Count > 0)
                {
                    var AlignGraphic = CogPMAlign.Subject.Results[0].CreateResultGraphics(CogPMAlignResultGraphicConstants.CoordinateAxes);
                    AlignGraphic.Color = CogColorConstants.DarkGreen;
                    CogDisplayOut.StaticGraphics.Add(AlignGraphic, "");
                    alignX = (float)CogPMAlign.Subject.Results[0].GetPose().TranslationX;
                    alignY = (float)CogPMAlign.Subject.Results[0].GetPose().TranslationY;
                    rotation = (float)CogPMAlign.Subject.Results[0].GetPose().Rotation;
                    //
                }
                // Static Graphic
                CogLine GraphicTest_X = new CogLine();
                GraphicTest_X.LineStyle = CogGraphicLineStyleConstants.Solid;
                GraphicTest_X.X = CogCalibGrid.Subject.Calibration.GetComputedUncalibratedFromCalibratedTransform().LinearTransform(0, 0).TranslationX;
                GraphicTest_X.Y = CogCalibGrid.Subject.Calibration.GetComputedUncalibratedFromCalibratedTransform().LinearTransform(0, 0).TranslationY;
                GraphicTest_X.Rotation = 0;
                CogLine GraphicTest_Y = new CogLine();
                GraphicTest_Y.LineStyle = CogGraphicLineStyleConstants.Solid;
                GraphicTest_Y.X = CogCalibGrid.Subject.Calibration.GetComputedUncalibratedFromCalibratedTransform().LinearTransform(0, 0).TranslationX;
                GraphicTest_Y.Y = CogCalibGrid.Subject.Calibration.GetComputedUncalibratedFromCalibratedTransform().LinearTransform(0, 0).TranslationY;
                GraphicTest_Y.Rotation = Math.PI / 2;
                GraphicTest_X.Color = CogColorConstants.DarkRed;
                GraphicTest_Y.Color = CogColorConstants.DarkRed;
                CogDisplayOut.StaticGraphics.Add(GraphicTest_X, "Test");
                CogDisplayOut.StaticGraphics.Add(GraphicTest_Y, "Test");
                //
                CogGraphicLabel cogGraphicLabelTest = new CogGraphicLabel();
                cogGraphicLabelTest.SetXYText(10, 10, $"RTC Camera 0   X = {alignX}  Y = {alignY}   Angle = {rotation}");
                cogGraphicLabelTest.Font = new Font(FontFamily.GenericSansSerif, 10, System.Drawing.FontStyle.Bold);
                cogGraphicLabelTest.Color = CogColorConstants.White;
                cogGraphicLabelTest.BackgroundColor = CogColorConstants.Black;
                cogGraphicLabelTest.Alignment = CogGraphicLabelAlignmentConstants.TopLeft;
                CogDisplayOut.StaticGraphics.Add(cogGraphicLabelTest, "Test");
                // 
                if (CogPMAlign.Subject.SearchRegion != null)
                {
                    var SearchRegion = (CogPMAlign.Subject.SearchRegion.Map(CogPMAlign.Subject.InputImage.GetTransform("@", "."), CogCopyShapeConstants.All) as CogRectangleAffine);
                    SearchRegion.Color = CogColorConstants.Orange;
                    CogDisplayOut.StaticGraphics.Add(SearchRegion, "Test");
                }


                CogDisplayOut.Fit();
            }));
        }

        /// <summary>
        /// Lưu Tool Camera theo Index
        /// </summary>
        /// <param name="CameraIndex"></param>
        /// <returns></returns>
        public bool Save(int CameraIndex)
        {
            string[] writeStrings = new string[10];
            string urlTool = Helper.CreatDirectionCameraVpro(CameraIndex);
            if (!Directory.Exists(urlTool)) Directory.CreateDirectory(urlTool);
            try
            {
                //
                Cognex.VisionPro.CogSerializer.SaveObjectToFile(CogAcqFifoEdit.Subject as CogAcqFifoTool, urlTool + "\\CogAcqFifoEdit.vpp");
                Cognex.VisionPro.CogSerializer.SaveObjectToFile(CogCalibGrid.Subject as CogCalibCheckerboardTool, urlTool + "\\CogCalibGrid.vpp");
                Cognex.VisionPro.CogSerializer.SaveObjectToFile(CogPMAlign.Subject as CogPMAlignTool, urlTool + "\\CogPMAlign.vpp");
                //
                RTCAutoCalibTool.Save(CameraIndex);
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Load Camera theo Index
        /// </summary>
        /// <param name="CameraIndex"></param>
        /// <returns></returns>
        public bool Load(int CameraIndex)
        {
            currentCameraIndex = CameraIndex;
            string[] writeStrings = new string[10];
            string urlTool = Helper.CreatDirectionCameraVpro(CameraIndex);
            if (!Directory.Exists(urlTool)) CopyTemplateCamera(urlTool);
            try
            {
                CogAcqFifoEdit.Subject = (CogAcqFifoTool)Cognex.VisionPro.CogSerializer.LoadObjectFromFile(urlTool + "\\CogAcqFifoEdit.vpp");
                CogCalibGrid.Subject = (CogCalibCheckerboardTool)Cognex.VisionPro.CogSerializer.LoadObjectFromFile(urlTool + "\\CogCalibGrid.vpp");
                CogPMAlign.Subject = (CogPMAlignTool)Cognex.VisionPro.CogSerializer.LoadObjectFromFile(urlTool + "\\CogPMAlign.vpp");
                RTCAutoCalibTool.Load(CameraIndex);
                currentCameraIndex = CameraIndex;
                //
                CogDisplayOut.BackColor = System.Drawing.ColorTranslator.FromHtml("#FF394261");
                CogDisplayOut.HorizontalScrollBar = false;
                CogDisplayOut.VerticalScrollBar = false;

            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Copy thư mục camera template sang thư mục camera index
        /// </summary>
        /// <param name="urlTool"></param>
        private void CopyTemplateCamera(string urlTool)
        {
            string templateUrl = Settings.Default.UrlTemplate;
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(templateUrl, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(templateUrl, urlTool));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(templateUrl, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(templateUrl, urlTool), true);
        }

        public void Close()
        {
            CogAcqFifoEdit.Subject.Dispose();
            CogAcqFifoEdit.Dispose();
        }
    }
}
