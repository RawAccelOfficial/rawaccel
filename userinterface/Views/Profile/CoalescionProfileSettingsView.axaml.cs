using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Profile;
using userinterface.Views.Controls;

namespace userinterface.Views.Profile;

public partial class CoalescionProfileSettingsView : UserControl
{
    private DualColumnLabelFieldView? CoalescionField;
    private DualColumnLabelFieldViewModel? CoalescionFieldViewModel;

    public CoalescionProfileSettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (CoalescionField == null)
        {
            SetupControls();
        }
    }

    private void SetupControls()
    {
        if (DataContext is not CoalescionProfileSettingsViewModel viewModel)
        {
            return;
        }

        CreateCoalescionFieldViewModel();
        AddCoalescionFields(viewModel);
        AddControlToMainPanel();
    }

    private void CreateCoalescionFieldViewModel()
    {
        CoalescionFieldViewModel = new DualColumnLabelFieldViewModel();
        CoalescionField = new DualColumnLabelFieldView(CoalescionFieldViewModel);
    }

    private void AddCoalescionFields(CoalescionProfileSettingsViewModel viewModel)
    {
        if (CoalescionFieldViewModel == null)
            return;

        CoalescionFieldViewModel.AddField("Input Smoothing Half Life", CreateInputControl(viewModel.InputSmoothingHalfLife));
        CoalescionFieldViewModel.AddField("Scale Smoothing Half Life", CreateInputControl(viewModel.ScaleSmoothingHalfLife));
    }

    private void AddControlToMainPanel()
    {
        if (CoalescionField == null)
        {
            return;
        }

        var mainStackPanel = this.FindControl<StackPanel>("MainStackPanel");
        mainStackPanel?.Children.Add(CoalescionField);
    }

    private static Control CreateInputControl(object bindingSource)
    {
        if (bindingSource is not EditableFieldViewModel editableField)
            return new TextBox();

        editableField.UpdateMode = UpdateMode.OnChange;

        var editableFieldView = new EditableFieldView
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = editableField
        };

        return editableFieldView;
    }
}