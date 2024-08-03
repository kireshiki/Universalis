using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Universalis.Application.Common;
using Universalis.Application.Views.V1;
using Universalis.DataTransformations;
using Universalis.DbAccess.MarketBoard;
using Universalis.DbAccess.Queries.MarketBoard;
using Universalis.Entities.MarketBoard;
using Universalis.GameData;

namespace Universalis.Application.Controllers;

public class HistoryControllerBase : WorldDcRegionControllerBase
{
    protected readonly IHistoryDbAccess History;

    public HistoryControllerBase(IGameDataProvider gameData, IHistoryDbAccess historyDb) : base(gameData)
    {
        History = historyDb;
    }

    protected async Task<(bool, HistoryView)> GetHistoryView(
        WorldDcRegion worldDcRegion,
        int[] worldIds,
        int itemId,
        int entries,
        long statsWithin = 604800000,
        long entriesWithin = 604800,
        DateTimeOffset? entriesUntil = null,
        int minSalePrice = 0,
        int maxSalePrice = int.MaxValue,
        CancellationToken cancellationToken = default)
    {
        using var activity = Util.ActivitySource.StartActivity("HistoryControllerBase.View");
        var now = DateTimeOffset.UtcNow;

        // Fetch the data
        var query = new HistoryManyQuery
        {
            WorldIds = worldIds,
            ItemIds = new[] { itemId },
            Count = entries,
            From = entriesWithin < 0 ? null : (entriesUntil ?? now).AddSeconds(-entriesWithin),
            To = entriesUntil,
        };
        var enumerable = await History.RetrieveMany(query, cancellationToken);
        var data = CollectSales(enumerable);
        var resolved = data.Count > 0;
        var worlds = GameData.AvailableWorlds();

        var history = data
            .Where(o => worlds.ContainsKey(o.WorldId))
            .Aggregate(new HistoryView(), (agg, next) =>
            {
                // Handle undefined arrays
                next.Sales ??= new List<Sale>();

                agg.Sales = next.Sales
                    .Where(s => {
                        var timestamp = new DateTimeOffset(s.SaleTime);
                        return (query.From == null || query.From?.CompareTo(timestamp) <= 0) &&
                            (query.To == null || query.To?.CompareTo(timestamp) >= 0);
                    })
                    .Where(s => s.Quantity is > 0)
                    .Where(s => s.PricePerUnit >= minSalePrice && s.PricePerUnit <= maxSalePrice)
                    .Select(s => new MinimizedSaleView
                    {
                        Hq = s.Hq,
                        PricePerUnit = s.PricePerUnit,
                        Quantity = s.Quantity ??
                                   0, // This should never be 0 since we're filtering out null and zero quantities
                        BuyerName = s.BuyerName,
                        OnMannequin = s.OnMannequin,
                        TimestampUnixSeconds = new DateTimeOffset(s.SaleTime).ToUnixTimeSeconds(),
                        WorldId = !worldDcRegion.IsWorld ? next.WorldId : null,
                        WorldName = !worldDcRegion.IsWorld ? worlds[next.WorldId] : null,
                    })
                    .Concat(agg.Sales)
                    .ToList();
                agg.LastUploadTimeUnixMilliseconds = (long)Math.Max(next.LastUploadTimeUnixMilliseconds,
                    agg.LastUploadTimeUnixMilliseconds);

                return agg;
            });

        history.Sales = history.Sales.OrderByDescending(s => s.TimestampUnixSeconds).Take(entries).ToList();

        var nqSales = history.Sales.Where(s => !s.Hq).ToList();
        var hqSales = history.Sales.Where(s => s.Hq).ToList();

        var untilTimestampMs = (query.To ?? now).ToUnixTimeMilliseconds();
        return (resolved, new HistoryView
        {
            Sales = history.Sales.Take(entries).ToList(),
            ItemId = itemId,
            WorldId = worldDcRegion.IsWorld ? worldDcRegion.WorldId : null,
            WorldName = worldDcRegion.IsWorld ? worldDcRegion.WorldName : null,
            DcName = worldDcRegion.IsDc ? worldDcRegion.DcName : null,
            RegionName = worldDcRegion.IsRegion ? worldDcRegion.RegionName : null,
            LastUploadTimeUnixMilliseconds = history.LastUploadTimeUnixMilliseconds,
            StackSizeHistogram = new SortedDictionary<int, int>(Statistics.GetDistribution(history.Sales
                .Select(s => s.Quantity))),
            StackSizeHistogramNq = new SortedDictionary<int, int>(Statistics.GetDistribution(nqSales
                .Select(s => s.Quantity))),
            StackSizeHistogramHq = new SortedDictionary<int, int>(Statistics.GetDistribution(hqSales
                .Select(s => s.Quantity))),
            SaleVelocity = Statistics.VelocityPerDay(history.Sales
                .Select(s => (s.TimestampUnixSeconds * 1000, s.Quantity)), untilTimestampMs, statsWithin),
            SaleVelocityNq = Statistics.VelocityPerDay(nqSales
                .Select(s => (s.TimestampUnixSeconds * 1000, s.Quantity)), untilTimestampMs, statsWithin),
            SaleVelocityHq = Statistics.VelocityPerDay(hqSales
                .Select(s => (s.TimestampUnixSeconds * 1000, s.Quantity)), untilTimestampMs, statsWithin),
        });
    }

    private static List<History> CollectSales(IEnumerable<History> sales)
    {
        using var activity = Util.ActivitySource.StartActivity("HistoryControllerBase.CollectSales");
        var result = sales.ToList();
        activity?.AddTag("db.resultCount", result.Count);
        return result;
    }
}