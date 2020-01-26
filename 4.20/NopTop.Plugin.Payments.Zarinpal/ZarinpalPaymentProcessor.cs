using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Stores;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Web.Framework.Menu;
using NopTop.Plugin.Payments.Zarinpal.Models;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;

namespace NopTop.Plugin.Payments.Zarinpal
{
    /// <summary>
    /// Zarinpal payment processor
    /// </summary>
    public class ZarinPalPaymentProcessor : BasePlugin, IPaymentMethod, IAdminMenuPlugin
    {
        #region Constants

        public static readonly HttpClient clientZarinPal = new HttpClient();

        #endregion

        #region Fields
        private readonly CustomerSettings _customerSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPaymentService _paymentService;
        private readonly CurrencySettings _currencySettings;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICurrencyService _currencyService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly ZarinpalPaymentSettings _ZarinPalPaymentSettings;
        private readonly ILanguageService _languageService;
        private readonly IStoreService _storeService;
        private readonly ICustomerService _customerService;
        private IWorkContext _workContext;
        private readonly IStoreContext _storeContext;


        #endregion

        #region Ctor

        public ZarinPalPaymentProcessor(CurrencySettings currencySettings,
            IHttpContextAccessor httpContextAccessor,
            IPaymentService paymentService,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICurrencyService currencyService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            ITaxService taxService,
            IWebHelper webHelper,
            ZarinpalPaymentSettings ZarinPalPaymentSettings,
            ILanguageService languageService,
            IStoreService storeService,
            ICustomerService customerService,
            IWorkContext workContext,
            IStoreContext storeContext,
            CustomerSettings _customerSettings
            )
        {
            this._paymentService = paymentService;
            this._httpContextAccessor = httpContextAccessor;
            this._workContext = workContext;
            this._customerService = customerService;
            this._storeService = storeService;
            this._currencySettings = currencySettings;
            this._checkoutAttributeParser = checkoutAttributeParser;
            this._currencyService = currencyService;
            this._genericAttributeService = genericAttributeService;
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._settingService = settingService;
            this._taxService = taxService;
            this._webHelper = webHelper;
            this._ZarinPalPaymentSettings = ZarinPalPaymentSettings;
            this._storeContext = storeContext;
            this._languageService = languageService;
            this._customerSettings = _customerSettings;
        }

        #endregion

        #region Utilities
        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            Customer customer = postProcessPaymentRequest.Order.Customer;
            Order order = postProcessPaymentRequest.Order;

            var total = Convert.ToInt32(Math.Round(order.OrderTotal, 2));
            if (_ZarinPalPaymentSettings.RialToToman) total = total / 10;

            string PhoneOfUser = String.Empty;
            if (_customerSettings.PhoneEnabled)// Default Phone number of the Customer
                PhoneOfUser = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.PhoneAttribute);
            if (string.IsNullOrEmpty(PhoneOfUser))//Phone number of the BillingAddress
                PhoneOfUser = order.BillingAddress.PhoneNumber;
            if (string.IsNullOrEmpty(PhoneOfUser))//Phone number of the First Address
                PhoneOfUser = _workContext.CurrentCustomer.Addresses.FirstOrDefault()?.PhoneNumber ?? "";

