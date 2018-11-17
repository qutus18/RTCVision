using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionApp
{
    public class DimentionObject : ObservableObject
    {
        private int _value;
        public DimentionObject()
        {
            _value = 0;
        }
        public DimentionObject(int settingValue)
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
