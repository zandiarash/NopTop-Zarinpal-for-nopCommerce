
namespace NopTop.Plugin.Payments.Zarinpal.Models
{
    public class StatusToResult
    {
        public StatusToResult()
        {
            Message = string.Empty;
            IsOk = false;
        }
        public string Message { get; set; }
        public bool IsOk { get; set; }
    }
}