using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Nixtus.Plugin.Payments.Paysafe.Dtos;
using Nixtus.Plugin.Payments.Paysafe.Models;
using Nixtus.Plugin.Payments.Paysafe.Validators;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace Nixtus.Plugin.Payments.Paysafe
{
    /// <summary>
    /// AuthorizeNet payment processor
    /// </summary>
    public class PaysafePaymentProcessor : BasePlugin, IPaymentMethod
    {
        private HttpClient _httpClient = new HttpClient();

        #region Fields

        private readonly ISettingService _settingService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IWebHelper _webHelper;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ILogger _logger;
        private readonly CurrencySettings _currencySettings;
        private readonly PaySafePaymentSettings _paySafePaymentSettings;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public PaysafePaymentProcessor(ISettingService settingService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IWebHelper webHelper,
            IOrderTotalCalculationService orderTotalCalculationService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            ILogger logger,
            CurrencySettings currencySettings,
            PaySafePaymentSettings paySafePaymentSettings,
            ILocalizationService localizationService)
        {
            this._paySafePaymentSettings = paySafePaymentSettings;
            this._settingService = settingService;
            this._currencyService = currencyService;
            this._customerService = customerService;
            this._currencySettings = currencySettings;
            this._webHelper = webHelper;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._logger = logger;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._localizationService = localizationService;
        }

        #endregion

        #region Utilities
        private string GetUrl()
        {
            return _paySafePaymentSettings.UseSandbox
                ? "https://api.sandbox.expinet.net/v2/transactions"
                : "https://api.expinet.net/v2/transactions";
        } 
        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            var postValues = new Dictionary<string, string>
            {
                { "location_id", _paySafePaymentSettings.LocationId },
                { "payment_method", "cc" },
                { "action", _paySafePaymentSettings.TransactMode == TransactMode.AuthorizeAndCapture ? "sale" : "authonly"},
                { "account_number", processPaymentRequest.CreditCardNumber },
                { "exp_date", $"{processPaymentRequest.CreditCardExpireMonth}{processPaymentRequest.CreditCardExpireYear.ToString().Substring(2, 2)}" },
                { "ccv", processPaymentRequest.CreditCardCvv2 },
                { "transaction_amount", processPaymentRequest.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture)},
                { "billing_street", customer.BillingAddress.Address1 },
                { "billing_city", customer.BillingAddress.City },
                { "billing_phone", customer.BillingAddress.PhoneNumber },
                { "billing_state", customer.BillingAddress.StateProvince.Abbreviation },
                { "billing_zip", customer.BillingAddress.ZipPostalCode }
            };

            var json = JsonConvert.SerializeObject(new { transaction = postValues });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                _httpClient.DefaultRequestHeaders.Add("developer-id", _paySafePaymentSettings.DeveloperId);
                _httpClient.DefaultRequestHeaders.Add("user-id", _paySafePaymentSettings.UserId);
                _httpClient.DefaultRequestHeaders.Add("user-api-key", _paySafePaymentSettings.UserApiKey);
                var response = _httpClient.PostAsync(GetUrl(), data).Result;
                var paySafeResponse = JsonConvert.DeserializeObject<PaySafeResponse>(response.Content.ReadAsStringAsync().Result);
                var transactionResult = paySafeResponse.Transaction;

                if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(transactionResult.Id))
                {
                    result.AuthorizationTransactionCode = $"{transactionResult.Id},{transactionResult.AuthCode}";
                    result.AuthorizationTransactionResult = $"Approved.  Status ID: {transactionResult.StatusId}, Reason Code: {transactionResult.ReasonCodeId}";

                    result.AvsResult = transactionResult.AvsEnhanced;
                    result.Cvv2Result = transactionResult.CvvResponse;

                    result.NewPaymentStatus = _paySafePaymentSettings.TransactMode == TransactMode.AuthorizeAndCapture
                        ? PaymentStatus.Paid
                        : PaymentStatus.Authorized;
                }
            }
            catch (Exception exception)
            {
                _logger.Error("PaySafe Error", exception, customer);
                result.AddError("Exception Occurred: " + exception.Message);
                return result;
            }

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _paySafePaymentSettings.AdditionalFee, _paySafePaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();

            var codes = capturePaymentRequest.Order.AuthorizationTransactionCode.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            _httpClient.DefaultRequestHeaders.Add("developer-id", _paySafePaymentSettings.DeveloperId);
            _httpClient.DefaultRequestHeaders.Add("user-id", _paySafePaymentSettings.UserId);
            _httpClient.DefaultRequestHeaders.Add("user-api-key", _paySafePaymentSettings.UserApiKey);

            var json = JsonConvert.SerializeObject(new { transaction = new Dictionary<string, string> { { "action", "authcomplete" } } });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = _httpClient.PutAsync($"{GetUrl()}/{codes[0]}", data).Result;
                var paySafeResponse = JsonConvert.DeserializeObject<PaySafeResponse>(response.Content.ReadAsStringAsync().Result);
                var transactionResult = paySafeResponse.Transaction;

                if (response.IsSuccessStatusCode)
                {
                    result.CaptureTransactionId = $"{transactionResult.Id},{transactionResult.AuthCode}";

                    result.NewPaymentStatus = PaymentStatus.Paid;
                }
            }
            catch (Exception exception)
            {
                _logger.Error("PaySafe Error", exception);
                result.AddError("Exception Occurred: " + exception.Message);
                return result;
            }

            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();

            var codes = refundPaymentRequest.Order.CaptureTransactionId == null
                ? refundPaymentRequest.Order.AuthorizationTransactionCode.Split(',')
                : refundPaymentRequest.Order.CaptureTransactionId.Split(',');

            _httpClient.DefaultRequestHeaders.Add("developer-id", _paySafePaymentSettings.DeveloperId);
            _httpClient.DefaultRequestHeaders.Add("user-id", _paySafePaymentSettings.UserId);
            _httpClient.DefaultRequestHeaders.Add("user-api-key", _paySafePaymentSettings.UserApiKey);

            var json = JsonConvert.SerializeObject(new
            {
                transaction = new Dictionary<string, string>
                {
                    { "action", "refund" },
                    { "payment_method", "cc" },
                    { "previous_transaction_id", codes[0]},
                    { "transaction_amount", refundPaymentRequest.AmountToRefund.ToString("0.00", CultureInfo.InvariantCulture)},
                    { "location_id", _paySafePaymentSettings.LocationId }
                }
            });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = _httpClient.PostAsync(GetUrl(), data).Result;
                var paySafeResponse = JsonConvert.DeserializeObject<PaySafeResponse>(response.Content.ReadAsStringAsync().Result);
                var transactionResult = paySafeResponse.Transaction;

                if (response.IsSuccessStatusCode)
                {
                    var refundedTotalAmount = refundPaymentRequest.AmountToRefund + refundPaymentRequest.Order.RefundedAmount;

                    var isOrderFullyRefunded = refundedTotalAmount == refundPaymentRequest.Order.OrderTotal;

                    result.NewPaymentStatus = isOrderFullyRefunded ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
                }
            }
            catch (Exception exception)
            {
                _logger.Error("PaySafe Error", exception);
                result.AddError("Exception Occurred: " + exception.Message);
                return result;
            }

            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();

            var codes = voidPaymentRequest.Order.CaptureTransactionId == null
                ? voidPaymentRequest.Order.AuthorizationTransactionCode.Split(',')
                : voidPaymentRequest.Order.CaptureTransactionId.Split(',');

            _httpClient.DefaultRequestHeaders.Add("developer-id", _paySafePaymentSettings.DeveloperId);
            _httpClient.DefaultRequestHeaders.Add("user-id", _paySafePaymentSettings.UserId);
            _httpClient.DefaultRequestHeaders.Add("user-api-key", _paySafePaymentSettings.UserApiKey);

            var json = JsonConvert.SerializeObject(new { transaction = new Dictionary<string, string> { { "action", "void" } } });
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = _httpClient.PutAsync($"{GetUrl()}/{codes[0]}", data).Result;
                var paySafeResponse = JsonConvert.DeserializeObject<PaySafeResponse>(response.Content.ReadAsStringAsync().Result);
                var transactionResult = paySafeResponse.Transaction;

                if (response.IsSuccessStatusCode)
                {
                    result.NewPaymentStatus = PaymentStatus.Voided;
                }
            }
            catch (Exception exception)
            {
                _logger.Error("PaySafe Error", exception);
                result.AddError("Exception Occurred: " + exception.Message);
                return result;
            }

            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);


            return result;
        }



        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();


            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return false;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };

            var validationResult = validator.Validate(model);

            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
            {
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };

            return paymentInfo;
        }

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = "Paysafe";
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentPaySafe/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            //settings
            var settings = new PaySafePaymentSettings
            {
                UseSandbox = true,
                TransactMode = TransactMode.AuthorizeAndCapture,
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UseSandbox.Hint", "Check to enable Sandbox (testing environment).");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.TransactModeValues", "Transaction mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.TransactModeValues.Hint", "Choose transaction mode.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.LocationId", "Location ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.LocationId.Hint", "Location ID found in your developer account project");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.DeveloperId", "Developer ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.DeveloperId.Hint", "Developer ID found in your developer account > project > project details");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserId", "User ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserId.Hint", "User ID found in your developer account > project > API credentials");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserApiKey", "User API Key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserApiKey.Hint", "User API Key found in your developer account > project > API credentials");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.PaySafe.PaymentMethodDescription", "Pay by credit / debit card");

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<PaySafePaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.TransactModeValues");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.TransactModeValues.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.LocationId");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.LocationId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.DeveloperId");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.DeveloperId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.AdditionalFeePercentage.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserId");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserApiKey");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.Fields.UserApiKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.PaySafe.PaymentMethodDescription");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => true;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => true;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => true;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Automatic;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Standard;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription => _localizationService.GetResource("Plugins.Payments.PaySafe.PaymentMethodDescription");

        #endregion
    }
}
