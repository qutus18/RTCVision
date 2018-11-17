using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisionApp
{
    public class StringValueObject:ObservableObject
    {
        private string _value;

        public StringValueObject(string optionString)
        {
            _value = optionString;
        }

        public string Value
        {
            get { return _value; }
            set
            {
                if (value.Length < 10000) _value = value;
                else _value = value.Substring(value.Length - 9999);
                OnPropertyChanged("Value");
            }
        }

    }
}
