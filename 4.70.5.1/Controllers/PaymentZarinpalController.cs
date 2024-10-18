using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;
using System.Text;
using Nop.Services.Messages;
using NopTop.Plugin.Payments.Zarinpal;
using NopTop.Plugin.Payments.Zarinpal.Models;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework;
using Nop.Services.Security;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Controllers;
public class PaymentZarinPalController : BasePaymentController
{
    #region Fields
    private readonly IPaymentService _paymentService;
    private readonly IGenericAttributeService _genericAttributeService;
    private readonly IOrderProcessingService _orderProcessingService;
    private readonly IOrderService _orderService;
    private readonly IPaymentPluginManager _paymentPluginManager;
    private readonly IPermissionService _permissionService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger _logger;
    private readonly INotificationService _notificationService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IWebHelper _webHelper;
    private readonly IWorkContext _workContext;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    protected readonly ILanguageService _languageService;
    private readonly ZarinpalPaymentSettings _zarinPalPaymentSettings;

    #endregion

    #region Ctor
    public PaymentZarinPalController(IGenericAttributeService genericAttributeService,
        IOrderProcessingService orderProcessingService,
        IOrderService orderService,
        IPaymentService paymentService,
        IPaymentPluginManager paymentPluginManager,
        IPermissionService permissionService,
        ILocalizationService localizationService,
        ILogger logger,
        INotificationService notificationService,
        ISettingService settingService,
        IStoreContext storeContext,
        IWebHelper webHelper,
        IWorkContext workContext,
        ShoppingCartSettings shoppingCartSettings,
        ILanguageService languageService,
        ZarinpalPaymentSettings zarinPalPaymentSettings
        )
    {
        _genericAttributeService = genericAttributeService;
        _paymentService = paymentService;
        _orderProcessingService = orderProcessingService;
        _orderService = orderService;
        _paymentPluginManager = paymentPluginManager;
        _permissionService = permissionService;
        _localizationService = localizationService;
        _logger = logger;
        _notificationService = notificationService;
        _settingService = settingService;
        _storeContext = storeContext;
        _webHelper = webHelper;
        _workContext = workContext;
        _shoppingCartSettings = shoppingCartSettings;
        _zarinPalPaymentSettings = zarinPalPaymentSettings;
        _languageService = languageService;
    }

    #endregion

    #region Methods

    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    public async Task<IActionResult> Configure()
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var zarinPalPaymentSettings = await _settingService.LoadSettingAsync<ZarinpalPaymentSettings>(storeScope);

        var model = new ConfigurationModel
        {
            UseSandbox = zarinPalPaymentSettings.UseSandbox,
            MerchantID = zarinPalPaymentSettings.MerchantID,
            BlockOverseas = zarinPalPaymentSettings.BlockOverseas,
            RialToToman = zarinPalPaymentSettings.RialToToman,
            Method = zarinPalPaymentSettings.Method,
            UseZarinGate = zarinPalPaymentSettings.UseZarinGate,
            ZarinGateType = zarinPalPaymentSettings.ZarinGateType
        };

        if (storeScope <= 0)
            return View("~/Plugins/NopTop.Payments.ZarinPal/Views/Configure.cshtml", model);

        model.UseSandbox_OverrideForStore = await _settingService.SettingExistsAsync(zarinPalPaymentSettings, x => x.UseSandbox, storeScope);
        model.MerchantID_OverrideForStore = await _settingService.SettingExistsAsync(zarinPalPaymentSettings, x => x.MerchantID, storeScope);
        model.BlockOverseas_OverrideForStore = await _settingService.SettingExistsAsync(zarinPalPaymentSettings, x => x.BlockOverseas, storeScope);
        model.RialToToman_OverrideForStore = await _settingService.SettingExistsAsync(zarinPalPaymentSettings, x => x.RialToToman, storeScope);
        model.Method_OverrideForStore = await _settingService.SettingExistsAsync(zarinPalPaymentSettings, x => x.Method, storeScope);
        model.UseZarinGate_OverrideForStore = await _settingService.SettingExistsAsync(zarinPalPaymentSettings, x => x.UseZarinGate, storeScope);
        model.ZarinGateType_OverrideForStore = await _settingService.SettingExistsAsync(zarinPalPaymentSettings, x => x.ZarinGateType, storeScope);

