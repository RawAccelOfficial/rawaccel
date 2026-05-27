using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using userspace_backend.Data.Profiles.Accel;
using userspace_backend.Data.Profiles;
using userspace_backend.Model.AccelDefinitions;

namespace userspace_backend.Model.EditableSettings
{
    public interface IUserInputParser<T>
    {
        bool TryParse(string input, out T parsedValue);
    }

    public class StringParser : IUserInputParser<string>
    {
        public bool TryParse(string input, out string parsedValue)
        {
            parsedValue = input;
            return true;
        }
    }

    public class IntParser : IUserInputParser<int>
    {
        public bool TryParse(string input, out int parsedValue)
        {
            if (int.TryParse(input, out int innerParsedValue))
            {
                parsedValue = innerParsedValue;
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class DoubleParser : IUserInputParser<double>
    {
        public bool TryParse(string input, out double parsedValue)
        {
            if (double.TryParse(input, out parsedValue) &&
                !double.IsNaN(parsedValue) &&
                !double.IsInfinity(parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class BoolParser : IUserInputParser<bool>
    {
        public bool TryParse(string input, out bool parsedValue)
        {
            if (bool.TryParse(input, out parsedValue))
            {
                return true;
            }

            parsedValue = default;
            return false;
        }
    }

    public class EnumParser<T> : IUserInputParser<T> where T : struct, Enum
    {
        public bool TryParse(string input, out T parsedValue)
        {
            // Enum.TryParse sets parsedValue to default on failure.
            return Enum.TryParse(input, ignoreCase: true, out parsedValue);
        }
    }

    public class LookupTableDataParser : IUserInputParser<LookupTableData>
    {
        public bool TryParse(string input, out LookupTableData parsedValue)
        {
            IEnumerable<double> ToDoubles(string[] splitInput)
            {
                foreach(string coordinateInput in splitInput)
                {
                    if (double.TryParse(coordinateInput.Trim(), out double result))
                    {
                        yield return result;
                    }
                }
            }

            try
            {
                string[] splitInput = input.Split(',');
                double[] inputCoordinates = ToDoubles(splitInput).ToArray();

                // TODO: further input point validation
                if (inputCoordinates.Length == splitInput.Length &&
                    inputCoordinates.Length % 2 == 0)
                {
                    parsedValue = new LookupTableData(inputCoordinates);
                    return true;
                }
            }
            catch
            {
                // Empty so that all code paths return value. False returned below
            }

            parsedValue = new LookupTableData();
            return false;
        }
    }
}
