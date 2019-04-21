using System.Numerics;
using Cognex.VisionPro.CalibFix;
namespace VisionApp
{
    class AutoCalibObject
    {
        // Khai báo Tool calib Npoint
        private CogCalibNPointToNPoint calibNPoint = null;
        public CogCalibNPointToNPoint CalibNPoint
        {
            get { return calibNPoint; }
            private set { calibNPoint = value; }
        }
        // Đếm số điểm đã Add vào Tool
        private int countPointCalib = 0;
        private PointWithAngleRobotCamera[] pointWNoAngleCollection = null;
        // Đếm số điểm có góc
        private int countPointAngle = 0;
        private PointWithAngleRobotCamera[] pointWAngleCollection = null;
        // Vector O=>TCP
        private Vector2 vector2OtoTCP;
        public Vector2 Vector2OtoTCP
        {
            get { return vector2OtoTCP; }
            private set { vector2OtoTCP = value; }
        }

        public bool CalibrationStatus { get; private set; }

        public AutoCalibObject()
        {
            calibNPoint = new CogCalibNPointToNPoint();
            pointWAngleCollection = new PointWithAngleRobotCamera[3];
            pointWNoAngleCollection = new PointWithAngleRobotCamera[9];
        }

        // Thêm điểm không có góc vào Tool NPoint
        public void AddPoint(float RobotX, float RobotY, float CamX, float CamY)
        {
            //calibNPoint.AddPointPair(CamX, CamY, RobotX, RobotY);
            if (countPointCalib < 9)
            {
                pointWNoAngleCollection[countPointCalib] = new PointWithAngleRobotCamera(RobotX, RobotY, CamX, CamY, 0, 0);
            }
            countPointCalib += 1;
        }

        // Thêm điểm có góc vào tập hợp điểm
        public void AddPointWAngle(float RobotX, float RobotY, float CamX, float CamY, float RobotAngle, float CamAngle)
        {
            if (countPointAngle < 3)
            {
                pointWAngleCollection[countPointAngle] = new PointWithAngleRobotCamera(RobotX, RobotY, CamX, CamY, RobotAngle, CamAngle);
                countPointAngle += 1;
            }
        }

        // 
        public void Calculate()
        {
            // Check Condition
            if ((countPointCalib < 9) || (countPointAngle < 3)) return;

            // Tính toán Vector
            float[] tempP1 = { pointWAngleCollection[0].CamX, pointWAngleCollection[0].CamY };
            float tempAngle1 = pointWAngleCollection[0].CamAngle;
            float[] tempP2 = { pointWAngleCollection[1].CamX, pointWAngleCollection[1].CamY };
            float tempAngle2 = pointWAngleCollection[1].CamAngle;
            float[] tempP3 = { pointWAngleCollection[2].CamX, pointWAngleCollection[2].CamY };
            float tempAngle3 = pointWAngleCollection[2].CamAngle;
            Vector2OtoTCP = Calculate2DVectorFromTCP.Run(tempP1, tempAngle1, tempP2, tempAngle2, tempP3, tempAngle3);

            // Chạy Tool Calib - Trả về?
            float[] tempFA = null;
            for (int i = 0; i < 9; i++)
            {
                tempFA = PointAddVector.Calculate(new float[] { pointWNoAngleCollection[i].CamX, pointWNoAngleCollection[i].CamY }, vector2OtoTCP);
                calibNPoint.AddPointPair(tempFA[0], tempFA[1], pointWNoAngleCollection[i].RobotX, pointWNoAngleCollection[i].RobotY);
            }
            calibNPoint.Calibrate();

            // Set Done
            CalibrationStatus = true;
        }
    }
}
