﻿using System.Collections.ObjectModel;
using System.Collections.Specialized;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class MappingsPageViewModel : ViewModelBase
    {
        public MappingsPageViewModel(BE.MappingsModel mappingsBE)
        {
            MappingsBE = mappingsBE;
            MappingViews = new ObservableCollection<MappingViewModel>();
            UpdateMappingViews();
            MappingsBE.Mappings.CollectionChanged += MappingsCollectionChanged;
        }

        public BE.MappingsModel MappingsBE { get; }

        public ObservableCollection<MappingViewModel> MappingViews { get; }

        private void MappingsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateMappingViews();
        }

        public void UpdateMappingViews()
        {
            MappingViews.Clear();
            foreach(BE.MappingModel mappingBE in MappingsBE.Mappings)
            {
                MappingViews.Add(new MappingViewModel(mappingBE, MappingsBE));
            }
        }

        public bool TryAddNewMapping()
        {
            return MappingsBE.TryAddMapping();
        }
    }
}