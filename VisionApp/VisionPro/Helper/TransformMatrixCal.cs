using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace VisionApp.VisionPro
{
	public static class TransformMatrixCal
	{
		/// <summary>
		/// double - Tính toán tọa độ Pget_OV - Tọa độ của Robot trong hệ tọa độ Camera
        /// Cách tính: Tính PBase theo 2 công thức, giải phương trình + đồng nhất hệ số
		/// </summary>
		/// <param name="xPV1">Tọa độ Camera X _ 1</param>
		/// <param name="yPV1">Tọa độ Camera Y _ 1</param>
		/// <param name="alpha_PV1">Góc Camera _ 1</param>
		/// <param name="xPV2">Tọa độ Camera X _ 2</param>
		/// <param name="yPV2">Tọa độ Camera Y _ 2</param>
		/// <param name="alpha_PV2">Góc Camera _ 2</param>
		/// <returns></returns>
		public static double[] CalPxPy(double xPV1, double yPV1, double alpha_PV1, double xPV2, double yPV2, double alpha_PV2)
		{
			double temp = (-Math.Sin((System.Double)(2 * alpha_PV2)) * yPV1 + Math.Sin((System.Double)(2 * alpha_PV2)) * yPV2 + xPV1 * Math.Cos((System.Double)(2 * alpha_PV2)) + Math.Cos((System.Double)(2 * alpha_PV2)) * xPV2 + yPV1 * Math.Sin((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) - yPV2 * Math.Sin((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) + Math.Cos((System.Double)(2 * alpha_PV1)) * xPV1 + xPV2 * Math.Cos((System.Double)(2 * alpha_PV1)) + xPV1 * Math.Cos((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) + xPV2 * Math.Cos((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) - 0.2e1 * xPV1 * Math.Cos((System.Double)(-alpha_PV2 + alpha_PV1)) - 0.2e1 * xPV2 * Math.Cos((System.Double)(-alpha_PV2 + alpha_PV1)) - 0.2e1 * xPV1 * Math.Cos((System.Double)(alpha_PV2 + alpha_PV1)) - 0.2e1 * xPV2 * Math.Cos((System.Double)(alpha_PV2 + alpha_PV1)) + Math.Sin((System.Double)(2 * alpha_PV1)) * yPV1 - Math.Sin((System.Double)(2 * alpha_PV1)) * yPV2 + xPV1 + xPV2) / (0.2e1 + 0.2e1 * Math.Cos((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) - 0.4e1 * Math.Cos((System.Double)(alpha_PV2 + alpha_PV1)) - 0.4e1 * Math.Cos((System.Double)(-alpha_PV2 + alpha_PV1)) + 0.2e1 * Math.Cos((System.Double)(2 * alpha_PV2)) + 0.2e1 * Math.Cos((System.Double)(2 * alpha_PV1)));
			double temp2 = (xPV1 * Math.Sin((System.Double)(2 * alpha_PV2)) - xPV2 * Math.Sin((System.Double)(2 * alpha_PV2)) + Math.Cos((System.Double)(2 * alpha_PV2)) * yPV1 + yPV2 * Math.Cos((System.Double)(2 * alpha_PV2)) - xPV1 * Math.Sin((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) + xPV2 * Math.Sin((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) + yPV1 * Math.Cos((System.Double)(2 * alpha_PV1)) + Math.Cos((System.Double)(2 * alpha_PV1)) * yPV2 + yPV1 * Math.Cos((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) + yPV2 * Math.Cos((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) - 0.2e1 * yPV1 * Math.Cos((System.Double)(-alpha_PV2 + alpha_PV1)) - 0.2e1 * yPV2 * Math.Cos((System.Double)(-alpha_PV2 + alpha_PV1)) - 0.2e1 * yPV1 * Math.Cos((System.Double)(alpha_PV2 + alpha_PV1)) - 0.2e1 * yPV2 * Math.Cos((System.Double)(alpha_PV2 + alpha_PV1)) - xPV1 * Math.Sin((System.Double)(2 * alpha_PV1)) + xPV2 * Math.Sin((System.Double)(2 * alpha_PV1)) + yPV1 + yPV2) / (0.2e1 + 0.2e1 * Math.Cos((System.Double)(2 * alpha_PV1 - 2 * alpha_PV2)) - 0.4e1 * Math.Cos((System.Double)(alpha_PV2 + alpha_PV1)) - 0.4e1 * Math.Cos((System.Double)(-alpha_PV2 + alpha_PV1)) + 0.2e1 * Math.Cos((System.Double)(2 * alpha_PV2)) + 0.2e1 * Math.Cos((System.Double)(2 * alpha_PV1)));
			double[] final = new double[2] { temp, temp2 };
			return final;
		}

		/// <summary>
		/// float - Tính toán ma trận chuyển đổi tọa độ Robot sang tọa độ Camera
        /// Từ tọa độ trong hệ Camera + Tọa độ trong hệ Robot => Vector chuyển hệ tọa độ Robot Camera
		/// </summary>
		/// <param name="xPG1"></param>
		/// <param name="yPG1"></param>
		/// <param name="alpha_PG1"></param>
		/// <param name="xPG1_OV"></param>
		/// <param name="yPG1_OV"></param>
		/// <returns></returns>
		public static Matrix4x4 CalMatrix(double xPG1, double yPG1, double alpha_PG1, double xPG1_OV, double yPG1_OV)
		{
			// Ma trận PGet1
			Matrix4x4 mPget = new Matrix4x4((float)Math.Cos(alpha_PG1), (float)-Math.Sin(alpha_PG1), 0, (float)xPG1,
											(float)Math.Sin(alpha_PG1), (float)Math.Cos(alpha_PG1), 0, (float)yPG1,
											0, 0, 1, 0,
											0, 0, 0, 1);
			// Ma trận PGet1_OV
			Matrix4x4 mPget_OV = new Matrix4x4((float)Math.Cos(alpha_PG1), (float)-Math.Sin(alpha_PG1), 0, (float)xPG1_OV,
											(float)Math.Sin(alpha_PG1), (float)Math.Cos(alpha_PG1), 0, (float)yPG1_OV,
											0, 0, 1, 0,
											0, 0, 0, 1);
			// Ma trận Pget1_OV_inv
			Matrix4x4 mPget_OV_inv = new Matrix4x4();
			Matrix4x4.Invert(mPget_OV, out mPget_OV_inv);
			// Ma trận OROV
			Matrix4x4 mPOROV = mPget * mPget_OV_inv; 
			return mPOROV;
		}

        /// <summary>
        /// Tính toán ma trận chuyển đổi hệ Robot sang hệ Camera
        /// Đầu vào 2 điểm góc xoay hệ tọa độ Camera + điểm tọa độ Robot
        /// </summary>
        /// <param name="pointCam1"></param>
        /// <param name="pointCam2"></param>
        /// <param name="pointRB"></param>
        /// <returns></returns>
        public static Matrix4x4 Calculate(PointWithTheta pointCam1, PointWithTheta pointCam2, PointWithTheta pointRB)
        {
            Matrix4x4 outputMatrixInv;
            double[] PxyGetOV = CalPxPy(pointCam1.X, pointCam1.Y, pointCam1.Theta, pointCam2.X, pointCam2.Y, pointCam2.Theta);
            Matrix4x4 outputMatrix = CalMatrix(pointRB.X, pointRB.Y, pointRB.Theta, PxyGetOV[0], PxyGetOV[1]);
            Matrix4x4.Invert(outputMatrix, out outputMatrixInv);
            return outputMatrixInv;
        }
	}
}
