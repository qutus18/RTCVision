using System.Numerics;

namespace VisionApp
{
    public static class PointAddVector
    {
        /// <summary>
        /// Cộng Vector 2D với Point 2D 
        /// Trả về Point 2D
        /// </summary>
        /// <param name="point"></param>
        /// <param name="vector"></param>
        /// <returns></returns>
        public static float[] Calculate(float[] point, Vector2 vector)
        {
            return new float[] { point[0] + vector.X, point[1] + vector.Y };
        }
    }
}
