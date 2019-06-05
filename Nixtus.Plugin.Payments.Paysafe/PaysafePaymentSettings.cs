using Nop.Core.Configuration;

namespace Nixtus.Plugin.Payments.Paysafe
{
    public class PaySafePaymentSettings : ISettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to use the sandbox
        /// </summary>
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Gets or sets a values indicating what transaction mode to us
        /// </summary>
        public TransactMode TransactMode { get; set; }

        /// <summary>
        /// Gets or sets a values indicating the location ID
        /// </summary>
        public string LocationId { get; set; }

        /// <summary>
        /// Gets or sets a values indicating developer ID
        /// </summary>
        public string DeveloperId { get; set; }

        /// <summary>
        /// Gets or sets a values indicating user ID
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a values indicating user API key
        /// </summary>
        public string UserApiKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }

        /// <summary>
        /// Gets or sets a values indicating an additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
    }
}
