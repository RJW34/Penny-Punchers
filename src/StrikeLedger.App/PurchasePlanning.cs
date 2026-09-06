using StrikeLedger.Core;

namespace StrikeLedger.App;

public sealed record PurchasePreview(bool Valid,string Error,int TotalCost,int Remaining,string[] ItemIds,
    string[] ExMoveIds,string SuperArtId,int SuperUses);

/// <summary>Mixed shop cart preview. Saved bank never authorizes a combat action.</summary>
public static class PurchasePlanning
{
    public static PurchasePreview Preview(GameContent content,string fighterId,int bank,IEnumerable<string> itemIds)
    {
        var ids=itemIds?.ToArray()??[];
        var quote=content.QuotePreparation(fighterId,bank,new PreparationPlan(ids));
        return new(quote.Valid,quote.Error,quote.TotalCost,quote.Remaining,ids.Order(StringComparer.Ordinal).ToArray(),
            quote.ExMoveIds,quote.SelectedSuper,quote.SuperCount);
    }
    public static PreparationPlan QuotedPlan(GameContent content,string fighterId,int bank,IEnumerable<string> itemIds)
    {
        var quote=Preview(content,fighterId,bank,itemIds);if(!quote.Valid)throw new InvalidDataException(quote.Error);
        return new(quote.ItemIds){ContentHash=content.ContentHash,QuotedCost=quote.TotalCost};
    }
}