            var Name = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.FirstNameAttribute);
            var Family = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.LastNameAttribute);

            string NameFamily = $"{Name ?? ""} {Family ?? ""}".Trim();
            string urlToRedirect = "";
            string ZarinGate = _ZarinPalPaymentSettings.UseZarinGate ? _ZarinPalPaymentSettings.ZarinGateType.ToString() : null;
            string Description = $"{_storeService.GetStoreById(order.StoreId).Name}{(string.IsNullOrEmpty(NameFamily) ? "" : $" - {NameFamily}")} - {customer.Email}{(string.IsNullOrEmpty(PhoneOfUser) ? "" : $" - {PhoneOfUser}")}";
            string CallbackURL = string.Concat(_webHelper.GetStoreLocation(), "Plugins/PaymentZarinpal/ResultHandler", "?OGUId=" + postProcessPaymentRequest.Order.OrderGuid);
            string StoreAddress = _webHelper.GetStoreLocation();

            if (_ZarinPalPaymentSettings.Method == EnumMethod.SOAP)
            {
                if (_ZarinPalPaymentSettings.UseSandbox)
                    using (ServiceReferenceZarinpalSandBox.PaymentGatewayImplementationServicePortTypeClient ZpalSr = new ServiceReferenceZarinpalSandBox.PaymentGatewayImplementationServicePortTypeClient())
                    {
                        ServiceReferenceZarinpalSandBox.PaymentRequestResponse resp = ZpalSr.PaymentRequestAsync(
                                _ZarinPalPaymentSettings.MerchantID,
                                total,
                                Description,
                                customer.Email,
                                PhoneOfUser,
                                CallbackURL
                            ).Result;

                        urlToRedirect = ZarinpalHelper.ProduceRedirectUrl(StoreAddress,
                                           resp.Body.Status,
                                           _ZarinPalPaymentSettings.UseSandbox,
                                           resp.Body.Authority,
                                           ZarinGate);
                    }
                else
                    using (ServiceReferenceZarinpal.PaymentGatewayImplementationServicePortTypeClient ZpalSr = new ServiceReferenceZarinpal.PaymentGatewayImplementationServicePortTypeClient())
                    {
                        ServiceReferenceZarinpal.PaymentRequestResponse resp = ZpalSr.PaymentRequestAsync(
                                _ZarinPalPaymentSettings.MerchantID,
                                total,
                                Description,
                                customer.Email,
                                PhoneOfUser,
                                CallbackURL
                            ).Result;

                        urlToRedirect = ZarinpalHelper.ProduceRedirectUrl(StoreAddress,
                                            resp.Body.Status,
                                            _ZarinPalPaymentSettings.UseSandbox,
                                            resp.Body.Authority,
                                            ZarinGate);
                    }
            }
            else if (_ZarinPalPaymentSettings.Method == EnumMethod.REST)
            {
                var _url = $"https://{(_ZarinPalPaymentSettings.UseSandbox ? "sandbox" : "www")}.zarinpal.com/pg/rest/WebGate/PaymentRequest.json";

                var _values = new Dictionary<string, string>
                {
                    { "MerchantID", _ZarinPalPaymentSettings.MerchantID }, //Change This To work, some thing like this : xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx
                    { "Amount", total.ToString() }, //Toman
                    { "CallbackURL", CallbackURL },
                    { "Mobile", PhoneOfUser },
                    { "Description", Description }
                };

                var _paymentRequestJsonValue = JsonConvert.SerializeObject(_values);
                var content = new StringContent(_paymentRequestJsonValue, Encoding.UTF8, "application/json");

                var _response = clientZarinPal.PostAsync(_url, content).Result;
                var _responseString = _response.Content.ReadAsStringAsync().Result;

                RestRequestModel _restRequestModel =
                             JsonConvert.DeserializeObject<RestRequestModel>(_responseString);

                urlToRedirect = ZarinpalHelper.ProduceRedirectUrl(StoreAddress,
                    _restRequestModel?.Status,
                    _ZarinPalPaymentSettings.UseSandbox,
                    _restRequestModel.Authority,
                    ZarinGate);
            }

            var uri = new Uri(urlToRedirect);
            _httpContextAccessor.HttpContext.Response.Redirect(uri.AbsoluteUri);
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            bool hide = false;
            var ZarinPalPaymentSettings = _settingService.LoadSetting<ZarinpalPaymentSettings>(_storeContext.ActiveStoreScopeConfiguration);
            hide = string.IsNullOrWhiteSpace(_ZarinPalPaymentSettings.MerchantID);
            if (_ZarinPalPaymentSettings.BlockOverseas)
                hide = hide || ZarinpalHelper.isOverseaseIp(_httpContextAccessor.HttpContext.Connection.RemoteIpAddress);
            return hide;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Paymentzarinpal/Configure";
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new ZarinpalPaymentSettings
            {
                UseSandbox = true,
                MerchantID = "99999999-9999-9999-9999-999999999999",
                BlockOverseas = false,
                RialToToman = true,
                Method = EnumMethod.REST,
                UseZarinGate = false,
                ZarinGateType = EnumZarinGate.ZarinGate
            };
            _settingService.SaveSetting(settings);

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Use", "Use ZarinGate");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Use", "استفاده از زرین گیت", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Type", "Select ZarinGate Type");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Type", "انتخاب نوع زرین گیت", languageCulture: "fa-IR");

            string ZarinGateLink = "https://www.zarinpal.com/blog/زرین-گیت،-درگاهی-اختصاصی-به-نام-وبسایت/";
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Instructions", $"Read About the <a href=\"{ZarinGateLink}\">Zarin Gate</a> Then Select the ZarinGateLink type from below :");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Instructions",
             string.Concat("لطفا اول شرایط استفاده از زرین گیت را در ", $"<a href=\"{ZarinGateLink}\"> در این قسمت </a>", "مطالعه نموده و سپس نوع آن را انتخاب نمایید")
            , languageCulture: "fa-IR");


            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.Method", "Communication Method");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.Method", "روش پرداخت", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.Method.REST", "REST(recommanded)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.Method.SOAP", "SOAP");

            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.UseSandbox", "Use Snadbox for testing payment GateWay without real paying.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.UseSandbox", "تست درگاه زرین پال بدون پرداخت هزینه", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.MerchantID", "GateWay Merchant ID");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.MerchantID", "کد پذیرنده", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Instructions",
            string.Concat("You can use Zarinpal.com GateWay as a payment gateway. Zarinpal is not a bank but it is an interface which customers can pay with.",
             "<br/>", "Please consider that if you leave MerchantId field empty the Zarinpal Gateway will be hidden and not choosable when checking out"));
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.ZarinPal.Instructions",
             string.Concat("شما می توانید از زرین پال به عنوان یک درگاه پرداخت استفاده نمایید، زرین پال یک بانک نیست بلکه یک واسط بانکی است که کاربران میتوانند از طریق آن مبلغ مورد نظر را پرداخت نمایند، باید آگاه باشید که درگاه زرین پال درصدی از پول پرداخت شده کاربران را به عنوان کارمزد دریافت میکند.",
            "<br/>", "توجه داشته باشید که اگر فیلد کد پذیرنده خالی باشد درگاه زرین پال در هنگام پرداخت مخفی می شود و قابل انتخاب نیست"), languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("plugins.payments.zarinpal.PaymentMethodDescription", "ZarinPal, The Bank Interface");
            _localizationService.AddOrUpdatePluginLocaleResource("plugins.payments.zarinpal.PaymentMethodDescription", "درگاه واسط زرین پال", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RedirectionTip", "You will be redirected to ZarinPal site to complete the order.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RedirectionTip", "هم اکنون به درگاه بانک زرین پال منتقل می شوید.", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.BlockOverseas", "Block oversease access (block non Iranians)");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.BlockOverseas", "قطع دسترسی برای آی پی های خارج از کشور", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RialToToman", "Convert Rial To Toman");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RialToToman", "تبدیل ریال به تومن", languageCulture: "fa-IR");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RialToToman.Instructions",
            string.Concat(
                "The default currency of zarinpal is Toman", "<br/>",
                "Therefore if your website uses Rial before paying it should be converted to Toman", "<br/>",
                "please consider that to convert Rial to Toman system divides total to 10, so the last digit will be removed", "<br/>",
                "To do the stuff check this option"
            ));
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RialToToman.Instructions",
            string.Concat(
                "واحد ارزی پیش فرض درگاه پرداخت زرین پال تومان می باشد.", "<br/>",
                "لذا در صورتی که وبسایت شما از واحد ارزی ریال استفاده می کند باید قبل از پرداخت مبلغ نهایی به تومان تبدیل گردد", "<br/>",
                "لطفا در نظر داشته باشید که جهت تبدیل ریال به تومان عدد تقسیم بر 10 شده و در واقع رقم آخر حذف می گردد", "<br/>",
                "در صورتی که مایل به تغییر از ریال به تومان هنگام پرداخت می باشید این گزینه را فعال نمایید"
            ), languageCulture: "fa-IR");
            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<ZarinpalPaymentSettings>();

            //locales

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Use");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Type");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.ZarinGate.Instructions");

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.Method");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.Method.SOAP");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.Method.REST");

            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.UseSandbox");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ZarinPal.Fields.MerchantID");
            _localizationService.DeletePluginLocaleResource("plugins.payments.zarinpal.PaymentMethodDescription");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.ZarinPal.Instructions");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RedirectionTip");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.BlockOverseas");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RialToToman");
            _localizationService.DeletePluginLocaleResource("Plugins.Payments.Zarinpal.Fields.RialToToman.Instructions");
            base.Uninstall();
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        public string GetPublicViewComponentName()
        {
            //return $"{_webHelper.GetStoreLocation()}Plugin/Payments.Zarinpal/Views/Configure";
            return "PaymentZarinpal";
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get { return RecurringPaymentType.NotSupported; }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get { return PaymentMethodType.Redirection; }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            //return description of this payment method to be display on "payment method" checkout step. good practice is to make it localizable
            //for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site to complete the payment"
            get { return _localizationService.GetResource("plugins.payments.zarinpal.PaymentMethodDescription"); }
        }

        #endregion
        public void ManageSiteMap(SiteMapNode rootNode)
        {
            var NopTopPluginsNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "NopTop");
            if (NopTopPluginsNode == null)
            {
                NopTopPluginsNode = new SiteMapNode()
                {
                    SystemName = "NopTop",
                    Title = "NopTop",
                    Visible = true,
                    IconClass = "fa-gear"
                };
                rootNode.ChildNodes.Add(NopTopPluginsNode);
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

            NopTopPluginsNode.ChildNodes.Add(menueLikeProduct);
        }
    }
}
