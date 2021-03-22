using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Revit.TestRunner.Shared
{
    public static class JsonHelper
    {
        public static JsonSerializerSettings JsonSerializerSettings => new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = new JsonConverter[] {
                new StringEnumConverter {
                    AllowIntegerValues = false
                },
                new VersionConverter()
            }
        };

        public static TContent FromFile<TContent>( string aPath )
        {
            TContent result = default;

            if( File.Exists( aPath ) ) {
                string jsonString = FileHelper.ReadStringWithLock( aPath );

                result = FromString<TContent>( jsonString );
            }
            return result;
        }

        public static object FromFile( string aPath, Type type )
        {
            object result = null;

            if( File.Exists( aPath ) ) {
                string jsonString = FileHelper.ReadStringWithLock( aPath );

                result = FromString( jsonString, type );
            }
            return result;
        }

        public static TContent FromString<TContent>( string aJsonString )
        {
            if( aJsonString == null ) throw new ArgumentNullException();

            TContent result = JsonConvert.DeserializeObject<TContent>( aJsonString, JsonSerializerSettings );

            return result;
        }

        public static object FromString( string aJsonString, Type type )
        {
            if( aJsonString == null ) throw new ArgumentNullException();

            object result = JsonConvert.DeserializeObject( aJsonString, type, JsonSerializerSettings );

            return result;
        }

        public static void ToFile<TContent>( string aFilePath, TContent aContent )
        {
            if( string.IsNullOrEmpty( aFilePath ) ) throw new ArgumentNullException();

            string json = ToString( aContent );

            FileHelper.WriteStringWithLock( aFilePath, json );
        }

        public static string ToString<TContent>( TContent aContent )
        {
            string result = null;

            result = JsonConvert.SerializeObject( aContent, JsonSerializerSettings );
            return result;
        }
    }
}
