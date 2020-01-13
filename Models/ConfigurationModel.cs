using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.N11.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.ApiKey")]
        public string ApiKey { get; set; }
        public bool ApiKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.SecretKey")]
        public string SecretKey { get; set; }
        public bool SecretKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.ShipmentTemplate")]
        public string ShipmentTemplate { get; set; }
        public bool ShipmentTemplate_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.ApprovalStatus")]
        public string ApprovalStatus { get; set; }
        public bool ApprovalStatus_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.PreparingDay")]
        public string PreparingDay { get; set; }
        public bool PreparingDay_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.CurrencyType")]
        public string CurrencyType { get; set; }
        public bool CurrencyType_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.DefaultCategoryId")]
        public string DefaultCategoryId { get; set; }
        public bool DefaultCategoryId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.CategoryIdMapping")]
        public string CategoryIdMapping { get; set; }
        public bool CategoryIdMapping_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.DefaultBrandName")]
        public string DefaultBrandName { get; set; }
        public bool DefaultBrandName_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Misc.N11.Admin.Fields.BrandIdMapping")]
        public string BrandIdMapping { get; set; }
        public bool BrandIdMapping_OverrideForStore { get; set; }
    }
}