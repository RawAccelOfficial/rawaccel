//using System;
//using Avalonia.Data;
//using Avalonia.Markup.Xaml;
//using Avalonia.Markup.Xaml.MarkupExtensions;
//using userinterface.Services;

//namespace userinterface.Extensions
//{
//    public class LocalizeExtension : MarkupExtension
//    {
//        public string Key { get; set; } = string.Empty;

//        public override object ProvideValue(IServiceProvider serviceProvider)
//        {
//            var localizationService = App.Current?.Services?.GetService(typeof(ILocalizationService)) as ILocalizationService;
            
//            if (localizationService == null)
//                return Key;

//            return new LocalizedBinding(Key, localizationService);
//        }
//    }

//    public class LocalizedBinding : IBinding
//    {
//        private readonly string key;
//        private readonly ILocalizationService localizationService;

//        public LocalizedBinding(string key, ILocalizationService localizationService)
//        {
//            this.key = key;
//            this.localizationService = localizationService;
//        }

//        public InstancedBinding Initiate(AvaloniaObject target, AvaloniaProperty targetProperty, object anchor = null, bool enableDataValidation = false)
//        {
//            return new LocalizedInstancedBinding(key, localizationService, target, targetProperty);
//        }
//    }

//    public class LocalizedInstancedBinding : InstancedBinding
//    {
//        private readonly string key;
//        private readonly ILocalizationService localizationService;
//        private readonly AvaloniaObject target;
//        private readonly AvaloniaProperty targetProperty;

//        public LocalizedInstancedBinding(string key, ILocalizationService localizationService, AvaloniaObject target, AvaloniaProperty targetProperty)
//        {
//            this.key = key;
//            this.localizationService = localizationService;
//            this.target = target;
//            this.targetProperty = targetProperty;

//            localizationService.CultureChanged += OnCultureChanged;
//            UpdateValue();
//        }

//        private void OnCultureChanged(object sender, System.Globalization.CultureInfo e)
//        {
//            UpdateValue();
//        }

//        private void UpdateValue()
//        {
//            var value = localizationService.GetString(key);
//            target.SetValue(targetProperty, value);
//        }

//        protected override void Unsubscribe()
//        {
//            localizationService.CultureChanged -= OnCultureChanged;
//        }
//    }
//}