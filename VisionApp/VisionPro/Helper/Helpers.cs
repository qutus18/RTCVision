using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
        public static List<PointWithTheta> CalTransRobotToOCam(List<PointWithTheta> listAutoCalibPointsRB, Matrix4x4 transformMatrixPOROVInv, Matrix4x4 transformMatrixBASE)
        {
            List<PointWithTheta> tempReturnListPoints = new List<PointWithTheta>();
            for (int i = 0; i < listAutoCalibPointsRB.Count; i++)
            {
                tempReturnListPoints.Add(TransRobotToCam(listAutoCalibPointsRB[i], transformMatrixPOROVInv, transformMatrixBASE));
            }
            return tempReturnListPoints;
        }

        /// <summary>
        /// P_VisionCal = P_GetOC*P_Base
        /// P_VisionCal = P_Get * P_OROC^-1 * P_Base
        /// </summary>
        /// <param name="pointWithTheta"></param>
        /// <param name="transformMatrixPOROVInv"></param>
        /// <param name="transformMatrixBASE"></param>
        /// <returns></returns>
        private static PointWithTheta TransRobotToCam(PointWithTheta pointWithTheta, Matrix4x4 transformMatrixPOROVInv, Matrix4x4 transformMatrixBASE)
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
            Matrix4x4 outputMatrix = Matrix4x4.Multiply(transformMatrixPOROVInv, transformMatrixBASE);
            outputMatrix = Matrix4x4.Multiply(inputMatrix, outputMatrix);
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
            float tempOffX = (float)((Math.Cos(tempTheta)) * tempX - (Math.Sin(tempTheta)) * tempY);
            float tempOffY = (float)((Math.Sin(tempTheta)) * tempX + (Math.Cos(tempTheta)) * tempY);
            Matrix4x4 inputMatrix = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);

            Matrix4x4 PBASE_INV;
            Matrix4x4.Invert(transformTT, out PBASE_INV);
            Matrix4x4 outputMatrix = Matrix4x4.Multiply(inputMatrix, transformMatrixPOROV);
            outputMatrix = Matrix4x4.Multiply(PBASE_INV, outputMatrix);
            PointWithTheta outputPoint = new PointWithTheta(outputMatrix.M14, outputMatrix.M24, (float)Math.Asin(outputMatrix.M21));
            return outputPoint;
        }

        public static PointWithTheta TransPointFromNPoint(ICogTransform2D pointTransformToolFromNPointCalib, PointWithTheta inputPoint)
        {
            double tempX, tempY;
            pointTransformToolFromNPointCalib.MapPoint(inputPoint.X, inputPoint.Y, out tempX, out tempY);
            PointWithTheta outputPoint = new PointWithTheta((float)tempX, (float)tempY, inputPoint.Theta);
            return outputPoint;
        }
    }
}
