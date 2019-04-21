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
        /// Tính toán ma trận chuyển đổi điểm TT từ cmd
        /// </summary>
        /// <returns></returns>
        public static Matrix4x4 Calculate(PointWithTheta inputPointOCam, PointWithTheta alignPointOCam)
        {
            float tempX = inputPointOCam.X;
            float tempY = inputPointOCam.Y;
            float tempTheta = inputPointOCam.Theta;
            Matrix4x4 TransMatrix1 = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);
            tempX = alignPointOCam.X;
            tempY = alignPointOCam.Y;
            tempTheta = alignPointOCam.Theta;
            Matrix4x4 TransMatrix2 = new Matrix4x4((float)Math.Cos(tempTheta), (float)-Math.Sin(tempTheta), 0, tempX,
                                                  (float)Math.Sin(tempTheta), (float)Math.Cos(tempTheta), 0, tempY,
                                                  0, 0, 1, 0,
                                                  0, 0, 0, 1);
            /// B = M1 * O
            /// C = M2 * O
            /// B = M1 * M2 ^ -1 * C
            /// outputMatrix = M1 * M2 ^ -1
            Matrix4x4 TransMatrix2Inv;
            Matrix4x4.Invert(TransMatrix2,out TransMatrix2Inv);
            Matrix4x4 outputMatrix = Matrix4x4.Multiply(TransMatrix1, TransMatrix2Inv);
            return outputMatrix;
        }
    }
}
