using System.Windows.Media.Media3D;
using System.Numerics;
using System;

namespace VisionApp.AutoCalib
{
    public class CalculateTranform3Dto2DMatrix
    {
        private Point3D pointA, pointB, pointC, pointuN, pointuAB, pointuV;
        private Matrix4x4 transformMatrix, matrixS, matrixSinv, matrixD;
        private Vector3D AB, AC, N, uN, uAB, uV;

        public CalculateTranform3Dto2DMatrix(Point3D point1, Point3D point2, Point3D point3)
        {
            transformMatrix = new Matrix4x4();
            pointA = point1;
            pointB = point2;
            pointC = point3;
        }

        /// <summary>
        /// Tính toán ma trận chuyển đổi 3D thành 2D
        /// </summary>
        /// <returns></returns>
        public Matrix4x4 CalculateTransformMatrix()
        {
            // Tính 2 Vector 3D
            CalculateVector3D();
            // Nhân 2 Vector ra vertor N
            N = Vector3D.CrossProduct(AB, AC);
            // Tính Vecter gốc uAB, uN
            N.Normalize();
            uN = N;
            AB.Normalize();
            uAB = AB;
            // Nhân ra Vector uV
            uV = Vector3D.CrossProduct(uN, uAB);
            // Tính ra 4 điểm Base
            pointuN = pointA + uN;
            pointuAB = pointA + uAB;
            pointuV = pointA + uV;
            // Tính ra ma trận 
            matrixS = new Matrix4x4((float)pointA.X, (float)pointuAB.X, (float)pointuV.X, (float)pointuN.X,
                                    (float)pointA.X, (float)pointuAB.X, (float)pointuV.X, (float)pointuN.X,
                                    (float)pointA.X, (float)pointuAB.X, (float)pointuV.X, (float)pointuN.X,
                                    1, 1, 1, 1);
            matrixSinv = new Matrix4x4();
            Matrix4x4.Invert(matrixS, out matrixSinv);
            matrixD = new Matrix4x4(0, 1, 0, 0,
                                    0, 0, 1, 0,
                                    0, 0, 0, 1,
                                    1, 1, 1, 1);
            // D M * S = D
            // M* S *Sinv = D * Sinv
            // M = D * Sinv
            transformMatrix = Matrix4x4.Multiply(matrixD, matrixSinv);
            return transformMatrix;
        }

        private void CalculateVector3D()
        {
            var tempX = pointB.X - pointA.X;
            var tempY = pointB.Y - pointA.Y;
            var tempZ = pointB.Z - pointA.Z;
            AB = new Vector3D(tempX, tempY, tempZ);
            tempX = pointC.X - pointA.X;
            tempY = pointC.Y - pointA.Y;
            tempZ = pointC.Z - pointA.Z;
            AC = new Vector3D(tempX, tempY, tempZ);
        }
    }
}
