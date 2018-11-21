namespace VisionApp
{
    public class CameraIndex : ObservableObject
    {
        private int valueIndex;

        public CameraIndex()
        {
            valueIndex = 0;
        }

        public int Value
        {
            get { return valueIndex; }
            set { valueIndex = value; OnPropertyChanged("Value"); }
        }

        public override string ToString()
        {
            return "Camera " + base.ToString();
        }
    }
}
