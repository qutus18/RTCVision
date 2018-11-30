namespace VisionApp
{
    class tabMainObject : ObservableObject
    {
        public IntValueObject valueTab1, valueTab2, valueTab3;

        public tabMainObject()
        {
            valueTab1 = new IntValueObject();
            valueTab2 = new IntValueObject();
            valueTab2 = new IntValueObject();
        }
    }
}
