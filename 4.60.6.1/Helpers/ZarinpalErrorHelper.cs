using System.Collections.Generic;
using NopTop.Plugin.Payments.Zarinpal.Models;

namespace NopTop.Plugin.Payments.Zarinpal;

public static class ZarinpalErrorHelper
{

    private static List<ErrorDescription> _errorDescriptions = new List<ErrorDescription>
    {
        // Public Errors
        new ErrorDescription(EnumErrorType.Public, -9, "Validation error", "خطای اعتبار سنجی1- مرچنت کد داخل تنظیمات وارد نشده باشد-2 آدرس بازگشت (callbackurl) وارد نشده باشد -3 توضیحات (description ) وارد نشده باشد و یا از حد مجاز 500 کارکتر بیشتر باشد -4 مبلغ پرداختی کمتر یا بیشتر از حد مجاز"),
        new ErrorDescription(EnumErrorType.Public, -10, "Terminal is not valid, please check merchant_id or ip address.", "ای پی یا مرچنت كد پذیرنده صحیح نیست."),
        new ErrorDescription(EnumErrorType.Public, -11, "Terminal is not active, please contact our support team.", "مرچنت کد فعال نیست، پذیرنده مشکل خود را به امور مشتریان زرین‌پال ارجاع دهد."),
        new ErrorDescription(EnumErrorType.Public, -12, "Too many attempts, please try again later.", "تلاش بیش از دفعات مجاز در یک بازه زمانی کوتاه به امور مشتریان زرین پال اطلاع دهید"),
        new ErrorDescription(EnumErrorType.Public, -15, "Terminal user is suspended: (please contact our support team).", "درگاه پرداخت به حالت تعلیق در آمده است، پذیرنده مشکل خود را به امور مشتریان زرین‌پال ارجاع دهد."),
        new ErrorDescription(EnumErrorType.Public, -16, "Terminal user level is not valid: (please contact our support team).", "سطح تایید پذیرنده پایین تر از سطح نقره ای است."),
        new ErrorDescription(EnumErrorType.Public, -17, "Terminal user level is not valid: (please contact our support team).", "محدودیت پذیرنده در سطح آبی"),
        new ErrorDescription(EnumErrorType.Public, 100, "Success", "عملیات موفق"),

        // PaymentRequest Errors
        new ErrorDescription(EnumErrorType.PaymentRequest, -30, "Terminal does not allow to accept floating wages.", "پذیرنده اجازه دسترسی به سرویس تسویه اشتراکی شناور را ندارد."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -31, "Terminal does not allow to accept wages, please add default bank account in panel.", "حساب بانکی تسویه را به پنل اضافه کنید. مقادیر وارد شده برای تسهیم درست نیست. پذیرنده جهت استفاده از خدمات سرویس تسویه اشتراکی شناور، باید حساب بانکی معتبری به پنل کاربری خود اضافه نماید."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -32, "Wages is not valid, Total wages(floating) has been overloaded max amount.", "مبلغ وارد شده از مبلغ کل تراکنش بیشتر است."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -33, "Wages floating is not valid.", "درصدهای وارد شده صحیح نیست."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -34, "Wages is not valid, Total wages(fixed) has been overloaded max amount.", "مبلغ وارد شده از مبلغ کل تراکنش بیشتر است."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -35, "Wages is not valid, Total wages(floating) has reached the limit in max parts.", "تعداد افراد دریافت کننده تسهیم بیش از حد مجاز است."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -36, "The minimum amount for wages(floating) should be 10,000 Rials", "حداقل مبلغ جهت تسهیم باید ۱۰۰۰۰ ریال باشد."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -37, "One or more IBAN entered for wages(floating) from the bank side are inactive.", "یک یا چند شماره شبای وارد شده برای تسهیم از سمت بانک غیر فعال است."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -38, "Wages need to set IBAN in shaparak.", "خطا٬عدم تعریف صحیح شبا٬لطفا دقایقی دیگر تلاش کنید."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -39, "Wages have an error!", "خطایی رخ داده است به امور مشتریان زرین پال اطلاع دهید."),
        new ErrorDescription(EnumErrorType.PaymentRequest, -40, "Invalid extra params, expire_in is not valid.", ""),
        new ErrorDescription(EnumErrorType.PaymentRequest, -41, "Maximum amount is 100,000,000 tomans.", "حداکثر مبلغ پرداختی ۱۰۰ میلیون تومان است."),

        // PaymentVerify Errors
        new ErrorDescription(EnumErrorType.PaymentVerify, -50, "Session is not valid, amounts values are not the same.", "مبلغ پرداخت شده با مقدار مبلغ ارسالی در متد وریفای متفاوت است."),
        new ErrorDescription(EnumErrorType.PaymentVerify, -51, "Session is not valid, session is not active paid try.", "پرداخت ناموفق"),
        new ErrorDescription(EnumErrorType.PaymentVerify, -52, "Oops!!, please contact our support team", "خطای غیر منتظره‌ای رخ داده است. پذیرنده مشکل خود را به امور مشتریان زرین‌پال ارجاع دهد."),
        new ErrorDescription(EnumErrorType.PaymentVerify, -53, "Session is not this merchant_id session.", "پرداخت متعلق به این مرچنت کد نیست."),
        new ErrorDescription(EnumErrorType.PaymentVerify, -54, "Invalid authority.", "اتوریتی نامعتبر است."),
        new ErrorDescription(EnumErrorType.PaymentVerify, -55, "Manual payment request not found.", "تراکنش مورد نظر یافت نشد."),
        new ErrorDescription(EnumErrorType.PaymentVerify, 101, "Verified", "تراکنش وریفای شده است."),

        // PaymentReverse Errors
        new ErrorDescription(EnumErrorType.PaymentReverse, -60, "Session cannot be reversed with the bank.", "امکان ریورس کردن تراکنش با بانک وجود ندارد."),
        new ErrorDescription(EnumErrorType.PaymentReverse, -61, "Session is not in success status.", "تراکنش موفق نیست یا قبلا ریورس شده است."),
        new ErrorDescription(EnumErrorType.PaymentReverse, -62, "Terminal IP limit must be active.", "آی پی درگاه ست نشده است."),
        new ErrorDescription(EnumErrorType.PaymentReverse, -63, "Maximum time for reversing this session has expired.", "حداکثر زمان (۳۰ دقیقه) برای ریورس کردن این تراکنش منقضی شده است.")
    };

    public static StatusToResult StatusToResult(int code, string lang)
    {
        StatusToResult statusToResult = new StatusToResult
        {
            IsOk = code == 100 || code == 101,
        };

        var errorDescription = _errorDescriptions.Find(ed => ed.Code == code);

        if (errorDescription == null)
        {
            statusToResult.Message = "خطای ناشناخته";
            statusToResult.ErrorType = EnumErrorType.Unknown;
        }
        else
        {
            statusToResult.Message = lang == "fa" ? errorDescription.FA : errorDescription.EN;
            statusToResult.ErrorType = errorDescription.ErrorType;
        }

        return statusToResult;
    }
}
