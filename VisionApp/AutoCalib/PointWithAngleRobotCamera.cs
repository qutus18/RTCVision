namespace VisionApp
{
    /// <summary>
    /// Đối tượng lưu cặp điểm Robot - Camera và góc
    /// </summary>
    public class PointWithAngleRobotCamera
    {
        private float robotX;

        public float RobotX
        {
            get { return robotX; }
            set { robotX = value; }
        }

        private float robotY;

        public float RobotY
        {
            get { return robotY; }
            set { robotY = value; }
        }

        private float camX;

        public float CamX
        {
            get { return camX; }
            set { camX = value; }
        }

        private float camY;

        public float CamY
        {
            get { return camY; }
            set { camY = value; }
        }

        private float robotAngle;

        public float RobotAngle
        {
            get { return robotAngle; }
            set { robotAngle = value; }
        }

        private float camAngle;

        public float CamAngle
        {
            get { return camAngle; }
            set { camAngle = value; }
        }

        public PointWithAngleRobotCamera(float RX, float RY, float CX, float CY, float RA, float CA)
        {
            RobotX = RX;
            RobotY = RY;
            RobotAngle = RA;
            CamX = CX;
            CamY = CY;
            CamAngle = CA;
        }
       
    }
}
