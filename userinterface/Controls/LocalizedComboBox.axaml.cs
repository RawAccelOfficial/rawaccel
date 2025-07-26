using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using userinterface.Services;

namespace userinterface.Controls
{
    public partial class LocalizedComboBox : UserControl
    {
        public static readonly StyledProperty<IEnumerable<string>> LocalizationKeysProperty =
            AvaloniaProperty.Register<LocalizedComboBox, IEnumerable<string>>(nameof(LocalizationKeys));

        public static readonly StyledProperty<IEnumerable<string>> EnumValuesProperty =
            AvaloniaProperty.Register<LocalizedComboBox, IEnumerable<string>>(nameof(EnumValues));

        private readonly LocalizationService localizationService;
        public readonly ObservableCollection<LocalizedComboItem> localizedItems;

        public LocalizedComboBox()
        {
            InitializeComponent();
            localizationService = App.Services.GetRequiredService<LocalizationService>();
            localizationService.PropertyChanged += OnLocalizationChanged;
            localizedItems = new ObservableCollection<LocalizedComboItem>();

            // Set up the internal ComboBox
            InternalComboBox.ItemsSource = localizedItems;
        }

        public IEnumerable<string> LocalizationKeys
        {
            get => GetValue(LocalizationKeysProperty);
            set => SetValue(LocalizationKeysProperty, value);
        }

        public IEnumerable<string> EnumValues
        {
            get => GetValue(EnumValuesProperty);
            set => SetValue(EnumValuesProperty, value);
        }

        // Expose the internal ComboBox properties directly
        public object SelectedItem
        {
            get => InternalComboBox.SelectedItem;
            set => InternalComboBox.SelectedItem = value;
        }

        public int SelectedIndex
        {
            get => InternalComboBox.SelectedIndex;
            set => InternalComboBox.SelectedIndex = value;
        }

        // Expose SelectionChanged event
        public event System.EventHandler<SelectionChangedEventArgs> SelectionChanged
        {
            add => InternalComboBox.SelectionChanged += value;
            remove => InternalComboBox.SelectionChanged -= value;
        }

        // Helper property to get the selected enum value
        public string SelectedEnumValue => (SelectedItem as LocalizedComboItem)?.EnumValue;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == LocalizationKeysProperty || change.Property == EnumValuesProperty)
            {
                UpdateLocalizedItems();
            }
        }

        public void RefreshItems()
        {
            UpdateLocalizedItems();
        }

        private void UpdateLocalizedItems()
        {
            var keys = LocalizationKeys?.ToList() ?? new List<string>();
            var values = EnumValues?.ToList() ?? new List<string>();

            localizedItems.Clear();

            if (keys.Count == 0 || values.Count == 0 || keys.Count != values.Count)
                return;

            for (int i = 0; i < keys.Count; i++)
            {
                localizedItems.Add(new LocalizedComboItem
                {
                    LocalizationKey = keys[i],
                    EnumValue = values[i],
                    LocalizedText = localizationService.GetText(keys[i])
                });
            }

            // Auto-select first item if nothing is selected
            if (localizedItems.Count > 0 && SelectedItem == null)
            {
                SelectedItem = localizedItems[0];
            }
        }

        private void OnLocalizationChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update localized text for all items when language changes
            foreach (var item in localizedItems)
            {
                item.LocalizedText = localizationService.GetText(item.LocalizationKey);
            }
        }

    }

    public class LocalizedComboItem : INotifyPropertyChanged
    {
        private string localizedText;

        public string LocalizationKey { get; set; }
        public string EnumValue { get; set; }

        public string LocalizedText
        {
            get => localizedText;
            set
            {
                if (localizedText != value)
                {
                    localizedText = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}