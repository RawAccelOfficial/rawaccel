using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using userinterface.ViewModels.Controls;

namespace userinterface.Views.Controls;

public partial class DualColumnLabelFieldView : UserControl
{
    private StackPanel? StackPanel;

    private StackPanel? StackPanelInstance
    {
        get => StackPanel;
        set
        {
            StackPanel = value;
            if (StackPanel != null && ViewModel != null)
            {
                ViewModel.SetStackPanelCommand.Execute(StackPanel);
            }
        }
    }

    public DualColumnLabelFieldViewModel? ViewModel => DataContext as DualColumnLabelFieldViewModel;

    public DualColumnLabelFieldView() : this(new DualColumnLabelFieldViewModel())
    {
    }

    public DualColumnLabelFieldView(DualColumnLabelFieldViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        InitializeStackPanel();
    }

    private void InitializeStackPanel()
    {
        StackPanelInstance = this.FindControl<StackPanel>("MainStackPanel");
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        StackPanelInstance = this.FindControl<StackPanel>("MainStackPanel");
    }
}