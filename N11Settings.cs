using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.N11
{
    public class N11Settings : ISettings
    {
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public string ShipmentTemplate { get; set; }
        public string ApprovalStatus { get; set; }
        public string PreparingDay { get; set; }
        public string CurrencyType { get; set; }
        public long DefaultCategoryId { get; set; }
        public string CategoryIdMapping { get; set; }
        public string DefaultBrandName { get; set; }
        public string BrandIdMapping { get; set; }
        public int LastAddedProductId { get; set; }
    }
}
