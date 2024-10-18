using Nop.Core.Configuration;
using NopTop.Plugin.Payments.Zarinpal.Models;

namespace NopTop.Plugin.Payments.Zarinpal
{
    public class ZarinpalPaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox (testing environment)
        /// </summary>
        public bool UseSandbox { get; set; }
        public string MerchantID { get; set; }
        /// <summary>
        /// Hide Zarinpal for overseases
        /// </summary>
        public bool BlockOverseas { get; set; }
        /// <summary>
        /// changes Rial to toman (if you use toman do not check)
        /// </summary>
        public bool RialToToman { get; set; }
        public EnumMethod Method { get; set; }
        public bool UseZarinGate  { get; set; }
        public EnumZarinGate ZarinGateType  { get; set; }
    }
}
