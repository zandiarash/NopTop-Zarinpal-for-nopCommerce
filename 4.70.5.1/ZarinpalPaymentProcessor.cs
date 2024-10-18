using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Stores;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Web.Framework.Menu;
using NopTop.Plugin.Payments.Zarinpal.Models;
using NopTop.Plugin.Payments.Zarinpal.Components;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

namespace NopTop.Plugin.Payments.Zarinpal;

public class ZarinPalPaymentProcessor : BasePlugin, IPaymentMethod, IAdminMenuPlugin
{
    #region Constants

    public static readonly HttpClient ClientZarinPal = new HttpClient();

    #endregion

    #region Fields
    private readonly CustomerSettings _customerSettings;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IWebHelper _webHelper;
    private readonly ZarinpalPaymentSettings _zarinPalPaymentSettings;
    private readonly ILanguageService _languageService;
    private readonly IStoreService _storeService;
    private readonly ICustomerService _customerService;
    private IWorkContext _workContext;
    private readonly IStoreContext _storeContext;
    private readonly IAddressService _addressService;


    #endregion

    #region Ctor

    public ZarinPalPaymentProcessor(
        IHttpContextAccessor httpContextAccessor,
        ILocalizationService localizationService,
        ISettingService settingService,
        ITaxService taxService,
        IWebHelper webHelper,
        ZarinpalPaymentSettings zarinPalPaymentSettings,
        ILanguageService languageService,
        IStoreService storeService,
        ICustomerService customerService,
        IWorkContext workContext,
        IStoreContext storeContext,
        CustomerSettings customerSettings,
        IAddressService addressService
        )
    {
        _httpContextAccessor = httpContextAccessor;
        _workContext = workContext;
        _customerService = customerService;
        _storeService = storeService;
        _localizationService = localizationService;
        _settingService = settingService;
        _webHelper = webHelper;
        _zarinPalPaymentSettings = zarinPalPaymentSettings;
        _storeContext = storeContext;
        _languageService = languageService;
        _customerSettings = customerSettings;
        _addressService = addressService;
    }

    #endregion

    #region Utilities
    #endregion

    #region Methods

