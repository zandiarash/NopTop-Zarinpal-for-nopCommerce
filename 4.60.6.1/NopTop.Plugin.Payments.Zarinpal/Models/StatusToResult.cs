
namespace NopTop.Plugin.Payments.Zarinpal.Models;
public class StatusToResult
{
    public StatusToResult()
    {
        Message = "Unknown Error";
        IsOk = false;
    }
    public string Message { get; set; }
    public EnumErrorType ErrorType { get; set; }
    public bool IsOk { get; set; }
}