namespace Fife.Core;

public enum FifeType
{
    Dynamic,
    Bool,
    Int,
    Float,
    String
}

/// <summary>Default values and value rules shared by every typed declaration.</summary>
public static class FifeTypes
{
    public static object? DefaultValue(FifeType type) => type switch
    {
        FifeType.Bool => false,
        FifeType.Int or FifeType.Float => 0d,
        FifeType.String => "",
        _ => null
    };

    public static bool Accepts(FifeType type, object? value) => type switch
    {
        FifeType.Bool => value is bool,
        FifeType.Int => value is double number && number == Math.Truncate(number),
        FifeType.Float => value is double,
        FifeType.String => value is string,
        _ => true
    };

    public static string Name(FifeType type) => type switch
    {
        FifeType.Bool => "bool",
        FifeType.Int => "int",
        FifeType.Float => "float",
        FifeType.String => "string",
        _ => "var"
    };

    public static string VariableRequirement(FifeType type) => type switch
    {
        FifeType.Bool => "Bool variables require a boolean value.",
        FifeType.Int => "Integer variables require an integer value.",
        FifeType.Float => "Float variables require a number value.",
        FifeType.String => "String variables require a string value.",
        _ => "Variable value does not match its declared type."
    };

    public static string ValueDescription(FifeType type) => type switch
    {
        FifeType.Bool => "a boolean value",
        FifeType.Int => "an integer value",
        FifeType.Float => "a number value",
        FifeType.String => "a string value",
        _ => "a value"
    };
}