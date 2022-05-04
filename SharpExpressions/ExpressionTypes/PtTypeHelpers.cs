using System;
using System.Linq;

namespace SharpExpressions.ExpressionTypes
{
    /// <summary>
    /// Set of static helper methods used to pull in the PTCommand Type as an extension class.
    /// </summary>
    public static class PtTypeHelpers
    {
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruCommandType GetTypeFromLines(this string[] InputLines)
        {
            // Return the result from our joined line output.
            return GetTypeFromLines(string.Join("\n", InputLines.Select(Input => Input.TrimEnd())));
        }
        /// <summary>
        /// Finds a PTCommand type from the given input line set
        /// </summary>
        /// <param name="InputLines">Lines to find the PTCommand Type for.</param>
        /// <returns>The type of PTCommand regex to search with.</returns>
        public static PassThruCommandType GetTypeFromLines(this string InputLines)
        {
            // Find the type of command by converting all enums to string array and searching for the type.
            var EnumTypesArray = Enum.GetValues(typeof(PassThruCommandType))
                .Cast<PassThruCommandType>()
                .Select(PtEnumValue => PtEnumValue.ToString())
                .ToArray();

            // Find the return type here based on the first instance of a PTCommand type object on the array.
            var EnumStringSelected = EnumTypesArray.FirstOrDefault(InputLines.Contains);
            return (PassThruCommandType)(string.IsNullOrWhiteSpace(EnumStringSelected) ?
                PassThruCommandType.NONE : Enum.Parse(typeof(PassThruCommandType), EnumStringSelected));
        }
    }
}