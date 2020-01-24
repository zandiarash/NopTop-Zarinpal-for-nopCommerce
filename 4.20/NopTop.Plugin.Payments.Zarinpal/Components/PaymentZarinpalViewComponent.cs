using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;

namespace NopTop.Plugin.Payments.Zarinpal.Components
{
    [ViewComponent(Name = "PaymentZarinpal")]
    public class PaymentZarinpalViewComponent : NopViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/NopTop.Payments.Zarinpal/Views/PaymentInfo.cshtml");
        }
    }
}