    Task<ProcessPaymentResult> IPaymentMethod.ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        var result = new ProcessPaymentResult();
        result.NewPaymentStatus = PaymentStatus.Pending;
        return Task.FromResult(result);
    }

    public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
    {

        var language = _languageService.GetTwoLetterIsoLanguageName(await _workContext.GetWorkingLanguageAsync());

        Customer customer = await _customerService.GetCustomerByIdAsync(postProcessPaymentRequest.Order.CustomerId);
        Order order = postProcessPaymentRequest.Order;
        var store = await _storeService.GetStoreByIdAsync(order.StoreId);

        var total = Convert.ToInt32(Math.Round(order.OrderTotal, 2));
        if (_zarinPalPaymentSettings.RialToToman)
            total = total / 10;

        var phoneOfUser = string.Empty;
        var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId);
        var shippingAddress = await _addressService.GetAddressByIdAsync(order.ShippingAddressId ?? 0);

        if (_customerSettings.PhoneEnabled)// Default Phone number of the Customer
            phoneOfUser = customer.Phone;
        if (string.IsNullOrEmpty(phoneOfUser))//Phone number of the BillingAddress
            phoneOfUser = billingAddress.PhoneNumber;
        if (string.IsNullOrEmpty(phoneOfUser))//Phone number of the ShippingAddress
            phoneOfUser = string.IsNullOrEmpty(shippingAddress?.PhoneNumber) ? phoneOfUser : $"{phoneOfUser} - {shippingAddress.PhoneNumber}";

        var nameFamily = $"{customer.FirstName ?? ""} {customer.LastName ?? ""}".Trim();
        var zarinGate = _zarinPalPaymentSettings.UseZarinGate ? _zarinPalPaymentSettings.ZarinGateType.ToString() : null;
        var description = $"{store.Name}{(string.IsNullOrEmpty(nameFamily) ? "" : $" - {nameFamily}")} - {customer.Email}{(string.IsNullOrEmpty(phoneOfUser) ? "" : $" - {phoneOfUser}")}";
        var callbackURL = string.Concat(_webHelper.GetStoreLocation(), "Plugins/PaymentZarinpal/ResultHandler", "?OGUId=" + postProcessPaymentRequest.Order.OrderGuid);
        var storeAddress = _webHelper.GetStoreLocation();


        var url = $"https://{(_zarinPalPaymentSettings.UseSandbox ? "sandbox" : "payment")}.zarinpal.com/pg/v4/payment/request.json";

        var requestData = new
        {
            merchant_id = _zarinPalPaymentSettings.MerchantID,
            amount = total,
            currency = "IRT",
            callback_url = callbackURL,
            description = description,
            metadata = new
            {
                mobile = phoneOfUser,
                email = customer.Email
            }
        };

        var paymentRequestJsonValue = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");
        var response = ClientZarinPal.PostAsync(url, paymentRequestJsonValue).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;

        RestRequestModel restRequestModel =
                     JsonConvert.DeserializeObject<RestRequestModel>(responseString);

        var uri = new Uri(ZarinpalHelper.ProduceRedirectUrl(storeAddress,
            restRequestModel?.data.code,
            _zarinPalPaymentSettings.UseSandbox,
            restRequestModel.data.authority,
            zarinGate, language));

        _httpContextAccessor.HttpContext.Response.Redirect(uri.AbsoluteUri);
    }

    public async Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
    {
        var hide = string.IsNullOrWhiteSpace(_zarinPalPaymentSettings.MerchantID);
        if (_zarinPalPaymentSettings.BlockOverseas)
            hide = hide || ZarinpalHelper.IsOverseaseIp(_httpContextAccessor.HttpContext.Connection.RemoteIpAddress);
        return hide;
    }

    public override string GetConfigurationPageUrl()
    {
        return $"{_webHelper.GetStoreLocation()}Admin/Paymentzarinpal/Configure";
    }

    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new ZarinpalPaymentSettings
        {
            UseSandbox = true,
            MerchantID = "99999999-9999-9999-9999-999999999999",
            BlockOverseas = false,
            RialToToman = true,
            Method = EnumMethod.REST,
            UseZarinGate = false,
            ZarinGateType = EnumZarinGate.ZarinGate
        });


        string zarinGateLink = "https://www.zarinpal.com/blog/زرین-گیت،-درگاهی-اختصاصی-به-نام-وبسایت/";

        await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
        {
            ["Plugins.Payments.Zarinpal.Fields.ZarinGate.ContactInformation"] = "Contact Information",
            ["Plugins.Payments.Zarinpal.Fields.ZarinGate.Use"] = "Use ZarinGate",
            ["Plugins.Payments.Zarinpal.Fields.ZarinGate.Type"] = "Select ZarinGate Type",

            ["Plugins.Payments.Zarinpal.Fields.ZarinGate.Instructions"] = $"Read About the <a href=\"{zarinGateLink}\">Zarin Gate</a> Then Select the ZarinGateLink type from below :",

            ["Plugins.Payments.ZarinPal.Fields.Method"] = "Communication Method",
            ["Plugins.Payments.ZarinPal.Fields.Method.REST"] = "REST(recommanded)",
            ["Plugins.Payments.ZarinPal.Fields.Method.SOAP"] = "SOAP(Discontinued By Zarinpal)",

            ["Plugins.Payments.ZarinPal.Fields.UseSandbox"] = "Use Snadbox for testing payment GateWay without real paying.",
            ["Plugins.Payments.ZarinPal.Fields.MerchantID"] = "GateWay Merchant ID",
            ["Plugins.Payments.ZarinPal.Instructions"] = string.Concat("You can use Zarinpal.com GateWay as a payment gateway. Zarinpal is not a bank but it is an interface which customers can pay with.",
                "<br/>", "Please consider that if you leave MerchantId field empty the Zarinpal Gateway will be hidden and not choosable when checking out"),

            ["plugins.payments.zarinpal.PaymentMethodDescription"] = "ZarinPal, The Bank Interface",
            ["Plugins.Payments.Zarinpal.Fields.RedirectionTip"] = "You will be redirected to ZarinPal site to complete the order.",
            ["Plugins.Payments.Zarinpal.Fields.BlockOverseas"] = "Block oversease access (block non Iranians)",
            ["Plugins.Payments.Zarinpal.Fields.RialToToman"] = "Convert Rial To Toman",
            ["Plugins.Payments.Zarinpal.Fields.RialToToman.Instructions"] =
                string.Concat(
                    "The default currency of zarinpal is Toman", "<br/>",
                    "Therefore if your website uses Rial before paying it should be converted to Toman", "<br/>",
                    "please consider that to convert Rial to Toman system divides total to 10, so the last digit will be removed", "<br/>",
                    "To do the stuff check this option"
                )
        });

        var lang = (await _languageService.GetAllLanguagesAsync()).FirstOrDefault(x => x.LanguageCulture == "fa-IR");
        var rialToToman = "تبدیل ریال به تومان";
        if (lang != null)
            await _localizationService.AddOrUpdateLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.Zarinpal.Fields.ZarinGate.ContactInformation"] = "اطلاعات تماس با پشتیبانی",
                ["Plugins.Payments.Zarinpal.Fields.ZarinGate.Use"] = "استفاده از زرین گیت",
                ["Plugins.Payments.Zarinpal.Fields.ZarinGate.Type"] = "انتخاب نوع زرین گیت",

                ["Plugins.Payments.Zarinpal.Fields.ZarinGate.Instructions"] = string.Concat("لطفا اول شرایط استفاده از زرین گیت را در ", $"<a href=\"{zarinGateLink}\"> در این قسمت </a>", "مطالعه نموده و سپس نوع آن را انتخاب نمایید"),

                ["Plugins.Payments.ZarinPal.Fields.Method"] = "روش پرداخت",

                ["Plugins.Payments.ZarinPal.Fields.UseSandbox"] = "تست درگاه زرین پال بدون پرداخت هزینه",
                ["Plugins.Payments.ZarinPal.Fields.MerchantID"] = "کد پذیرنده",

                ["Plugins.Payments.ZarinPal.Instructions"] =
                    string.Concat("شما می توانید از زرین پال به عنوان یک درگاه پرداخت استفاده نمایید، زرین پال یک بانک نیست بلکه یک واسط بانکی است که کاربران میتوانند از طریق آن مبلغ مورد نظر را پرداخت نمایند، باید آگاه باشید که درگاه زرین پال درصدی از پول پرداخت شده کاربران را به عنوان کارمزد دریافت میکند.",
                    "<br/>", "توجه داشته باشید که اگر فیلد کد پذیرنده خالی باشد درگاه زرین پال در هنگام پرداخت مخفی می شود و قابل انتخاب نیست"),
                ["plugins.payments.zarinpal.PaymentMethodDescription"] = "درگاه واسط زرین پال",
                ["Plugins.Payments.Zarinpal.Fields.RedirectionTip"] = "هم اکنون به درگاه بانک زرین پال منتقل می شوید.",
                ["Plugins.Payments.Zarinpal.Fields.BlockOverseas"] = "قطع دسترسی برای آی پی های خارج از کشور",
                ["Plugins.Payments.Zarinpal.Fields.RialToToman"] = rialToToman,
                ["Plugins.Payments.Zarinpal.Fields.RialToToman.Instructions"] =
                    string.Concat(
                        "در صورتی که قیمت کالاها بصورت ریال وارد شده باشد اما به کاربر بصورت تومان نمایش داده می شود", " ",
                        "شما بایستی گزینه ", $"<b>{rialToToman}</b>", " را انتخاب نمایید ", "<br/>",
                        "اما در صورتی که قیمت کالاها بصورت تومان وارد شده باشد و به کاربر نیز بصورت تومان نمایش داده می شود", " ",
                        "نیازی به انتخاب گزینه زیر نیست", "<br/>",
                        "لطفا در نظر داشته باشید که جهت تبدیل ریال به تومان عدد تقسیم بر 10 شده و در واقع رقم آخر حذف می گردد", "<br/>",
                        "در صورتی که مایل به تغییر از ریال به تومان هنگام پرداخت می باشید این گزینه را فعال نمایید"
                    )
            }, languageId: lang.Id);

        await base.InstallAsync();
    }

    public override async Task UninstallAsync()
    {
        //settings
        await _settingService.DeleteSettingAsync<ZarinpalPaymentSettings>();

        //locales
        await _localizationService.DeleteLocaleResourcesAsync(new List<string>{
                "Plugins.Payments.Zarinpal.Fields.ZarinGate.Use",
                "Plugins.Payments.Zarinpal.Fields.ZarinGate.Type",
                "Plugins.Payments.Zarinpal.Fields.ZarinGate.Instructions",
                "Plugins.Payments.ZarinPal.Fields.Method",
                "Plugins.Payments.ZarinPal.Fields.Method.REST",
                "Plugins.Payments.ZarinPal.Fields.Method.SOAP",
                "Plugins.Payments.ZarinPal.Fields.UseSandbox",
                "Plugins.Payments.ZarinPal.Fields.MerchantID",
                "plugins.payments.zarinpal.PaymentMethodDescription",
                "Plugins.Payments.ZarinPal.Instructions",
                "Plugins.Payments.Zarinpal.Fields.RedirectionTip",
                "Plugins.Payments.Zarinpal.Fields.BlockOverseas",
                "Plugins.Payments.Zarinpal.Fields.RialToToman",
                "Plugins.Payments.Zarinpal.Fields.RialToToman.Instructions",
            });

        await base.UninstallAsync();
    }

    public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
    {
        return Task.FromResult<IList<string>>(new List<string>());
    }

    public Type GetPublicViewComponent()
    {
        //return "PaymentZarinpal";
        return typeof(PaymentZarinpalViewComponent);
    }

    #endregion

    #region Properties

    public bool SupportCapture
    {
        get { return false; }
    }

    public bool SupportPartiallyRefund
    {
        get { return false; }
    }

    public bool SupportRefund
    {
        get { return false; }
    }

    public bool SupportVoid
    {
        get { return false; }
    }

    public RecurringPaymentType RecurringPaymentType
    {
        get { return RecurringPaymentType.NotSupported; }
    }

    public PaymentMethodType PaymentMethodType
    {
        get { return PaymentMethodType.Redirection; }
    }

    public bool SkipPaymentInfo
    {
        get { return false; }
    }

    public async Task<string> GetPaymentMethodDescriptionAsync()
    {
        return await _localizationService.GetResourceAsync("plugins.payments.zarinpal.PaymentMethodDescription");
    }

    #endregion
    public Task ManageSiteMapAsync(SiteMapNode rootNode)
    {
        var nopTopPluginsNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "NopTop");
        if (nopTopPluginsNode == null)
        {
            nopTopPluginsNode = new SiteMapNode()
            {
                SystemName = "NopTop",
                Title = "NopTop",
                Visible = true,
                IconClass = "fa-gear"
            };
            rootNode.ChildNodes.Add(nopTopPluginsNode);
        }

        var menueLikeProduct = new SiteMapNode()
        {
            SystemName = "ZarinPal",
            Title = "ZarinPal Configuration",
            ControllerName = "PaymentZarinPal",
            ActionName = "Configure",
            Visible = true,
            IconClass = "fa-dot-circle-o",
            RouteValues = new RouteValueDictionary() { { "Area", "Admin" } },
        };

        nopTopPluginsNode.ChildNodes.Add(menueLikeProduct);
        return Task.CompletedTask;
    }

    public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
    {
        return Task.FromResult<decimal>(0);
    }

    public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
    {
        var result = new CapturePaymentResult();
        result.AddError("Capture method not supported");
        return Task.FromResult(result);
    }

    public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
    {
        var result = new RefundPaymentResult();
        result.AddError("Refund method not supported");
        return Task.FromResult(result);
    }

    public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
    {
        var result = new VoidPaymentResult();
        result.AddError("Void method not supported");
        return Task.FromResult(result);
    }

    public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
    {
        var result = new ProcessPaymentResult();
        result.AddError("Recurring payment not supported");
        return Task.FromResult(result);
    }

    public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
    {
        var result = new CancelRecurringPaymentResult();
        result.AddError("Recurring payment not supported");
        return Task.FromResult(result);
    }

    public Task<bool> CanRePostProcessPaymentAsync(Order order)
    {
        if (order == null)
            throw new ArgumentNullException("order");

        //let's ensure that at least 5 seconds passed after order is placed
        //P.S. there's no any particular reason for that. we just do it
        if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
    {
        return Task.FromResult(new ProcessPaymentRequest());
    }
}
