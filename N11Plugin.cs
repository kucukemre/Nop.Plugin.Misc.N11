using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Tasks;

namespace Nop.Plugin.Misc.N11
{
    public class N11Plugin : BasePlugin, IMiscPlugin
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IScheduleTaskService _scheduleTaskService;

        #endregion

        #region Ctor

        public N11Plugin(ISettingService settingService, IWebHelper webHelper, ILocalizationService localizationService, IScheduleTaskService scheduleTaskService)
        {
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._localizationService = localizationService;
            this._scheduleTaskService = scheduleTaskService;
        }

        #endregion

        #region Methods

        public override void Install()
        {
            //settings
            var settings = new N11Settings
            {
                ApiKey = "",
                SecretKey = "",
                ApprovalStatus = "",
                CurrencyType = "",
                ShipmentTemplate = "",
                PreparingDay = "",
                CategoryIdMapping = "",
                DefaultCategoryId = 0,
                BrandIdMapping = "",
                DefaultBrandName = "",
                LastAddedProductId = 0

            };
            _settingService.SaveSetting(settings);

            //install synchronization task
            if (_scheduleTaskService.GetTaskByType("Nop.Plugin.Misc.N11.Services.SynchronizationTask") == null)
            {
                _scheduleTaskService.InsertTask(new ScheduleTask
                {
                    Enabled = true,
                    Seconds = 12 * 60 * 60,
                    Name = "Synchronization (N11 plugin)",
                    Type = "Nop.Plugin.Misc.N11.Services.SynchronizationTask",
                });
            }

            //locales
            #region locales
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.Title", "N11 Plugin");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApiKey", "N11 API Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApiKey.Hint", "Enter N11 API Key.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.SecretKey", "N11 Secret Key");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.SecretKey.Hint", "Enter N11 Secret Key.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ShipmentTemplate", "N11 Shipment Template");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ShipmentTemplate.Hint", "Enter N11 Shipment Template.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApprovalStatus", "N11 Approval Status");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApprovalStatus.Hint", "Enter N11 Approval Status.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.PreparingDay", "N11 Preparing Day");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.PreparingDay.Hint", "Enter N11 Preparing Day.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CurrencyType", "N11 Currency Type");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CurrencyType.Hint", "Enter N11 Currency Type.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultCategoryId", "N11 Default Category Id");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultCategoryId.Hint", "Enter N11 Default Category Id.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CategoryIdMapping", "N11 Category Id Mapping");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CategoryIdMapping.Hint", "Enter N11 Category Id Mapping. {nopcommerce-category-id}=>{n11-category-id};{nopcommerce-category-id}=>{n11-category-id} Example: 1=>1001298;2=>1001300");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultBrandName", "N11 Default Brand Name");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultBrandName.Hint", "Enter N11 Default Brand Name.");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.BrandIdMapping", "N11 Brand Id Mapping");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.BrandIdMapping.Hint", "Enter N11 Brand Id Mapping. {nopcommerce-brand-id}=>{n11-brand-name};{nopcommerce-brand-id}=>{n11-brand-name} Example: 1=>Diğer;2=>Acar");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.Categories.ExportToExcel.All", "Export All N11 Categories To Xlsx");
            _localizationService.AddOrUpdatePluginLocaleResource("Plugins.Misc.N11.Admin.CategoriesAttributes.ExportToExcel.All", "Export All N11 Categories Attributes To Xlsx");
            #endregion

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<N11Settings>();

            //schedule task
            var task = _scheduleTaskService.GetTaskByType("Nop.Plugin.Misc.N11.Services.SynchronizationTask");
            if (task != null)
                _scheduleTaskService.DeleteTask(task);

            //locales
            #region locales
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.Title");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApiKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApiKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.SecretKey");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.SecretKey.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ShipmentTemplate");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ShipmentTemplate.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApprovalStatus");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.ApprovalStatus.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.PreparingDay");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.PreparingDay.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CurrencyType");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CurrencyType.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultCategoryId");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultCategoryId.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CategoryIdMapping");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.CategoryIdMapping.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultBrandName");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.DefaultBrandName.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.BrandIdMapping");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Fields.BrandIdMapping.Hint");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.Categories.ExportToExcel.All");
            _localizationService.DeletePluginLocaleResource("Plugins.Misc.N11.Admin.CategoriesAttributes.ExportToExcel.All");
            #endregion

            base.Uninstall();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/N11/Configure";
        }

        #endregion
    }
}