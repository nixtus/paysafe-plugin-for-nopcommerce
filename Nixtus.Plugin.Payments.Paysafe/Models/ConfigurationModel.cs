using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Mvc.Models;

namespace Nixtus.Plugin.Payments.Paysafe.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        public bool UseSandbox_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.TransactModeValues")]
        public int TransactModeId { get; set; }
        public bool TransactModeId_OverrideForStore { get; set; }
        public SelectList TransactModeValues { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.LocationId")]
        public string LocationId { get; set; }
        public bool LocationId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.DeveloperId")]
        public string DeveloperId { get; set; }
        public bool DeveloperId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        public bool AdditionalFee_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        public bool AdditionalFeePercentage_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.UserId")]
        public string UserId { get; set; }
        public bool UserId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.PaySafe.Fields.UserApiKey")]
        public string UserApiKey { get; set; }
        public bool UserApiKey_OverrideForStore { get; set; }
    }
}