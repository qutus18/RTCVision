using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Cognex.VisionPro;
using Cognex.VisionPro.CalibFix;

namespace VisionApp.VisionPro
{
    public class AutoCalibTool
    {
        #region Khai báo 
        public List<PointWithTheta> ListAutoCalibPointsRB { get; set; }
        public List<PointWithTheta> ListAutoCalibPointsCam { get; set; }
        public int NumberPoints { get; set; }
        public CogCalibNPointToNPoint CalibNPointToolRBCam { get; set; }
        //public CogCalibNPointToNPoint CalibNPointToolRamRB { get; set; }
        public Matrix4x4 TransMatrixPOROV { get; set; }
        public Matrix4x4 TransformTT { get; set; }
        public Matrix4x4 TransMatrixPBASE { get; set; }
        public ICogTransform2D PointTransformToolFromNPointCalib { get; private set; }
        public bool CalNpointOK { get; set; }
        public bool CalTTMatrixOK { get; set; }
        #endregion

        /// <summary>
        /// Hàm khởi tạo
        /// </summary>
        public AutoCalibTool()
        {
            ListAutoCalibPointsRB = new List<PointWithTheta>();
            ListAutoCalibPointsCam = new List<PointWithTheta>();
            CalibNPointToolRBCam = new CogCalibNPointToNPoint();
            PointTransformToolFromNPointCalib = null;
            CalNpointOK = false;
            CalTTMatrixOK = false;
            NumberPoints = 0;
        }

        /// <summary>
        /// Xóa hết dữ liệu điểm, khởi tạo lại về ban đầu
        /// </summary>
        public void ResetReceiveData()
        {
            ListAutoCalibPointsRB.Clear();
            ListAutoCalibPointsCam.Clear();
            CalibNPointToolRBCam = new CogCalibNPointToNPoint();
            PointTransformToolFromNPointCalib = null;
            CalNpointOK = false;
            CalTTMatrixOK = false;
            NumberPoints = 0;
        }

        /// <summary>
        /// Xử lý thêm điểm vào list từ lệnh HE nhận từ Robot
        /// </summary>
        /// <param name="tempCamPoint"></param>
        /// <param name="tempRobotPoint"></param>
        public void AddPoint(PointWithTheta tempCamPoint, PointWithTheta tempRobotPoint)
        {
            ListAutoCalibPointsRB.Add(tempRobotPoint);
            ListAutoCalibPointsCam.Add(tempCamPoint);
            NumberPoints += 1;
        }

        /// <summary>
        /// Tính toán AutoCalib
        /// </summary>
        /// <returns></returns>
        public bool Calculate()
        {
            // Tính toán ma trận chuyển hệ Robot sang hệ Camera POROV TransformMatrixPOROV
            var tempArrMatrix = TransformMatrixCal.CalPBaseAndPOROC(ListAutoCalibPointsRB[9], ListAutoCalibPointsRB[10], ListAutoCalibPointsCam[9], ListAutoCalibPointsCam[10]);
            TransMatrixPOROV = tempArrMatrix[1];
            TransMatrixPBASE = tempArrMatrix[0];
            if (TransMatrixPOROV == null) return false;

            // Chuyển đổi sang ma trận Robot trên hệ tọa độ Camera
            List<PointWithTheta> ListAutoCalibPointsRB_OCam = Helper.CalTransRobotToOCam(ListAutoCalibPointsRB, TransMatrixPOROV, TransMatrixPBASE);

            // Thêm điểm vào Tool Calib N Point, tính toán trả về Tool chuyển đổi điểm qua N Point 
            if (NumberPoints >= 11)
            {
                for (int i = 0; i < 9; i++)
                {
                    CalibNPointToolRBCam.AddPointPair(ListAutoCalibPointsRB_OCam[i].X, ListAutoCalibPointsRB_OCam[i].Y, ListAutoCalibPointsCam[i].X, ListAutoCalibPointsCam[i].Y);
                }
                CalibNPointToolRBCam.Calibrate();
                if (!CalibNPointToolRBCam.Calibrated)
                {
                    MessageBox.Show("N Point Calib Fail!");
                    return false;
                }
                else
                {
                    PointTransformToolFromNPointCalib = CalibNPointToolRBCam.GetComputedUncalibratedFromCalibratedTransform();
                    CalNpointOK = true;
                }
            }
            return true;
        }

