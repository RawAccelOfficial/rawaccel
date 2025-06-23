using Avalonia;
using Avalonia.Controls;

namespace userinterface.Views.Controls
{
    public partial class DualColumnLabelField : UserControl
    {
        public static readonly StyledProperty<string> LabelProperty =
            AvaloniaProperty.Register<DualColumnLabelField, string>(nameof(Label), string.Empty);

        public static readonly StyledProperty<object> InputContentProperty =
            AvaloniaProperty.Register<DualColumnLabelField, object>(nameof(InputContent));

        public static readonly StyledProperty<double> LabelWidthProperty =
            AvaloniaProperty.Register<DualColumnLabelField, double>(nameof(LabelWidth), 200.0);

        public string Label
        {
            get => GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public object InputContent
        {
            get => GetValue(InputContentProperty);
            set => SetValue(InputContentProperty, value);
        }

        public double LabelWidth
        {
            get => GetValue(LabelWidthProperty);
            set => SetValue(LabelWidthProperty, value);
        }

        public DualColumnLabelField()
        {
            InitializeComponent();
        }
    }
}
