using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SharpWrap2534.J2534Objects;

namespace SharpWrap2534.SupportingLogic
{
    /// <summary>
    /// Converts a J2534 filter object around with a specified format routine
    /// </summary>
    public class PtFilterJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert the input object or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(J2534Filter); }

        /// <summary>
        /// Writes a J2534 Filter object to JSON
        /// </summary>
        /// <param name="JWriter"></param>
        /// <param name="ValueObject"></param>
        /// <param name="JSerializer"></param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Reads a J2534 object from JSON
        /// </summary>
        /// <param name="JReader"></param>
        /// <param name="ObjectType"></param>
        /// <param name="ExistingValue"></param>
        /// <param name="JSerializer"></param>
        /// <returns></returns>
        public override object? ReadJson(JsonReader JReader, Type ObjectType, object? ExistingValue, JsonSerializer JSerializer)
        {
            throw new NotImplementedException();
        }
    }
}