        return View("~/Plugins/NopTop.Payments.ZarinPal/Views/Configure.cshtml", model);
    }


    [HttpPost]
    [AuthorizeAdmin]
    [Area(AreaNames.ADMIN)]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
            return AccessDeniedView();

        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var zarinPalPaymentSettings = await _settingService.LoadSettingAsync<ZarinpalPaymentSettings>(storeScope);

        //save settings
        zarinPalPaymentSettings.UseSandbox = model.UseSandbox;
        zarinPalPaymentSettings.MerchantID = model.MerchantID;
        zarinPalPaymentSettings.BlockOverseas = model.BlockOverseas;
        zarinPalPaymentSettings.RialToToman = model.RialToToman;
        zarinPalPaymentSettings.Method = model.Method;
        zarinPalPaymentSettings.UseZarinGate = model.UseZarinGate;
        zarinPalPaymentSettings.ZarinGateType = model.ZarinGateType;

        /* We do not clear cache after each setting update.
         * This behavior can increase performance because cached settings will not be cleared 
         * and loaded from database after each update */
        await _settingService.SaveSettingOverridablePerStoreAsync(zarinPalPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(zarinPalPaymentSettings, x => x.MerchantID, model.MerchantID_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(zarinPalPaymentSettings, x => x.BlockOverseas, model.MerchantID_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(zarinPalPaymentSettings, x => x.RialToToman, model.RialToToman_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(zarinPalPaymentSettings, x => x.Method, model.Method_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(zarinPalPaymentSettings, x => x.UseZarinGate, model.UseZarinGate_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(zarinPalPaymentSettings, x => x.ZarinGateType, model.ZarinGateType_OverrideForStore, storeScope, false);


        //now clear settings cache
        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

        return await Configure();
    }

    #endregion

    public async Task<IActionResult> ResultHandler(string status, string authority, string oGUID)
    {
        if (await _paymentPluginManager.LoadPluginBySystemNameAsync("NopTop.Payments.Zarinpal") is not ZarinPalPaymentProcessor processor || !_paymentPluginManager.IsPluginActive(processor))
            throw new NopException("ZarinPal module cannot be loaded");

        var language = _languageService.GetTwoLetterIsoLanguageName(await _workContext.GetWorkingLanguageAsync());
        Guid orderNumberGuid = Guid.Empty;
        try
        {
            orderNumberGuid = new Guid(oGUID);
        }
        catch { }

        var order = await _orderService.GetOrderByGuidAsync(orderNumberGuid);

        var total = Convert.ToInt32(Math.Round(order.OrderTotal, 2));
        if (_zarinPalPaymentSettings.RialToToman)
            total = total / 10;

        if (string.IsNullOrEmpty(status) == false && string.IsNullOrEmpty(authority) == false)
        {
            string refId = "0";
            System.Net.ServicePointManager.Expect100Continue = false;
            int statusCode = -1;
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var zarinPalSettings = await _settingService.LoadSettingAsync<ZarinpalPaymentSettings>(storeScope);

            var url = $"https://{(_zarinPalPaymentSettings.UseSandbox ? "sandbox" : "payment")}.zarinpal.com/pg/v4/payment/verify.json";
            var values = new
            {
                merchant_id = zarinPalSettings.MerchantID,
                amount = total.ToString(),  //Toman
                authority = authority
            };

            var content = new StringContent(JsonConvert.SerializeObject(values), Encoding.UTF8, "application/json");
            var response = ZarinPalPaymentProcessor.ClientZarinPal.PostAsync(url, content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            if (status == "OK")
            {
                var restVerifyModelOK = JsonConvert.DeserializeObject<RestVerifyModel<List<string>>>(responseString);
                statusCode = restVerifyModelOK.data.code;
                refId = restVerifyModelOK.data.ref_id.ToString();
            }
            else if (status == "NOK")
            {
                var restVerifyModelNOK = JsonConvert.DeserializeObject<RestVerifyModel<ErrorDetails>>(responseString);
                statusCode = restVerifyModelNOK.errors.code;
            }

            var resultMessage = ZarinpalErrorHelper.StatusToResult(statusCode, language);
            var orderNote = new OrderNote()
            {
                OrderId = order.Id,
                Note = string.Concat("وضعیت : پرداخت ", resultMessage.IsOk ? "" : "نا", "موفق", " - ", "پیام زرین پال : ", resultMessage.Message,
                  resultMessage.IsOk ?
                    string.Concat(" - کد پی گیری : ", refId) :
                    string.Concat(" - محل خطا : ", language == "fa" ? resultMessage.ErrorType.GetDescription() : resultMessage.ErrorType)
                ),
                DisplayToCustomer = true,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _orderService.InsertOrderNoteAsync(orderNote);

            if (resultMessage.IsOk && _orderProcessingService.CanMarkOrderAsPaid(order))
            {
                order.AuthorizationTransactionId = refId;
                await _orderService.UpdateOrderAsync(order);
                await _orderProcessingService.MarkOrderAsPaidAsync(order);
                return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            }
        }
        return RedirectToRoute("orderdetails", new { orderId = order.Id });
    }
    public ActionResult ErrorHandler(string error)
    {
        var language = _languageService.GetTwoLetterIsoLanguageName(_workContext.GetWorkingLanguageAsync().Result);
        int code;
        int.TryParse(error, out code);
        if (code != 0)
            error = ZarinpalErrorHelper.StatusToResult(code, language).Message;
        ViewBag.Err = string.Concat("خطا : ", error);
        return View("~/Plugins/NopTop.Payments.ZarinPal/Views/ErrorHandler.cshtml");
    }
}