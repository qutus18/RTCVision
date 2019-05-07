using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Cognex.VisionPro;

namespace VisionApp.VisionPro
{
    public static class Helper
    {
        /// <summary>
        /// Kiểm tra chế độ Command nhận từ Robot
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static string GetCommandId(string cmd)
        {
            if (cmd.IndexOf("HEB,") >= 0) return "HEB";
            if (cmd.IndexOf("HE,") >= 0) return "HE";
            if (cmd.IndexOf("HEE,") >= 0) return "HEE";
            if (cmd.IndexOf("TT,") >= 0) return "TT";
            if (cmd.IndexOf("XT,") >= 0) return "XT";
            return "";
        }

        /// <summary>
        /// Lấy tọa độ Robot gửi từ cmd
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public static PointWithTheta GetRobotPointFromCmd(string cmd)
        {
            string[] tempArr = cmd.Split(',');
            float tempX, tempY, tempTheta;
            try
            {
                tempX = float.Parse(tempArr[2]);
                tempY = float.Parse(tempArr[3]);
                tempTheta = float.Parse(tempArr[4]);
            }
            catch
            {
                return null;
            }
            return new PointWithTheta(tempX, tempY, tempTheta);

        }

        /// <summary>
        /// Chuyển đổi điểm sang định dạng string trả về cho Robot
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static string CreatXTMessage(PointWithTheta point)
        {
            return $"XT,1,{point.X.ToString("###.##")},{point.Y.ToString("###.##")},{point.Theta.ToString("###.##")}";
        }

        /// <summary>
        /// Chuyển đổi list tọa độ Robot, sang list tọa độ Robot trong hệ tọa độ Camera
        /// </summary>
        /// <param name="listAutoCalibPointsRB"></param>
        /// <param name="transformMatrixPOROV"></param>
        /// <returns></returns>
        public static List<PointWithTheta> CalTransRobotToOCam(List<PointWithTheta> listAutoCalibPointsRB, Matrix4x4 transMatrixPOROV, Matrix4x4 transformMatrixBASE)
        {
            Matrix4x4 transMatrixPOROV_Inv;
            Matrix4x4.Invert(transMatrixPOROV, out transMatrixPOROV_Inv);
            List<PointWithTheta> tempReturnListPoints = new List<PointWithTheta>();
            for (int i = 0; i < listAutoCalibPointsRB.Count; i++)
            {
                tempReturnListPoints.Add(TransRobotToCam(listAutoCalibPointsRB[i], transMatrixPOROV_Inv, transformMatrixBASE));
            }
            return tempReturnListPoints;
        }

        /// <summary>
        /// P_VisionCal = POROC^-1 * PGET * PBASE
        /// </summary>
        /// <param name="pointWithTheta"></param>
        /// <param name="transMatrixPOROV_Inv"></param>
        /// <param name="transMatrixBASE"></param>
        /// <returns></returns>
        private static PointWithTheta TransRobotToCam(PointWithTheta pointWithTheta, Matrix4x4 transMatrixPOROV_Inv, Matrix4x4 transMatrixBASE)
        {
            float tempX = pointWithTheta.X;
            float tempY = pointWithTheta.Y;
            float tempTheta = pointWithTheta.Theta;
            Matrix4x4 PGET = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);
            Matrix4x4 outputMatrix = Matrix4x4.Multiply(transMatrixPOROV_Inv, PGET);
            outputMatrix = Matrix4x4.Multiply(outputMatrix, transMatrixBASE);
            PointWithTheta outputPoint = new PointWithTheta(outputMatrix.M14, outputMatrix.M24, (float)Math.Asin(outputMatrix.M21));
            return outputPoint;
        }

