using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionApp.VisionPro
{
    public class PointWithTheta
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Theta { get; set; }

        public PointWithTheta()
        {
            X = 0; Y = 0; Theta = 0;
        }
        public PointWithTheta(float tX, float tY,float tTheta)
        {
            X = tX;
            Y = tY;
            Theta = tTheta;
        }
    }
}
