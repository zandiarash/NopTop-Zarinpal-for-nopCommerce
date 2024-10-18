using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace NopTop.Plugin.Payments.Zarinpal.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute("NopTop.Payments.Zarinpal.ResultHandler", "Plugins/PaymentZarinpal/ResultHandler",
                 new { controller = "PaymentZarinpal", action = "ResultHandler" });

            endpointRouteBuilder.MapControllerRoute("NopTop.Payments.Zarinpal.ErrorHandler", "Plugins/PaymentZarinpal/ErrorHandler",
                 new { controller = "PaymentZarinpal", action = "ErrorHandler" });
        }
        public int Priority => -1;

    }
}