using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionApp
{
    public class IntValueObject : ObservableObject
    {
        private int _value;
        public IntValueObject()
        {
            _value = 0;
        }
        public IntValueObject(int settingValue)
        {
            _value = settingValue;
        }
        public int Value
        {
            get { return _value; }
            set
            {
                _value = value;
                OnPropertyChanged("Value");
            }
        }

    }
}
