public class SubscriptionRoot
{
    public List<Subscription> Data { get; set; }
    public int has_more_pages { get; set; }
    public string next_cursor { get; set; }
}

public class Subscription
{
    public string Id { get; set; }
    public string SubscriptionCode { get; set; }
    public string Payment_Method { get; set; }
    public int ChargedTimes { get; set; }
    public bool CancelAtCycleEnd { get; set; }
    public long Created_At { get; set; }
    public string CycleStartDate { get; set; }
    public string CycleEndDate { get; set; }
    public string LastStatus { get; set; }
    public long LastStatusAt { get; set; }
    public long StartedAt { get; set; }
    public long UpdatedAt { get; set; }

    public Contact Contact { get; set; }
    public Product Product { get; set; }
    public ContractContainer Contracts { get; set; }
}

public class Contact
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Doc { get; set; }
    public string Email { get; set; }
    public string Phone_Local_Code { get; set; }
    public string Phone_Number { get; set; }
}

public class Product
{
    public string Id { get; set; }
    public string MarketplaceId { get; set; }
    public string Marketplace_Name { get; set; }
    public string Name { get; set; }
}

public class ContractContainer
{
    public DocusignContract Docusign { get; set; }
}

public class DocusignContract
{
    public string Status { get; set; }
    public string SignedAt { get; set; }
    public string EnvelopeId { get; set; }
}