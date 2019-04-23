using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace VisionApp.VisionPro
{
    public static class TransformTTCal
    {
        /// <summary>
        /// Tính toán ma trận chuyển đổi điểm TT(PBASE) từ cmd
        /// </summary>
        /// <returns></returns>
        public static Matrix4x4 Calculate(PointWithTheta inputPoint, PointWithTheta alignPointOCam, Matrix4x4 transMatrixPOROC)
        {
            float tempX = inputPoint.X;
            float tempY = inputPoint.Y;
            float tempTheta = inputPoint.Theta;
            Matrix4x4 PGET = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);
            tempX = alignPointOCam.X;
            tempY = alignPointOCam.Y;
            tempTheta = alignPointOCam.Theta;
            Matrix4x4 PVISION = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);
            Matrix4x4 PGET_OCINV;
            Matrix4x4.Invert(PGET,out PGET_OCINV);
            Matrix4x4 outputMatrix = Matrix4x4.Multiply(PGET_OCINV, transMatrixPOROC);
            outputMatrix = Matrix4x4.Multiply(outputMatrix, PVISION);
            return outputMatrix;
        }
    }
}
