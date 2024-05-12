﻿using Avalonia.Input;
using Avalonia.Interactivity;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model.EditableSettings;

namespace userinterface.ViewModels
{
    public class EditableFieldViewModel : ViewModelBase
    {
        private string _name;

        public EditableFieldViewModel(string name, IEditableSetting editableSetting)
        {
            _name = name;
            EditableSetting = editableSetting;
            SetValueTextFromEditableSetting();
        }

        public string Name { get => $"{_name}: "; }

        public string ValueText { get; set; }

        public IEditableSetting EditableSetting { get; }
 
        public void TakeValueTextAsNewValue()
        {
            if (!EditableSetting.TryParseAndSet(ValueText))
            {
                //TODO throw new exception here
            }

            SetValueTextFromEditableSetting();
        }

        protected void SetValueTextFromEditableSetting()
        {
            ValueText = EditableSetting.EditedValueForDiplay;
        }
    }
}