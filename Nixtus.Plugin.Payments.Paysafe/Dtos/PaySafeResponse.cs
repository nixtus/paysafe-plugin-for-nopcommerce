using System;
using Newtonsoft.Json;

namespace Nixtus.Plugin.Payments.Paysafe.Dtos
{
    public class PaySafeResponse
    {
        [JsonProperty("transaction")]
        public Transaction Transaction { get; set; }

        [JsonProperty("errors")]
        public Errors Errors { get; set; }
    }

    public partial class Errors
    {
        
    }

    public partial class Transaction
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("payment_method")]
        public string PaymentMethod { get; set; }

        [JsonProperty("account_vault_id")]
        public object AccountVaultId { get; set; }

        [JsonProperty("recurring_id")]
        public object RecurringId { get; set; }

        [JsonProperty("first_six")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long FirstSix { get; set; }

        [JsonProperty("last_four")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long LastFour { get; set; }

        [JsonProperty("account_holder_name")]
        public object AccountHolderName { get; set; }

        [JsonProperty("transaction_amount")]
        public string TransactionAmount { get; set; }

        [JsonProperty("description")]
        public object Description { get; set; }

        [JsonProperty("transaction_code")]
        public object TransactionCode { get; set; }

        [JsonProperty("avs")]
        public object Avs { get; set; }

        [JsonProperty("batch")]
        [JsonConverter(typeof(ParseStringConverter))]
        public long Batch { get; set; }

        [JsonProperty("order_num")]
        public string OrderNum { get; set; }

        [JsonProperty("verbiage")]
        public string Verbiage { get; set; }

        [JsonProperty("transaction_settlement_status")]
        public object TransactionSettlementStatus { get; set; }

        [JsonProperty("effective_date")]
        public object EffectiveDate { get; set; }

        [JsonProperty("routing")]
        public object Routing { get; set; }

        [JsonProperty("return_date")]
        public object ReturnDate { get; set; }

        [JsonProperty("created_ts")]
        public long CreatedTs { get; set; }

        [JsonProperty("modified_ts")]
        public long ModifiedTs { get; set; }

        [JsonProperty("transaction_api_id")]
        public object TransactionApiId { get; set; }

        [JsonProperty("terms_agree")]
        public object TermsAgree { get; set; }

        [JsonProperty("notification_email_address")]
        public object NotificationEmailAddress { get; set; }

        [JsonProperty("notification_email_sent")]
        public bool NotificationEmailSent { get; set; }

        [JsonProperty("response_message")]
        public object ResponseMessage { get; set; }

        [JsonProperty("auth_amount")]
        public string AuthAmount { get; set; }

        [JsonProperty("auth_code")]
        public string AuthCode { get; set; }

        [JsonProperty("status_id")]
        public long StatusId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("location_id")]
        public string LocationId { get; set; }

        [JsonProperty("reason_code_id")]
        public long ReasonCodeId { get; set; }

        [JsonProperty("contact_id")]
        public object ContactId { get; set; }

        [JsonProperty("billing_zip")]
        public object BillingZip { get; set; }

        [JsonProperty("billing_street")]
        public object BillingStreet { get; set; }

        [JsonProperty("product_transaction_id")]
        public string ProductTransactionId { get; set; }

        [JsonProperty("tax")]
        public string Tax { get; set; }

        [JsonProperty("customer_ip")]
        public object CustomerIp { get; set; }

        [JsonProperty("customer_id")]
        public object CustomerId { get; set; }

        [JsonProperty("po_number")]
        public object PoNumber { get; set; }

        [JsonProperty("avs_enhanced")]
        public string AvsEnhanced { get; set; }

        [JsonProperty("cvv_response")]
        public string CvvResponse { get; set; }

        [JsonProperty("billing_phone")]
        public object BillingPhone { get; set; }

        [JsonProperty("billing_city")]
        public object BillingCity { get; set; }

        [JsonProperty("billing_state")]
        public object BillingState { get; set; }

        [JsonProperty("clerk_number")]
        public object ClerkNumber { get; set; }

        [JsonProperty("tip_amount")]
        public string TipAmount { get; set; }

        [JsonProperty("created_user_id")]
        public string CreatedUserId { get; set; }

        [JsonProperty("modified_user_id")]
        public string ModifiedUserId { get; set; }

        [JsonProperty("ach_identifier")]
        public object AchIdentifier { get; set; }

        [JsonProperty("check_number")]
        public object CheckNumber { get; set; }

        [JsonProperty("settle_date")]
        public object SettleDate { get; set; }

        [JsonProperty("charge_back_date")]
        public object ChargeBackDate { get; set; }

        [JsonProperty("void_date")]
        public object VoidDate { get; set; }

        [JsonProperty("account_type")]
        public string AccountType { get; set; }

        [JsonProperty("is_recurring")]
        public bool IsRecurring { get; set; }

        [JsonProperty("is_accountvault")]
        public bool IsAccountvault { get; set; }

        [JsonProperty("transaction_c1")]
        public object TransactionC1 { get; set; }

        [JsonProperty("transaction_c2")]
        public object TransactionC2 { get; set; }

        [JsonProperty("transaction_c3")]
        public object TransactionC3 { get; set; }

        [JsonProperty("additional_amounts")]
        public object[] AdditionalAmounts { get; set; }

        [JsonProperty("terminal_serial_number")]
        public object TerminalSerialNumber { get; set; }

        [JsonProperty("entry_mode_id")]
        public string EntryModeId { get; set; }

        [JsonProperty("terminal_id")]
        public object TerminalId { get; set; }

        [JsonProperty("quick_invoice_id")]
        public object QuickInvoiceId { get; set; }

        [JsonProperty("ach_sec_code")]
        public object AchSecCode { get; set; }

        [JsonProperty("custom_data")]
        public object CustomData { get; set; }

        [JsonProperty("hosted_payment_page_id")]
        public object HostedPaymentPageId { get; set; }

        [JsonProperty("trx_source_id")]
        public long TrxSourceId { get; set; }

        [JsonProperty("transaction_batch_id")]
        public string TransactionBatchId { get; set; }

        [JsonProperty("emv_receipt_data")]
        public object EmvReceiptData { get; set; }

        [JsonProperty("_links")]
        public Links Links { get; set; }
    }

    public partial class Links
    {
        [JsonProperty("self")]
        public Self Self { get; set; }
    }

    public partial class Self
    {
        [JsonProperty("href")]
        public Uri Href { get; set; }
    }

    internal class ParseStringConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(long) || t == typeof(long?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            long l;
            if (Int64.TryParse(value, out l))
            {
                return l;
            }
            throw new Exception("Cannot unmarshal type long");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (long)untypedValue;
            serializer.Serialize(writer, value.ToString());
            return;
        }

        public static readonly ParseStringConverter Singleton = new ParseStringConverter();
    }
}
