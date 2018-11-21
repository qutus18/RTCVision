namespace VisionApp
{
    public class patternObject
    {
        private double x;
        private double y;
        private double angle;

        public patternObject()
        {
            x = -10000;
            y = -10000;
            angle = -10000;
        }

        public double Angle
        {
            get { return angle; }
            set { angle = value; }
        }

        public double Y
        {
            get { return y; }
            set { y = value; }
        }

        public double X
        {
            get { return x; }
            set { x = value; }
        }

    }
}
