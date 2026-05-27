using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class EditableSettingsListTests
    {
        #region TestClasses

        public class TestData
        {
            public string Name { get; set; }

            public int Property { get; set; }
        }

        public interface IEditableSettingsTestCollection : IEditableSettingsCollectionSpecific<TestData>
        {
            public IEditableSettingSpecific<string> NameSetting { get; }

            public IEditableSettingSpecific<int> PropertySetting { get; }
        }

        public interface IEditableSettingsTestList : IEditableSettingsList<IEditableSettingsTestCollection, TestData>
        {
        }

        public class EditableSettingsTestCollection : EditableSettingsCollectionV2<TestData>, IEditableSettingsTestCollection
        {
            public const string NameSettingName = $"{nameof(EditableSettingsTestCollection)}.{nameof(NameSetting)}";
            public const string ProperySettingName = $"{nameof(EditableSettingsTestCollection)}.{nameof(PropertySetting)}";

            public EditableSettingsTestCollection(
                [FromKeyedServices(NameSettingName)]IEditableSettingSpecific<string> nameSetting,
                [FromKeyedServices(ProperySettingName)]IEditableSettingSpecific<int> propertySetting)
                : base([propertySetting], [])
                
            {
                NameSetting = nameSetting;
                PropertySetting = propertySetting;
            }

            public IEditableSettingSpecific<string> NameSetting { get; }

            public IEditableSettingSpecific<int> PropertySetting { get; }

            public override TestData MapToData()
            {
                return new TestData()
                {
                    Property = PropertySetting.ModelValue,
                };
            }

            protected override bool TryMapEditableSettingsCollectionsFromData(TestData data)
            {
                return PropertySetting.TryUpdateModelDirectly(data.Property)
                    & NameSetting.TryUpdateModelDirectly(data.Name);
            }

            protected override bool TryMapEditableSettingsFromData(TestData data)
            {
                return true;
            }
        }

        public class EditableSettingsTestList : EditableSettingsList<IEditableSettingsTestCollection, TestData>, IEditableSettingsTestList
        {
            public const string TestNameTemplate = "TestData";

            public EditableSettingsTestList(IServiceProvider serviceProvider)
                : base(serviceProvider, [], [])
            {
            }

            protected override string DefaultNameTemplate => TestNameTemplate;

            public override IEnumerable<TestData> MapToData()
            {
                return ElementsInternal.Select(e => e.MapToData());
            }

            protected override string GetNameFromData(TestData data)
            {
                return data.Name;
            }

            protected override string GetNameFromElement(IEditableSettingsTestCollection element) => element.NameSetting.ModelValue;

            protected override void SetElementName(IEditableSettingsTestCollection element, string name)
            {
                element.NameSetting.InterfaceValue = name;
                element.NameSetting.TryUpdateFromInterface();
            }

            protected override bool TryMapEditableSettingsFromData(IEnumerable<TestData> data)
            {
                return true;
            }
        }

        #endregion TestClasses

        #region InitDI

        public (IEditableSettingsTestList, IServiceProvider) InitTestObject(
            string propertyName,
            int propertyValue,
            string nameName,
            string nameValue)
        {
            ServiceCollection services = new ServiceCollection();
            services.AddTransient<IEditableSettingsTestList, EditableSettingsTestList>();
            services.AddTransient<IEditableSettingsTestCollection, EditableSettingsTestCollection>();
            services.AddKeyedTransient<IEditableSettingSpecific<int>>(
                EditableSettingsTestCollection.ProperySettingName, (_, _) =>
                    new EditableSettingV2<int>(
                        propertyName,
                        propertyValue,
                        UserInputParsers.IntParser,
                        ModelValueValidators.DefaultIntValidator,
                        autoUpdateFromInterface: false));
            services.AddKeyedTransient<IEditableSettingSpecific<string>>(
                EditableSettingsTestCollection.NameSettingName, (_, _) =>
                    new EditableSettingV2<string>(
                        nameName,
                        nameValue,
                        UserInputParsers.StringParser,
                        ModelValueValidators.DefaultStringValidator,
                        autoUpdateFromInterface: false));

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IEditableSettingsTestList testObject = serviceProvider.GetRequiredService<IEditableSettingsTestList>();
            return (testObject, serviceProvider);
        }

        #endregion InitDI

        #region Tests

        [TestMethod]
        public void EditableSettingsList_Construction()
        {
            string propertyName = "Property";
            int propertyInitialValue = 2;
            string nameName = "Name";
            string nameInitialValue = "My test data list";
            (IEditableSettingsTestList testObject, _) = InitTestObject(
                propertyName,
                propertyInitialValue,
                nameName,
                nameInitialValue);
            
            Assert.IsNotNull(testObject);
            Assert.IsNotNull(testObject.Elements);
            Assert.AreEqual(0, testObject.Elements.Count);
        }

        [TestMethod]
        public void EditableSettingsList_AddRemoveElements()
        {
            string propertyName = "Property";
            int propertyInitialValue = 2;
            string nameName = "Name";
            string nameInitialValue = "My test data list";
            (IEditableSettingsTestList testObject, IServiceProvider serviceProvider) = InitTestObject(
                propertyName,
                propertyInitialValue,
                nameName,
                nameInitialValue);
            
            Assert.IsNotNull(testObject);
            Assert.IsNotNull(testObject.Elements);
            Assert.AreEqual(0, testObject.Elements.Count);

            // Test case: add one default element
            testObject.TryAddNewDefault();
            Assert.AreEqual(1, testObject.Elements.Count);
            IEditableSettingsTestCollection firstElement = testObject.Elements[0];
            Assert.IsNotNull(firstElement);
            Assert.AreEqual($"{EditableSettingsTestList.TestNameTemplate}1", firstElement.NameSetting.ModelValue);
            Assert.AreEqual(propertyInitialValue, firstElement.PropertySetting.ModelValue);

            // Test case: add second default element
            testObject.TryAddNewDefault();
            Assert.AreEqual(2, testObject.Elements.Count);
            IEditableSettingsTestCollection secondElement = testObject.Elements[1];
            Assert.IsNotNull(secondElement );
            Assert.AreEqual($"{EditableSettingsTestList.TestNameTemplate}2", secondElement.NameSetting.ModelValue);
            Assert.AreEqual(propertyInitialValue, secondElement.PropertySetting.ModelValue);

            // Test case: add custom-created element
            IEditableSettingsTestCollection elementToAdd = serviceProvider.GetRequiredService<IEditableSettingsTestCollection>();
            bool result = testObject.TryAdd(elementToAdd);
            Assert.IsTrue(result);
            Assert.AreEqual(3, testObject.Elements.Count);
            IEditableSettingsTestCollection thirdElement = testObject.Elements[2];
            Assert.AreEqual(elementToAdd, thirdElement);
            Assert.AreEqual(nameInitialValue, elementToAdd.NameSetting.ModelValue);

            // Test case: add element with same name (should fail)
            IEditableSettingsTestCollection duplicateElementToAdd = serviceProvider.GetRequiredService<IEditableSettingsTestCollection>();
            result = testObject.TryAdd(duplicateElementToAdd);
            Assert.IsFalse(result);
            Assert.AreEqual(3, testObject.Elements.Count);

            // Test case: remove element
            result = testObject.TryRemoveElement(elementToAdd);
            Assert.IsTrue(result);
            Assert.AreEqual(2, testObject.Elements.Count);

            // Test case: remove element that was never added (should fail)
            result = testObject.TryRemoveElement(duplicateElementToAdd);
            Assert.IsFalse(result);
            Assert.AreEqual(2, testObject.Elements.Count);
        }

        [TestMethod]
        public void EditableSettingsList_MapFromData_RemovesStaleElementsAndUpdatesKept()
        {
            // Regression: the list previously only added/updated on data load and
            // never removed elements that the new data no longer contained.
            (IEditableSettingsTestList testObject, _) = InitTestObject("Property", 2, "Name", "seed");

            testObject.TryMapFromData(new[]
            {
                new TestData { Name = "A", Property = 1 },
                new TestData { Name = "B", Property = 2 },
                new TestData { Name = "C", Property = 3 },
            });
            Assert.AreEqual(3, testObject.Elements.Count);

            // Reload with a subset: C must be dropped, A and B kept and updated.
            testObject.TryMapFromData(new[]
            {
                new TestData { Name = "A", Property = 10 },
                new TestData { Name = "B", Property = 20 },
            });

            Assert.AreEqual(2, testObject.Elements.Count);
            CollectionAssert.AreEquivalent(
                new[] { "A", "B" },
                testObject.Elements.Select(e => e.NameSetting.ModelValue).ToList());
            Assert.IsFalse(testObject.TryGetElement("C", out _), "Stale element C should have been removed.");
            Assert.IsTrue(testObject.TryGetElement("A", out var a) && a!.PropertySetting.ModelValue == 10);
        }

        #endregion Tests
    }
}
