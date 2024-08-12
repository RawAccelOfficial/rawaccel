﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class HiddenProfileSettingsViewModel : ViewModelBase
    {
        public HiddenProfileSettingsViewModel(BE.ProfileComponents.HiddenModel hiddenBE)
        {
            HiddenBE = hiddenBE;
            RotationField = new NamedEditableFieldViewModel(hiddenBE.RotationDegrees);
            SpeedCapField = new NamedEditableFieldViewModel(hiddenBE.SpeedCap);
            LRRatioField = new NamedEditableFieldViewModel(hiddenBE.LeftRightRatio);
            UDRatioField = new NamedEditableFieldViewModel(hiddenBE.UpDownRatio);
            AngleSnappingField = new NamedEditableFieldViewModel(hiddenBE.AngleSnappingDegrees);
        }

        protected BE.ProfileComponents.HiddenModel HiddenBE { get; }

        public NamedEditableFieldViewModel RotationField { get; set; }

        public NamedEditableFieldViewModel SpeedCapField { get; set; }

        public NamedEditableFieldViewModel LRRatioField { get; set; }
        
        public NamedEditableFieldViewModel UDRatioField { get; set; }

        public NamedEditableFieldViewModel AngleSnappingField { get; set; }
    }
}