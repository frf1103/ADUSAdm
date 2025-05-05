public class RootObject
{
    public class AffiliationsItem
    {
        public string AffiliatesGroupName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactsGroupId { get; set; }
        public string ContactsGroupName { get; set; }
        public string ContactId { get; set; }
        public string Currency { get; set; }

        public string Id { get; set; }
        public string MarketplaceId { get; set; }
        public string Name { get; set; }
        public double NetValue { get; set; }
        public double Value { get; set; }
        public string RecipientId { get; set; }
        public string RecipientMarketplaceId { get; set; }
    }

    public List<AffiliationsItem> Affiliations { get; set; }
    public string CheckoutUrl { get; set; }

    public class Contact
    {
        public string id { get; set; }
        public string name { get; set; }
        public string CompanyName { get; set; }
        public string email { get; set; }
        public string doc { get; set; }
        public string phone_number { get; set; }
        public string phone_local_code { get; set; }
        public string address { get; set; }
        public string address_number { get; set; }
        public string address_comp { get; set; }
        public string address_district { get; set; }
        public string AddressCity { get; set; }
        public string AddressState { get; set; }
        public string AddressStateFullName { get; set; }
        public string AddressCountry { get; set; }
        public string address_zip_code { get; set; }
        public List<object> Lead { get; set; }
    }

    public Contact contact { get; set; }
    public List<object> Contracts { get; set; }

    public class Dates
    {
        public object CanceledAt { get; set; }
        public object ConfirmedAt { get; set; }
        public int created_at { get; set; }
        public string ExpiresAt { get; set; }
        public int OrderedAt { get; set; }
        public object UnavailableUntil { get; set; }
        public int UpdatedAt { get; set; }
        public object WarrantyUntil { get; set; }
    }

    public Dates dates { get; set; }
    public List<object> Ecommerces { get; set; }

    public class Extras
    {
        public int AcceptedTermsUrl { get; set; }
        public int AcceptedPrivacyPolicyUrl { get; set; }
    }

    public Extras extras { get; set; }
    public int HasOrderBump { get; set; }
    public string id { get; set; }

    public class Infrastructure
    {
        public string Ip { get; set; }
        public string City { get; set; }
        public string Host { get; set; }
        public string Region { get; set; }
        public string Country { get; set; }
        public string UserAgent { get; set; }
        public string CityLatLong { get; set; }
    }

    public Infrastructure infrastructure { get; set; }

    public class Invoice
    {
        public string ChargeAt { get; set; }
        public string created_at { get; set; }
        public int cycle { get; set; }
        public int DiscountValue { get; set; }
        public string id { get; set; }
        public int IncrementValue { get; set; }
        public string PeriodEnd { get; set; }
        public string PeriodStart { get; set; }
        public string Status { get; set; }
        public int TaxValue { get; set; }
        public int Tries { get; set; }
        public int Try { get; set; }
        public string Type { get; set; }
        public int Value { get; set; }
    }

    public Invoice? invoice { get; set; }
    public int IsOrderBump { get; set; }

    public int IsReissue { get; set; }

    public class ItemsItem
    {
        public string Id { get; set; }
        public string ImageUrl { get; set; }
        public string InternalId { get; set; }
        public string MarketplaceId { get; set; }
        public string marketplace_name { get; set; }
        public string Name { get; set; }

        public class Offer
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public Offer offer { get; set; }

        public class Producer
        {
            public object ContactEmail { get; set; }
            public string MarketplaceId { get; set; }
            public string Name { get; set; }
        }

        public Producer producer { get; set; }
        public int Qty { get; set; }
        public int TotalValue { get; set; }
        public string Type { get; set; }
        public int UnitValue { get; set; }
    }

    public List<ItemsItem> Items { get; set; }
    public List<object> LastTransaction { get; set; }

    public class Payment
    {
        public double AffiliateValue { get; set; }

        public class Acquirer
        {
            public string code { get; set; }
            public string message { get; set; }
            public string Name { get; set; }
            public string nsu { get; set; }
            public string Tid { get; set; }
        }

        public Acquirer acquirer { get; set; }
        public int CanTryAgain { get; set; }
        public object Coupon { get; set; }
        public string Currency { get; set; }
        public int DiscountValue { get; set; }
        public int Gross { get; set; }

        public class Installments
        {
            public int Value { get; set; }
            public int Qty { get; set; }
            public int Interest { get; set; }
        }

        public Installments installments { get; set; }
        public string marketplace_id { get; set; }
        public string marketplace_name { get; set; }
        public int MarketplaceValue { get; set; }
        public string method { get; set; }
        public double Net { get; set; }

        public class ProcessingTimes
        {
            public string StartedAt { get; set; }
            public string FinishedAt { get; set; }
            public int DelayInSeconds { get; set; }
        }

        public ProcessingTimes processingTimes { get; set; }
        public string RefundReason { get; set; }
        public string RefuseReason { get; set; }

        public class Tax
        {
            public int Value { get; set; }
            public int Rate { get; set; }
        }

        public Tax tax { get; set; }
        public int Total { get; set; }

        public class Pix
        {
            public class Qrcode
            {
                public string signature { get; set; }
                public string Url { get; set; }
            }

            public Qrcode qrcode { get; set; }
            public string ExpirationDate { get; set; }
        }

        public Pix pix { get; set; }
    }

    public Payment payment { get; set; }

    public class Product
    {
        public string Id { get; set; }
        public string ImageUrl { get; set; }
        public string InternalId { get; set; }
        public string MarketplaceId { get; set; }
        public string marketplace_name { get; set; }
        public string Name { get; set; }

        public class Offer
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        public Offer offer { get; set; }

        public class Producer
        {
            public string MarketplaceId { get; set; }
            public string Name { get; set; }
            public string ContactEmail { get; set; }
        }

        public Producer producer { get; set; }
        public int qty { get; set; }
        public double total_value { get; set; }
        public string Type { get; set; }
        public double unit_value { get; set; }
    }

    public Product product { get; set; }

    public class Shipment
    {
        public string Carrier { get; set; }
        public string Service { get; set; }
        public string Tracking { get; set; }
        public int Value { get; set; }
        public string Status { get; set; }
        public string DeliveryTime { get; set; }
    }

    public Shipment shipment { get; set; }

    public class Shipping
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public Shipping shipping { get; set; }
    public string status { get; set; }

    public class Subscription
    {
        public int CanCancel { get; set; }
        public object CanceledAt { get; set; }
        public int ChargedEveryDays { get; set; }
        public int ChargedTimes { get; set; }
        public string id { get; set; }
        public string internal_id { get; set; }
        public string LastStatus { get; set; }
        public int LastStatusAt { get; set; }
        public string Name { get; set; }
        public int StartedAt { get; set; }
        public string SubscriptionCode { get; set; }
        public int TrialDays { get; set; }
        public object TrialFinishedAt { get; set; }
        public object TrialStartedAt { get; set; }
        public int started_at { get; set; }
    }

    public Subscription subscription { get; set; }

    public class Trackings
    {
        public object Source { get; set; }
        public object CheckoutSource { get; set; }
        public object UtmSource { get; set; }
        public object UtmCampaign { get; set; }
        public object UtmMedium { get; set; }
        public object UtmContent { get; set; }
        public object UtmTerm { get; set; }
        public List<object> Pptc { get; set; }
    }

    public Trackings trackings { get; set; }
    public string Type { get; set; }
}

public class Transactions
{
    public List<RootObject> Data { get; set; }
    public int has_more_pages { get; set; }
    public string next_cursor { get; set; }
    public int total_rows { get; set; }
}