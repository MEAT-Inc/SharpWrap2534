using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SharpSimulator.SimulationObjects
{
    /// <summary>
    /// Class used to convert Simulation channels into readable format in JSON
    /// </summary>
    internal class SimulationChannelJsonConverter: JsonConverter
    {
        /// <summary>
        /// Sets if we can convert the input object or not.
        /// </summary>
        /// <param name="ObjectType"></param>
        /// <returns></returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(SimulationChannel); }

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
