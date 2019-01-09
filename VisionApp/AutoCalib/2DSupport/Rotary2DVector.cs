using System;
using System.Numerics;

namespace VisionApp.AutoCalib
{
    public static class Rotary2DVector
    {
        /// <summary>
        /// Xoay Vector theo góc "degrees" theo chiều ngược kim đồng hồ
        /// </summary>
        /// <param name="vector"></param>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static Vector2 Rotate2D(Vector2 vector, float degrees)
        {
            float degreesInRadian = (float)Math.PI * (degrees / 180);
            float tempX = (float)(vector.X * Math.Cos(degrees) - vector.Y * Math.Sin(degrees));
            float tempY = (float)(vector.X * Math.Sin(degrees) + vector.Y * Math.Cos(degrees));
            return new Vector2(tempX, tempY);
        }
    }
}
