using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend_tests.ModelTests
{
    [TestClass]
    public class EditableSettingsTests
    {
        #region Init

        public static EditableSettingV2<int> InitTestObject(
            string testSettingName,
            int testSettingInitialValue,
            bool autoUpddateFromInterface = false)
        {
            return new EditableSettingV2<int>(
                testSettingName,
                testSettingInitialValue,
                UserInputParsers.IntParser,
                ModelValueValidators.DefaultIntValidator,
                autoUpdateFromInterface: autoUpddateFromInterface);
        }

        #endregion Init

        #region Tests

        [TestMethod]
        public void EditableSetting_Construction()
        {
            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;

            EditableSettingV2<int> testObject = InitTestObject(testSettingName, testSettingInitialValue);

            Assert.IsNotNull(testObject);
            Assert.AreEqual(testSettingInitialValue, testObject.ModelValue);
            Assert.AreEqual(testSettingName, testObject.DisplayName);
        }

        [TestMethod]
        public void EditableSetting_SetFromInterface()
        {
            int propertyChangedHookCalls = 0;

            void TestObject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (string.Equals(e.PropertyName, nameof(EditableSettingV2<IComparable>.ModelValue)))
                {
                    propertyChangedHookCalls++;
                }
            }

            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;
            int testSettingSecondValue = 500;

            EditableSettingV2<int> testObject = InitTestObject(testSettingName, testSettingInitialValue);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = testSettingSecondValue.ToString();
            bool updateResult = testObject.TryUpdateFromInterface();

            Assert.IsTrue(updateResult);
            Assert.AreEqual(testSettingSecondValue, testObject.ModelValue);
            Assert.AreEqual(1, propertyChangedHookCalls);
        }

        [TestMethod]
        public void EditableSetting_SetFromInterface_Automatic()
        {
            int propertyChangedHookCalls = 0;

            void TestObject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (string.Equals(e.PropertyName, nameof(EditableSettingV2<IComparable>.ModelValue)))
                {
                    propertyChangedHookCalls++;
                }
            }

            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;
            int testSettingSecondValue = 500;

            EditableSettingV2<int> testObject = InitTestObject(testSettingName, testSettingInitialValue, autoUpddateFromInterface: true);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = testSettingSecondValue.ToString();

            Assert.AreEqual(testSettingSecondValue, testObject.ModelValue);
            Assert.AreEqual(1, propertyChangedHookCalls);
        }

        [TestMethod]
        public void EditableSetting_SetFromInterface_BadValue()
        {
            int propertyChangedHookCalls = 0;

            void TestObject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (string.Equals(e.PropertyName, nameof(EditableSettingV2<IComparable>.ModelValue)))
                {
                    propertyChangedHookCalls++;
                }
            }

            string testSettingName = "Test Setting";
            int testSettingInitialValue = 0;

            // Test case: bad value, cannot parse
            EditableSettingV2<int> testObject = InitTestObject(testSettingName, testSettingInitialValue);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = "ASDFJLKL";
            bool updateResult = testObject.TryUpdateFromInterface();

            Assert.IsFalse(updateResult);
            Assert.AreEqual(testSettingInitialValue, testObject.ModelValue);
            Assert.AreEqual(0, propertyChangedHookCalls);

            // Test case: validator determines input is invalid
            testObject = 
                new EditableSettingV2<int>(
                    testSettingName,
                    testSettingInitialValue,
                    UserInputParsers.IntParser,
                    new AllChangeInvalidValueValidator<int>(),
                    autoUpdateFromInterface: false);

            testObject.PropertyChanged += TestObject_PropertyChanged;
            testObject.InterfaceValue = 500.ToString();
            updateResult = testObject.TryUpdateFromInterface();

            Assert.IsFalse(updateResult);
            Assert.AreEqual(testSettingInitialValue, testObject.ModelValue);
            Assert.AreEqual(0, propertyChangedHookCalls);
        }

        [TestMethod]
        public void EditableSetting_HasChanged_ReflectsWhetherValueDiffersFromLastWritten()
        {
            // Regression: HasChanged() previously returned CompareTo == 0, i.e. true
            // when the value was UNCHANGED (inverted relative to its name).
            EditableSettingV2<int> testObject = InitTestObject("Test Setting", 0);

            Assert.IsFalse(testObject.HasChanged(), "An untouched setting must not report as changed.");

            Assert.IsTrue(testObject.TryUpdateModelDirectly(500));

            Assert.IsTrue(testObject.HasChanged(), "After changing away from the initial value, must report as changed.");
        }

        #endregion Tests
    }
}
