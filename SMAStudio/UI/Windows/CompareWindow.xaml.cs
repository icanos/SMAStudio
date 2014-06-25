using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using SMAStudio.Commands;
using SMAStudio.SMAWebService;
using SMAStudio.UI.Colorizing;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SMAStudio
{
    /// <summary>
    /// Interaction logic for CompareWindow.xaml
    /// </summary>
    public partial class CompareWindow : Window
    {
        private RunbookViewModel _runbookViewModel;
        private RunbookVersionViewModel _runbookVersionViewModel;

        public CompareWindow(RunbookViewModel runbookViewModel, RunbookVersionViewModel runbookVersionViewModel)
        {
            DataContext = new CompareViewModel(this);

            InitializeComponent();

            _runbookViewModel = runbookViewModel;
            _runbookVersionViewModel = runbookVersionViewModel;

            // Create a new revert command
            ((CompareViewModel)DataContext).RevertCommand =
                new RevertSpecificCommand(this, _runbookViewModel, _runbookVersionViewModel);

            Loaded += WindowLoaded;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            var scrollView = ControlHelper.FindDescendant<ScrollViewer>(txtDiffLeft);
            scrollView.ScrollChanged += ScrollChanged;

            scrollView = ControlHelper.FindDescendant<ScrollViewer>(txtDiffRight);
            scrollView.ScrollChanged += ScrollChanged;

            BuildDiffView();
        }

        private void ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scrollViewer = ((ScrollViewer)sender);

            if (scrollViewer.Content == txtDiffLeft.TextArea)
            {
                txtDiffRight.ScrollToVerticalOffset(e.VerticalOffset);
                txtDiffRight.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
            else if (scrollViewer.Content == txtDiffRight.TextArea)
            {
                txtDiffLeft.ScrollToVerticalOffset(e.VerticalOffset);
                txtDiffLeft.ScrollToHorizontalOffset(e.HorizontalOffset);
            }
        }

        private void BuildDiffView()
        {
            txtDiffLeft.TextArea.TextView.LineTransformers.Clear();
            txtDiffRight.TextArea.TextView.LineTransformers.Clear();

            txtDiffLeft.Text = _runbookVersionViewModel.GetContent();
            txtDiffRight.Text = _runbookViewModel.Content;

            txtDiffLeftTitle.Text = "revision " + _runbookVersionViewModel.RunbookVersion.VersionNumber + " - (system)";
            txtDiffRightTitle.Text = "Current version of " + _runbookViewModel.RunbookName;

            var differ = new Differ();
            var diffBuilder = new SideBySideDiffBuilder(differ);

            var model = diffBuilder.BuildDiffModel(txtDiffLeft.Text, txtDiffRight.Text);

            // Replace the content in each textbox with the text retrieved from the diff builder
            txtDiffLeft.Text = "";
            foreach (var line in model.OldText.Lines)
            {
                txtDiffLeft.Text += line.Text + "\r\n";
            }

            txtDiffRight.Text = "";
            foreach (var line in model.NewText.Lines)
            {
                txtDiffRight.Text += line.Text + "\r\n";
            }

            Edits = model;

            var diffColorizer = new DiffColorizing(Edits, true);
            txtDiffLeft.TextArea.TextView.LineTransformers.Add(diffColorizer);

            var diffColorizerRight = new DiffColorizing(Edits, false);
            txtDiffRight.TextArea.TextView.LineTransformers.Add(diffColorizerRight);
        }

        private SideBySideDiffModel Edits
        {
            get;
            set;
        }

        public RunbookViewModel Runbook
        {
            get { return _runbookViewModel; }
        }

        public RunbookVersionViewModel Version
        {
            get { return _runbookVersionViewModel; }
            set
            {
                _runbookVersionViewModel = value;
                
                // Rebuild the differencing view
                BuildDiffView();
            }
        }
    }
}
