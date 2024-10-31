using System;
using System.Runtime.Serialization;

//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;

namespace GServer
{
    /// <summary>
    /// HTTP methods, i.e. GET, PUT, POST, DELETE, etc.
    /// </summary>
    //[JsonConverter(typeof(StringEnumConverter))]
    public enum HttpMethod
    {
        [EnumMember(Value = "GET")]
        GET,
        [EnumMember(Value = "HEAD")]
        HEAD,
        [EnumMember(Value = "PUT")]
        PUT,
        [EnumMember(Value = "POST")]
        POST,
        [EnumMember(Value = "DELETE")]
        DELETE,
        [EnumMember(Value = "PATCH")]
        PATCH,
        [EnumMember(Value = "CONNECT")]
        CONNECT,
        [EnumMember(Value = "OPTIONS")]
        OPTIONS,
        [EnumMember(Value = "TRACE")]
        TRACE,
        [EnumMember(Value = "NULL")]
        NULL//gebug костыль
    }
}
