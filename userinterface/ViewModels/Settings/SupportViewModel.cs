using System.Windows.Input;
using userinterface.Commands;

namespace userinterface.ViewModels.Settings;

public class SupportViewModel : ViewModelBase
{
    public SupportViewModel()
    {
        BugReportCommand = new RelayCommand(() => App.OpenBugReportUrl());
    }

    public ICommand BugReportCommand { get; }
}