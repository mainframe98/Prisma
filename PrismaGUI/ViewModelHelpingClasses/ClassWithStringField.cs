using System.Windows.Markup;

namespace PrismaGUI.ViewModelHelpingClasses
{
    public class ClassWithStringField : ClassWithPropertiesThatNotify
    {
        private string _value;

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

        public ClassWithStringField() : this("") {}

        public ClassWithStringField(string value)
        {
            this._value = value;
        }

        public override string ToString() => this._value;
    }
}
