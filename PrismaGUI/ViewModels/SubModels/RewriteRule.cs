using System.Text.RegularExpressions;
using System.Windows.Markup;
using PrismaGUI.ViewModelHelpingClasses;

namespace PrismaGUI.ViewModels.SubModels
{
    public class RewriteRule : ClassWithPropertiesThatNotify
    {
        private Regex _rule;
        private string _rewriteTo;

        [ConstructorArgument("rule")]
        public Regex Rule
        {
            get => this._rule;
            set
            {
                this._rule = value;
                this.NotifyPropertyChanged();
            }
        }

        [ConstructorArgument("rewriteTo")]
        public string RewriteTo
        {
            get => this._rewriteTo;
            set
            {
                this._rewriteTo = value;
                this.NotifyPropertyChanged();
            }
        }

        public RewriteRule() : this(new(""), "") {}

        public RewriteRule(Regex rule, string rewriteTo)
        {
            this._rule = rule;
            this._rewriteTo = rewriteTo;
        }
    }
}
