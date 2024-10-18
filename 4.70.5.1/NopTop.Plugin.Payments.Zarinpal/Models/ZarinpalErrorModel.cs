using NopTop.Plugin.Payments.Zarinpal.Models;

namespace NopTop.Plugin.Payments.Zarinpal;
public class ErrorDescription
{
    public ErrorDescription(EnumErrorType errorType, int code, string en, string fa)
    {
        ErrorType = errorType;
        Code = code;
        EN = en;
        FA = fa;
    }

    public EnumErrorType ErrorType { get; }
    public int Code { get; }
    public string FA { get; }
    public string EN { get; }
}