using System;
using System.ComponentModel;
using System.Reflection;

namespace NopTop.Plugin.Payments.Zarinpal;
public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        FieldInfo field = value.GetType().GetField(value.ToString());
        if (field != null)
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute != null)
            {
                return attribute.Description;
            }
        }
        return value.ToString();
    }
}