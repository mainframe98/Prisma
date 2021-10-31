using System.Collections.Generic;
using System.Windows.Markup;
using PrismaGUI.ViewModelHelpingClasses;

namespace PrismaGUI.ViewModels.SubModels
{
    public class EnvironmentVariable : ClassWithPropertiesThatNotify
    {
        private string _variable;
        private string _value;

        [ConstructorArgument("variable")]
        public string Variable
        {
            get => this._variable;
            set
            {
                this._variable = value;
                this.NotifyPropertyChanged();
            }
        }

        [ConstructorArgument("value")]
        public string Value
        {
            get => this._value;
            set
            {
                this._value = value;
                this.NotifyPropertyChanged();
            }
        }

        public EnvironmentVariable() : this("", "") {}

        public EnvironmentVariable(string variable, string value)
        {
            this._variable = variable;
            this._value = value;
        }

        public EnvironmentVariable(KeyValuePair<string, string> pair) : this(pair.Key, pair.Value) {}
    }
}
