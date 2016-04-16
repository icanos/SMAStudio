using System;
using System.Windows;
using System.Windows.Controls;
using SMAStudiovNext.Modules.WindowRunbook.Editor.Completion;

namespace SMAStudiovNext.Modules.DialogStartRun
{
    public class PropertyDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var resourceName = "InputListBoxItem";

            var listBoxItem = item as ParameterCompletionData;
            if (listBoxItem != null)
            {
                if (listBoxItem.Type.Equals("bool", StringComparison.InvariantCultureIgnoreCase))
                {
                    resourceName = "CheckboxListBoxItem";
                }
            }

            var element = container as FrameworkElement;
            // ReSharper disable once PossibleNullReferenceException
            return element.FindResource(resourceName) as DataTemplate;
        }
    }
}