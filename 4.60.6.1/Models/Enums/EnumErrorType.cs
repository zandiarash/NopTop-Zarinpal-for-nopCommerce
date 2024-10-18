using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NopTop.Plugin.Payments.Zarinpal.Models;
public enum EnumErrorType
{
    [Description("ناشناخته")]
    Unknown,
    [Description("عمومی")]
    Public,
    [Description("درخواست پرداخت")]
    PaymentRequest,
    [Description("تایید پرداخت")]
    PaymentVerify,
    [Description("بازگشت تراکنش")]
    PaymentReverse
}
