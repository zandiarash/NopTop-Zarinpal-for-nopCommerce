using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace NopTop.Plugin.Payments.Zarinpal.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Zarinpal.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }
        
        [NopResourceDisplayName("Plugins.Payments.Zarinpal.Fields.MerchantID")]
        public string MerchantID { get; set; }
        public bool MerchantID_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Zarinpal.Fields.BlockOverseas")]
        public bool BlockOverseas  { get; set; }
        public bool BlockOverseas_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Zarinpal.Fields.RialToToman")]
        public bool RialToToman  { get; set; }
        public bool RialToToman_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Zarinpal.Fields.Method")]
        public EnumMethod Method  { get; set; }
        public bool Method_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Zarinpal.Fields.ZarinGate.Use")]
        public bool UseZarinGate  { get; set; }
        public bool UseZarinGate_OverrideForStore { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Zarinpal.Fields.ZarinGate.Type")]
        public EnumZarinGate ZarinGateType  { get; set; }
        public bool ZarinGateType_OverrideForStore { get; set; }
    }
}