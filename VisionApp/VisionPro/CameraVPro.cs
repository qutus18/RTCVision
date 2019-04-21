using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.ImageFile;
using Cognex.VisionPro.PMAlign;
using Cognex.VisionPro.CalibFix;
using System;
using System.Windows;
using System.IO;
using System.Runtime.Serialization;

namespace VisionApp.VisionPro
{
    public class CameraVPro
    {
        #region Khai báo
        public CogAcqFifoEditV2 CogAcqFifoEdit { get; set; }
        public CogCalibCheckerboardEditV2 CogCalibGrid { get; set; }
        public CogPMAlignEditV2 CogPMAlign { get; set; }
        public AutoCalibTool RTCAutoCalibTool { get; set; }
        public InspectionTool RTCInspectionTool { get; set; }
        public CogDisplay CogDisplayOut { get; set; }
        public bool AutoCalibRunning { get; set; }
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

            // 

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
                    PointWithTheta tempPoint = CalculateAlignRB();
                    if (tempPoint != null)
                    {
                        string tempMessage = Helper.CreatXTMessage(tempPoint);
                    }
                    else return "XT,0";
                    return "XT,1";
                default:
                    break;
            }
            return "HE,1";
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
                    //CogDisplayOut.Image = CogAcqFifoEdit.Subject.OutputImage;
                    //CogDisplayOut.StaticGraphics.Add(CogPMAlign.Subject.Results[0].CreateResultGraphics(CogPMAlignResultGraphicConstants.CoordinateAxes), "");
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
    }
}
