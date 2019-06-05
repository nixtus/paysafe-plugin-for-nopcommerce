using System;
using Microsoft.AspNetCore.Mvc;
using Nixtus.Plugin.Payments.Paysafe.Models;
using Nop.Core;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nixtus.Plugin.Payments.Paysafe.Controllers
{
    public class PaymentPaysafeController : BasePaymentController
    {
        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IStoreService _storeService;
        private readonly IWorkContext _workContext;
        private readonly IPermissionService _permissionService;
        
        public PaymentPaysafeController(ILocalizationService localizationService,
            ISettingService settingService,
            IStoreService storeService,
            IWorkContext workContext,
            IPermissionService permissionService)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _storeService = storeService;
            _workContext = workContext;
            _permissionService = permissionService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var paySafePaymentSettings = _settingService.LoadSetting<PaySafePaymentSettings>(storeScope);

            var model = new ConfigurationModel
            {
                UseSandbox = paySafePaymentSettings.UseSandbox,
                TransactModeId = Convert.ToInt32(paySafePaymentSettings.TransactMode),
                LocationId = paySafePaymentSettings.LocationId,
                DeveloperId = paySafePaymentSettings.DeveloperId,
                UserId = paySafePaymentSettings.UserId,
                UserApiKey = paySafePaymentSettings.UserApiKey,
                AdditionalFee = paySafePaymentSettings.AdditionalFee,
                AdditionalFeePercentage = paySafePaymentSettings.AdditionalFeePercentage,
                TransactModeValues = paySafePaymentSettings.TransactMode.ToSelectList(),
                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.UseSandbox_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.UseSandbox, storeScope);
                model.TransactModeId_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.TransactMode, storeScope);
                model.LocationId_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.LocationId, storeScope);
                model.DeveloperId_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.DeveloperId, storeScope);
                model.AdditionalFee_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.AdditionalFee, storeScope);
                model.AdditionalFeePercentage_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.AdditionalFeePercentage, storeScope);
                model.UserId_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.UserId, storeScope);
                model.UserApiKey_OverrideForStore = _settingService.SettingExists(paySafePaymentSettings, x => x.UserApiKey, storeScope);
            }

            return View("~/Plugins/Payments.Paysafe/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var authorizeNetPaymentSettings = _settingService.LoadSetting<PaySafePaymentSettings>(storeScope);

            //save settings
            authorizeNetPaymentSettings.UseSandbox = model.UseSandbox;
            authorizeNetPaymentSettings.TransactMode = (TransactMode) model.TransactModeId;
            authorizeNetPaymentSettings.LocationId = model.LocationId;
            authorizeNetPaymentSettings.DeveloperId = model.DeveloperId;
            authorizeNetPaymentSettings.AdditionalFee = model.AdditionalFee;
            authorizeNetPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            authorizeNetPaymentSettings.UserId = model.UserId;
            authorizeNetPaymentSettings.UserApiKey = model.UserApiKey;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.UseSandbox, model.UseSandbox_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.TransactMode, model.TransactModeId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.LocationId, model.LocationId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.DeveloperId, model.DeveloperId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.AdditionalFee, model.AdditionalFee_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.AdditionalFeePercentage, model.AdditionalFeePercentage_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.UserId, model.UserId_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(authorizeNetPaymentSettings, x => x.UserApiKey, model.UserApiKey_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}