        /// <summary>
        /// Tính toán điểm khi qua ma trận chuyển vị
        /// </summary>
        /// <param name="pointWithTheta"></param>
        /// <param name="transformMatrixPOROV"></param>
        /// <returns></returns>
        public static PointWithTheta TransPoint(PointWithTheta pointWithTheta, Matrix4x4 transformMatrixPOROV)
        {
            float tempX = pointWithTheta.X;
            float tempY = pointWithTheta.Y;
            float tempTheta = pointWithTheta.Theta;
            float tempOffX = (float)((Math.Cos(tempTheta)) * tempX - (Math.Sin(tempTheta)) * tempY);
            float tempOffY = (float)((Math.Sin(tempTheta)) * tempX + (Math.Cos(tempTheta)) * tempY);
            Matrix4x4 inputMatrix = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);
            Matrix4x4 outputMatrix = Matrix4x4.Multiply(transformMatrixPOROV, inputMatrix);
            PointWithTheta outputPoint = new PointWithTheta(outputMatrix.M14, outputMatrix.M24, (float)Math.Asin(outputMatrix.M21));
            return outputPoint;
        }

        internal static PointWithTheta CalTransAlignToRobot(PointWithTheta pointWithTheta, Matrix4x4 transformTT, Matrix4x4 transformMatrixPOROV)
        {
            float tempX = pointWithTheta.X;
            float tempY = pointWithTheta.Y;
            float tempTheta = pointWithTheta.Theta;
            Matrix4x4 PVISION = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);

            Matrix4x4 PBASE_INV;
            Matrix4x4.Invert(transformTT, out PBASE_INV);
            Matrix4x4 outputMatrix = Matrix4x4.Multiply(PVISION, PBASE_INV);
            outputMatrix = Matrix4x4.Multiply(transformMatrixPOROV, outputMatrix);
            PointWithTheta outputPoint = new PointWithTheta(outputMatrix.M14, outputMatrix.M24, (float)Math.Asin(outputMatrix.M21));
            return outputPoint;
        }

        /// <summary>
        /// Lưu 3 ma trận tool AutoCalib ra file
        /// </summary>
        /// <param name="urlTool"></param>
        /// <param name="transMatrixPBASE"></param>
        /// <param name="transformTT"></param>
        /// <param name="transMatrixPOROV"></param>
        public static void SaveAutoCalibMatrix(string urlTool, Matrix4x4 transMatrixPBASE, Matrix4x4 transformTT, Matrix4x4 transMatrixPOROV)
        {
            string url_transMatrixPBASE = urlTool + "\\transMatrixPBASE.rtc";
            string url_transformTT = urlTool + "\\transformTT.rtc";
            string url_transMatrixPOROV = urlTool + "\\transMatrixPOROV.rtc";
            float[] arr_transMatrixPBASE = TransMatrixToArray(transMatrixPBASE);
            float[] arr_transformTT = TransMatrixToArray(transformTT);
            float[] arr_transMatrixPOROV = TransMatrixToArray(transMatrixPOROV);
            SaveObjToFile(url_transMatrixPBASE, arr_transMatrixPBASE);
            SaveObjToFile(url_transformTT, arr_transformTT);
            SaveObjToFile(url_transMatrixPOROV, arr_transMatrixPOROV);
        }

        /// <summary>
        /// Load 3 ma trận tool AutoCalib trả về 
        /// </summary>
        /// <param name="urlTool"></param>
        /// <param name="transMatrixPBASE"></param>
        /// <param name="transformTT"></param>
        /// <param name="transMatrixPOROV"></param>
        public static Matrix4x4[] LoadAutoCalibMatrix(string urlTool)
        {
            Matrix4x4 transMatrixPBASE;
            Matrix4x4 transformTT;
            Matrix4x4 transMatrixPOROV;
            try
            {
                string url_transMatrixPBASE = urlTool + "\\transMatrixPBASE.rtc";
                string url_transformTT = urlTool + "\\transformTT.rtc";
                string url_transMatrixPOROV = urlTool + "\\transMatrixPOROV.rtc";
                float[] arr_transMatrixPBASE = (float[])LoadObjFromFile(url_transMatrixPBASE);
                float[] arr_transformTT = (float[])LoadObjFromFile(url_transformTT);
                float[] arr_transMatrixPOROV = (float[])LoadObjFromFile(url_transMatrixPOROV);
                transMatrixPBASE = TransArrayToMatrix(arr_transMatrixPBASE);
                transformTT = TransArrayToMatrix(arr_transformTT);
                transMatrixPOROV = TransArrayToMatrix(arr_transMatrixPOROV);
            }
            catch { return null; }
            return new Matrix4x4[] { transMatrixPBASE, transformTT, transMatrixPOROV };
        }

        /// <summary>
        /// Load Obj từ File
        /// </summary>
        /// <param name="url_transMatrixPOROV"></param>
        /// <returns></returns>
        private static object LoadObjFromFile(string url)
        {
            object tempReturn = null;
            if (File.Exists(url))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                FileStream userFile = File.Open(url, FileMode.Open);
                tempReturn = binaryFormatter.Deserialize(userFile);
                userFile.Close();
            }
            return tempReturn;
        }

        /// <summary>
        /// Lưu Object ra file
        /// </summary>
        /// <param name="url"></param>
        /// <param name="obj"></param>
        public static void SaveObjToFile(string url, object obj)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            var userFile = File.Open(url, FileMode.Create);
            binaryFormatter.Serialize(userFile, obj);//This function serializes all our data to file
            userFile.Close();
        }

        /// <summary>
        /// Tạo đường dẫn lưu tool AutoCalib
        /// </summary>
        /// <param name="cameraIndex"></param>
        /// <returns></returns>
        public static string CreatDirectionAutoCalib(int cameraIndex)
        {
            string tempReturn = "";
            tempReturn = "D:";
            tempReturn += $"\\VProTool\\RTCCamera_0{cameraIndex}\\RTCAutoCalibTool";
            return tempReturn;
        }

        /// <summary>
        /// Tạo đường dẫn lưu tool AutoCalib
        /// </summary>
        /// <param name="cameraIndex"></param>
        /// <returns></returns>
        public static string CreatDirectionCameraVpro(int cameraIndex)
        {
            string tempReturn = "";
            tempReturn = "D:";
            tempReturn += $"\\VProTool\\RTCCamera_0{cameraIndex}";
            return tempReturn;
        }

        /// <summary>
        /// Chuyển đổi tọa độ 1 điểm qua ma trận chuyển đổi N Point
        /// </summary>
        /// <param name="pointTransformToolFromNPointCalib"></param>
        /// <param name="inputPoint"></param>
        /// <returns></returns>
        public static PointWithTheta TransPointFromNPoint(ICogTransform2D pointTransformToolFromNPointCalib, PointWithTheta inputPoint)
        {
            double tempX, tempY;
            pointTransformToolFromNPointCalib.MapPoint(inputPoint.X, inputPoint.Y, out tempX, out tempY);
            PointWithTheta outputPoint = new PointWithTheta((float)tempX, (float)tempY, inputPoint.Theta);
            return outputPoint;
        }

        /// <summary>
        /// Chuyển đổi từ ma trận 4x4 thành mảng 16 phần tử
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static float[] TransMatrixToArray(Matrix4x4 a)
        {
            return new float[16] { a.M11, a.M12, a.M13, a.M14, a.M21, a.M22, a.M23, a.M24, a.M31, a.M32, a.M33, a.M34, a.M41, a.M42, a.M43, a.M44 };
        }

        /// <summary>
        /// Chuyển đổi từ mảng 16 phần tử thành ma trận 4x4
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Matrix4x4 TransArrayToMatrix(float[] a)
        {
            return new Matrix4x4(a[0], a[1], a[2], a[3], a[4], a[5], a[6], a[7], a[8], a[9], a[10], a[11], a[12], a[13], a[14], a[15]);
        }

        /// <summary>
        /// Wait for an Event to trigger or for a timeout
        /// </summary>
        /// <param name="eventHandle">The event to wait for</param>
        /// <param name="timeout">Maximum time to wait</param>
        /// <returns>true if the event triggered, false on timeout</returns>
        public static bool WaitForEvent(EventWaitHandle eventHandle, TimeSpan timeout)
        {
            bool didWait = false;
            var frame = new DispatcherFrame();
            new Thread(() =>
            {
            // asynchronously wait for the event/timeout
            didWait = eventHandle.WaitOne(timeout);
            // signal the secondary dispatcher to stop
            frame.Continue = false;
            }).Start();
            Dispatcher.PushFrame(frame); // start the secondary dispatcher, pausing this code
            return didWait;
        }

        public static void WriteLogString(string inputString)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(delegate
            {
                MainWindow.logString.Value += inputString + "\r\n";
            }));
        }
    }
}
