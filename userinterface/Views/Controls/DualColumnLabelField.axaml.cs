using Avalonia;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;

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

        public static readonly StyledProperty<ObservableCollection<FieldItem>> AdditionalFieldsProperty =
            AvaloniaProperty.Register<DualColumnLabelField, ObservableCollection<FieldItem>>(
                nameof(AdditionalFields));

        public static readonly StyledProperty<bool> HasAdditionalFieldsProperty =
            AvaloniaProperty.Register<DualColumnLabelField, bool>(nameof(HasAdditionalFields), false);

        public static readonly StyledProperty<object> AlternativeViewContentProperty =
            AvaloniaProperty.Register<DualColumnLabelField, object>(nameof(AlternativeViewContent));

        public static readonly StyledProperty<bool> ShowAlternativeViewProperty =
            AvaloniaProperty.Register<DualColumnLabelField, bool>(nameof(ShowAlternativeView), false);

        public static readonly StyledProperty<bool> ShowAdditionalFieldsProperty =
            AvaloniaProperty.Register<DualColumnLabelField, bool>(nameof(ShowAdditionalFields), false);

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

        public ObservableCollection<FieldItem> AdditionalFields
        {
            get => GetValue(AdditionalFieldsProperty) ?? new ObservableCollection<FieldItem>();
            set => SetValue(AdditionalFieldsProperty, value);
        }

        public bool HasAdditionalFields
        {
            get => GetValue(HasAdditionalFieldsProperty);
            private set => SetValue(HasAdditionalFieldsProperty, value);
        }

        public object AlternativeViewContent
        {
            get => GetValue(AlternativeViewContentProperty);
            set => SetValue(AlternativeViewContentProperty, value);
        }

        public bool ShowAlternativeView
        {
            get => GetValue(ShowAlternativeViewProperty);
            set => SetValue(ShowAlternativeViewProperty, value);
        }

        public bool ShowAdditionalFields
        {
            get => GetValue(ShowAdditionalFieldsProperty);
            private set => SetValue(ShowAdditionalFieldsProperty, value);
        }

        public DualColumnLabelField()
        {
            // Initialize the collection before calling InitializeComponent
            SetValue(AdditionalFieldsProperty, new ObservableCollection<FieldItem>());
            InitializeComponent();
            
            // Subscribe to collection changes to update properties
            AdditionalFields.CollectionChanged += OnAdditionalFieldsChanged;
            
            // Subscribe to ShowAlternativeView changes to update ShowAdditionalFields
            this.PropertyChanged += OnPropertyChanged;
            
            // Set initial values
            UpdateHasAdditionalFields();
            UpdateShowAdditionalFields();
        }

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ShowAlternativeViewProperty)
            {
                UpdateShowAdditionalFields();
            }
        }

        private void OnAdditionalFieldsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateHasAdditionalFields();
            UpdateShowAdditionalFields();
        }

        private void UpdateHasAdditionalFields()
        {
            HasAdditionalFields = AdditionalFields?.Count > 0;
        }

        private void UpdateShowAdditionalFields()
        {
            // Show additional fields only if we have them and we're not showing alternative view
            ShowAdditionalFields = HasAdditionalFields && !ShowAlternativeView;
        }

        // Helper method to add new fields dynamically
        public void AddField(string label, object inputContent)
        {
            AdditionalFields.Add(new FieldItem { Label = label, InputContent = inputContent });
        }

        // Helper method to clear additional fields
        public void ClearAdditionalFields()
        {
            AdditionalFields.Clear();
        }

        // Helper method to remove a specific field
        public void RemoveField(int index)
        {
            if (index >= 0 && index < AdditionalFields.Count)
            {
                AdditionalFields.RemoveAt(index);
            }
        }

        // Helper method to switch to alternative view
        public void ShowAlternativeViewContent(object viewContent)
        {
            AlternativeViewContent = viewContent;
            ShowAlternativeView = true;
        }

        // Helper method to switch back to additional fields
        public void ShowAdditionalFieldsContent()
        {
            ShowAlternativeView = false;
        }

        // Helper method to toggle between view modes
        public void ToggleViewMode()
        {
            ShowAlternativeView = !ShowAlternativeView;
        }
    }

    // Data model for additional fields
    public class FieldItem
    {
        public string Label { get; set; } = string.Empty;
        public object? InputContent { get; set; }
    }
}
