using System.Runtime.Serialization;

public enum RouteType
{
    [EnumMember(Value = "Default")]
    Default,
    [EnumMember(Value = "Content")]
    Content,
    [EnumMember(Value = "Static")]
    Static,
    [EnumMember(Value = "Parameterized")]
    Parameterized,
    [EnumMember(Value = "Dynamic")]
    Dynamic
}