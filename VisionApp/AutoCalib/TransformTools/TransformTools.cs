using System.Windows.Media.Media3D;
using System.Numerics;

namespace VisionApp.AutoCalib
{
    public static class TransformTools
    {
        /// <summary>
        /// Chuyển điểm 3D thành điểm 2D dưới dạng float[2]
        /// </summary>
        /// <param name="p3D"></param>
        /// <param name="transM"></param>
        /// <returns></returns>
        public static float[] Trans3DPointTo2D(Point3D p3D, Matrix4x4 transM)
        {
            float[] returnPoint = new float[2];
            Vector3 tempV3 = new Vector3((float)p3D.X, (float)p3D.Y, (float)p3D.Z);
            Vector3.Transform(tempV3, transM);
            returnPoint[0] = tempV3.X;
            returnPoint[1] = tempV3.Y;
            return returnPoint;
        }

        /// <summary>
        /// Chuyển điểm 2D thành điểm 3D dưới dạng Point3D
        /// </summary>
        /// <param name="inputPoint"></param>
        /// <param name="transM"></param>
        /// <returns></returns>
        public static Point3D Trans2DPointTo3D(float[] inputPoint, Matrix4x4 transM)
        {
            Matrix4x4 tranMinv = new Matrix4x4();
            Point3D returnPoint = new Point3D(inputPoint[0], inputPoint[1], 0);
            Matrix4x4.Invert(transM, out tranMinv);
            Vector3 tempV3 = new Vector3((float)returnPoint.X, (float)returnPoint.Y, (float)returnPoint.Z);
            Vector3.Transform(tempV3, tranMinv);
            returnPoint.X = tempV3.X;
            returnPoint.Y = tempV3.Y;
            returnPoint.Z = tempV3.Z;
            return returnPoint;
        }
    }
}
