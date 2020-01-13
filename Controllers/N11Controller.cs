using System;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.N11.Models;
using Nop.Plugin.Misc.N11.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.N11.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    public class N11Controller : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly N11Settings _n11Settings;
        private readonly N11Manager _n11manager;

        public N11Controller(ISettingService settingService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            N11Settings n11Settings,
            N11Manager n11manager)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _n11Settings = n11Settings;
            _n11manager = n11manager;
        }

        [AuthorizeAdmin]
        public IActionResult Configure()
        {
            var model = new ConfigurationModel()
            {
                ApiKey = _n11Settings.ApiKey,
                SecretKey = _n11Settings.SecretKey,
                ShipmentTemplate = _n11Settings.ShipmentTemplate,
                ApprovalStatus = _n11Settings.ApprovalStatus,
                PreparingDay = _n11Settings.PreparingDay,
                CurrencyType = _n11Settings.CurrencyType,
                DefaultCategoryId = _n11Settings.DefaultCategoryId.ToString(),
                CategoryIdMapping = _n11Settings.CategoryIdMapping,
                DefaultBrandName = _n11Settings.DefaultBrandName,
                BrandIdMapping = _n11Settings.BrandIdMapping
            };

            return View(@"~/Plugins/Misc.N11/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [AdminAntiForgery]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            _n11Settings.ApiKey = model.ApiKey;
            _n11Settings.SecretKey = model.SecretKey;
            _n11Settings.ShipmentTemplate = model.ShipmentTemplate;
            _n11Settings.ApprovalStatus = model.ApprovalStatus;
            _n11Settings.PreparingDay = model.PreparingDay;
            _n11Settings.CurrencyType = model.CurrencyType;
            _n11Settings.DefaultCategoryId = long.Parse(model.DefaultCategoryId);
            _n11Settings.CategoryIdMapping = model.CategoryIdMapping;
            _n11Settings.DefaultBrandName = model.DefaultBrandName;
            _n11Settings.BrandIdMapping = model.BrandIdMapping;
            _settingService.SaveSetting(_n11Settings);

            _settingService.ClearCache();

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return View(@"~/Plugins/Misc.N11/Views/Configure.cshtml", model);
        }

        [HttpPost, ActionName("Configure")]
        [AuthorizeAdmin]
        [FormValueRequired("exportexcel-categories-all")]
        public virtual IActionResult ExportExcelAllCategories()
        {
            try
            {
                var bytes = _n11manager.ExportN11CategoriesToXlsx();
                return File(bytes, MimeTypes.TextXlsx, "n11categories.xlsx");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc);
                return Configure();
            }
        }

        [HttpPost, ActionName("Configure")]
        [AuthorizeAdmin]
        [FormValueRequired("exportexcel-categoriesattributes-all")]
        public virtual IActionResult ExportExcelAllCategoriesAttributes()
        {
            try
            {
                var bytes = _n11manager.ExportN11CategoriesAttributesToXlsx();
                return File(bytes, MimeTypes.TextXlsx, "n11categoriesattributes.xlsx");
            }
            catch (Exception exc)
            {
                _notificationService.ErrorNotification(exc);
                return Configure();
            }
        }
    }
}