using System;
using System.Numerics;

namespace VisionApp
{
    public static class Calculate2DVectorFromTCP
    {
        public static float RMS = 0;

        /// <summary>
        /// Tính toán Vector từ tâm O đến tâm TCP
        /// 1. Tính Hệ số nhân Vector OTCP/OA
        /// 2. Nhân Vector OA với hệ số => Vector V
        /// 3. Xoay Vector V ngược kim đồng hồ theo góc AOTCP => Vector OTCP
        /// </summary>
        /// <returns></returns>
        public static Vector2 Run(float[] point1, float angle1, float[] point2, float angle2, float[] point3, float angle3)
        {
            // Đổi góc sang Radian
            float angle1Rad = (float)Math.PI * (angle1 / 180);
            float angle2Rad = (float)Math.PI * (angle2 / 180);
            float angle3Rad = (float)Math.PI * (angle3 / 180);
            Vector2 returnVector2 = new Vector2();
            // Tính hệ số nhân Vector AO
            float temp1 = (float)Math.Pow(point1[0] - point2[0], 2);
            float temp2 = (float)Math.Pow(point1[1] - point2[1], 2);
            temp1 = (float)Math.Sqrt(temp1 + temp2);
            temp2 = CalculateDistanceToTCP(angle1Rad, angle2Rad, point1, point2);
            temp1 = temp2 / temp1;
            // Tính toán Vector OA * hệ số
            Vector2 tempV = new Vector2(point2[0] - point1[0], point2[1] - point1[1]);
            tempV = Vector2.Multiply(temp1, tempV);
            // Tính góc xoay
            temp2 = (180 - (angle1 - angle2)) / 2;
            // Tính ra Vector O/TCP
            tempV = Rotary2DVector.Rotate2D(tempV, temp2);
            returnVector2 = tempV;
            // Tính RMS
            temp1 = CalculateDistanceToTCP(angle1Rad, angle2Rad, point1, point2);
            temp2 = CalculateDistanceToTCP(angle3Rad, angle2Rad, point3, point2);
            RMS = temp1 - temp2;
            return returnVector2;
        }

        /// <summary>
        /// Tính toán khoảng cách A/TCP
        /// </summary>
        /// <param name="angle1"></param>
        /// <param name="angle2"></param>
        /// <param name="pointA"></param>
        /// <param name="pointO"></param>
        /// <returns></returns>
        private static float CalculateDistanceToTCP(float angle1, float angle2, float[] pointA, float[] pointO)
        {
            // Tính AO
            float temp1 = (float)Math.Pow(pointA[0] - pointO[0], 2);
            float temp2 = (float)Math.Pow(pointA[1] - pointO[1], 2);
            temp1 = (float)Math.Sqrt(temp1 + temp2);
            // Tính góc
            temp2 = (float)(Math.PI - Math.Abs(angle1 - angle2)) / 2;
            // Tính chiều cao H
            temp1 = (float)(Math.Sin(temp2) * temp1);
            // Tính khoảng cách, trả về
            return (float)(temp1/(Math.Sin(angle2)));
        }
    }
}