        /// <summary>
        /// Tool chuyển đổi tọa độ Align trả về từ Camera thành tọa độ Robot
        /// Nếu chưa đủ điều kiện trả về null
        /// </summary>
        /// <param name="outputAlign"></param>
        /// <returns></returns>
        public PointWithTheta Trans(PointWithTheta outputAlign)
        {
            if (CalNpointOK && CalTTMatrixOK)
            {
                PointWithTheta inputPointOCam = Helper.TransPointFromNPoint(PointTransformToolFromNPointCalib, outputAlign);
                PointWithTheta outputRBPoint = Helper.CalTransAlignToRobot(inputPointOCam, TransformTT, TransMatrixPOROV);
                return outputRBPoint;
            }
            else return null;
        }

        /// <summary>
        /// Tính toán TT
        /// Lấy điểm Input từ Robot
        /// Chuyển đổi từ điểm Align Camera qua Tool chuyển NPoint
        /// Tính toán từ 2 điểm trên ra ma trận TT
        /// </summary>
        /// <param name="cmd"></param>
        public void CalTTTransMatrix(string cmd, PointWithTheta alignPoint)
        {
            PointWithTheta inputPoint = Helper.GetRobotPointFromCmd(cmd);
            PointWithTheta alignPointOCam = Helper.TransPointFromNPoint(PointTransformToolFromNPointCalib, alignPoint);
            TransformTT = TransformTTCal.Calculate(inputPoint, alignPointOCam, TransMatrixPOROV);
            CalTTMatrixOK = true;
        }

        /// <summary>
        /// Lưu Tool
        /// </summary>
        /// <param name="CameraIndex"></param>
        public void Save(int CameraIndex)
        {
            string[] writeStrings = new string[10];
            string urlTool = Helper.CreatDirectionAutoCalib(CameraIndex);
            //
            if (!Directory.Exists(urlTool)) Directory.CreateDirectory(urlTool);
            //
            writeStrings[0] = "RTC Technology - Autocalib Tool";
            writeStrings[1] = "-------------------------------";
            writeStrings[2] = $"CalNpointOK,{CalNpointOK}";
            writeStrings[3] = $"CalTTMatrixOK,{CalTTMatrixOK}";
            File.WriteAllLines(urlTool + "\\info.txt", writeStrings, Encoding.ASCII);
            //
            Cognex.VisionPro.CogSerializer.SaveObjectToFile(CalibNPointToolRBCam, urlTool + "\\CalibNPointToolRBCam.vpp");
            //
            Helper.SaveAutoCalibMatrix(urlTool, TransMatrixPBASE, TransformTT, TransMatrixPOROV);
        }

        /// <summary>
        /// Load Tool AutoCalib theo CameraIndex
        /// </summary>
        /// <param name="CameraIndex"></param>
        public void Load(int CameraIndex)
        {
            string[] writeStrings = new string[10];
            string urlTool = Helper.CreatDirectionAutoCalib(CameraIndex);
            // 
            if (File.Exists(urlTool + "\\info.txt"))
            {
                string[] readString = File.ReadAllLines(urlTool + "\\info.txt");
                foreach (string item in readString)
                {
                    if (item.IndexOf("CalNpointOK,") >= 0) CalNpointOK = bool.Parse(item.Split(',')[1]);
                    if (item.IndexOf("CalTTMatrixOK,") >= 0) CalTTMatrixOK = bool.Parse(item.Split(',')[1]);
                }
            }
            else
            {
                CalNpointOK = false;
                CalTTMatrixOK = false;
            }
            //
            if (CalNpointOK && CalTTMatrixOK)
            {
                Matrix4x4[] tempArrMatrix = Helper.LoadAutoCalibMatrix(urlTool);
                if (tempArrMatrix != null)
                {
                    TransMatrixPBASE = tempArrMatrix[0];
                    TransformTT = tempArrMatrix[1];
                    TransMatrixPOROV = tempArrMatrix[2];
                }
            }
            //
            if (File.Exists(urlTool + "\\CalibNPointToolRBCam.vpp"))
                CalibNPointToolRBCam = (CogCalibNPointToNPoint)Cognex.VisionPro.CogSerializer.LoadObjectFromFile(urlTool + "\\CalibNPointToolRBCam.vpp", typeof(System.Runtime.Serialization.Formatters.Binary.BinaryFormatter), CogSerializationOptionsConstants.All);
            PointTransformToolFromNPointCalib = CalibNPointToolRBCam.GetComputedUncalibratedFromCalibratedTransform();
        }
    }
}
