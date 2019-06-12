using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    internal static class IndustryProcessor
    {
        public static void ProcessAllIndustrySectors(IndustryAllSectors theEconomy, CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var orderedList = theEconomy.GetSectorsInOrderOfDescendingPriority();

            foreach (var sector in orderedList)
            {
                stockpile = sector.ProcessBatches(stockpile, tradeGoodsLibrary);
            }
        }
    }
}
