using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpWrapper.J2534Objects;
using SharpWrapper.PassThruTypes;

namespace SharpSimulator.PassThruSimulationSupport
{
    /// <summary>
    /// JSON Converter for loading simulation configuration objects.
    /// This is mainly here to help improve readability for SConfig Lists
    /// </summary>
    internal class PassThruSimConfigJsonConverter : JsonConverter
    {
        /// <summary>
        /// Sets if we can convert the input object or not.
        /// </summary>
        /// <param name="ObjectType">The type of object we're converting</param>
        /// <returns>True if conversion is supported. False if it is not</returns>
        public override bool CanConvert(Type ObjectType) { return ObjectType == typeof(PassThruSimulationConfiguration); }

        /// <summary>
        /// Writes a simulation configuration object to JSON
        /// </summary>
        /// <param name="JWriter">JSON Writer to build output</param>
        /// <param name="ValueObject">The configuration object we're converting</param>
        /// <param name="JSerializer">JSON serialization settings for conversion</param>
        public override void WriteJson(JsonWriter JWriter, object? ValueObject, JsonSerializer JSerializer)
        {
            // Check if value object is null. Build output
            if (ValueObject is not PassThruSimulationConfiguration InputConfiguration) return;

            // Build an output JSON object for our simulation configuration
            JObject OutputObject = new JObject()
            {
                InputConfiguration.ConfigurationName,
                InputConfiguration.ReaderTimeout,
                InputConfiguration.ReaderMsgCount,
                InputConfiguration.ResponseTimeout,
                InputConfiguration.ReaderBaudRate,
                InputConfiguration.ReaderChannelFlags,
                InputConfiguration.ReaderProtocol,
            };

            // Now write this built object.
            JWriter.WriteRawValue(JsonConvert.SerializeObject(OutputObject, Formatting.Indented));
        }
        /// <summary>
        /// Reads a simulation configuration object from JSON
        /// </summary>
        /// <param name="JReader">JSON reader reading the input JSON contents</param>
        /// <param name="ObjectType">The type of object we're converting</param>
        /// <param name="ExistingValue">An existing object we're trying to provide context with</param>
        /// <param name="JSerializer">JSON serialization settings for conversion</param>
        /// <returns>The built simulation configuration object from the JSON input</returns>
        public override object? ReadJson(JsonReader JReader, Type ObjectType, object? ExistingValue, JsonSerializer JSerializer)
        {
            // Check if input is null. Build object from it.
            JObject InputObject = JObject.Load(JReader);
            if (InputObject.HasValues == false) { return default; }

            // TODO: Populate values for a configuration using the JObject pulled in 

            // Build and return a new simulation configuration here 
            return new PassThruSimulationConfiguration();
        }
    }
}
