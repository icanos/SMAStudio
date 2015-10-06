using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SMAStudio.Util
{
    public class WorkspaceTemplateSelector : DataTemplateSelector
    {
        public DataTemplate RunbookTemplate { get; set; }
        public DataTemplate VariableTemplate { get; set; }
        public DataTemplate CredentialTemplate { get; set; }
        public DataTemplate ScheduleTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is RunbookViewModel)
                return RunbookTemplate;

            if (item is VariableViewModel)
                return VariableTemplate;

            if (item is CredentialViewModel)
                return CredentialTemplate;

            if (item is ScheduleViewModel)
                return ScheduleTemplate;

            return base.SelectTemplate(item, container);
        }
    }
}
