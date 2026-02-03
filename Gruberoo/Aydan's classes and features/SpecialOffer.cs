public class SpecialOffer
{
    public string OfferCode { get; }
    public string Description { get; }
    public double? DiscountAmount { get; } // null if "-"

    public SpecialOffer(string offerCode, string description, double? discountAmount)
    {
        OfferCode = offerCode ?? "";
        Description = description ?? "";
        DiscountAmount = discountAmount;
    }

    public override string ToString()
    {
        string disc = DiscountAmount.HasValue ? $"{DiscountAmount.Value:0.##}" : "-";
        return $"{OfferCode} - {Description} (Discount: {disc})";
    }
}
