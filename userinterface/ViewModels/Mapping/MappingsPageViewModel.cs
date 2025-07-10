using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping
{
    public partial class MappingsPageViewModel : ViewModelBase
    {
        public MappingsPageViewModel(BE.MappingsModel mappingsBE)
        {
            MappingsBE = mappingsBE;
            MappingViews = [];
            UpdateMappingViews();
            MappingsBE.Mappings.CollectionChanged += MappingsCollectionChanged;

            AddMappingCommand = new RelayCommand(() => TryAddNewMapping());
        }

        public BE.MappingsModel MappingsBE { get; }

        public ObservableCollection<MappingViewModel> MappingViews { get; }

        public ICommand AddMappingCommand { get; }

        private void MappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
            UpdateMappingViews();

        public void UpdateMappingViews()
        {
            MappingViews.Clear();
            for (int i = 0; i < MappingsBE.Mappings.Count; i++)
            {
                var mappingBE = MappingsBE.Mappings[i];
                bool isDefault = i == 0;
                MappingViews.Add(new MappingViewModel(mappingBE, MappingsBE, isDefault));
            }
        }

        public bool TryAddNewMapping() => MappingsBE.TryAddMapping();
    }
}