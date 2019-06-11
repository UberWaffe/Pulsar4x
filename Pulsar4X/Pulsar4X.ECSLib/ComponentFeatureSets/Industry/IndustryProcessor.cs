using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    internal static class IndustryProcessor
    {
        public static void ProcessIndustrySector(IndustrySector theSector, CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var consumptionResult = theSector.GetInput();
            var consumptionGoodsList = new Dictionary<ICargoable, long>();

            foreach (var goodEntry in consumptionResult.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var finalRequiredAmount = goodEntry.Value;

                consumptionGoodsList.Add(good, finalRequiredAmount);
            }

            var hasAllRequiredConsumption = StorageSpaceProcessor.HasRequiredItems(stockpile, consumptionGoodsList);
            if (hasAllRequiredConsumption == false)
                return;

            foreach (var goodEntry in consumptionResult.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var finalRequiredAmount = goodEntry.Value;

                StorageSpaceProcessor.RemoveCargo(stockpile, good, finalRequiredAmount);
            }

            var productionResult = theSector.GetOutput();
            foreach (var goodEntry in productionResult.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var finalRequiredAmount = goodEntry.Value;

                var freeCapacity = StorageSpaceProcessor.GetFreeCapacity(stockpile, good);
                var actualAmountToAdd = Math.Min(finalRequiredAmount, freeCapacity);

                StorageSpaceProcessor.AddCargo(stockpile, good, actualAmountToAdd);
            }
        }

        public static void ProcessAllIndustrySectors(IndustryAllSectors theEconomy, CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var orderedList = theEconomy.GetSectorsInOrderOfDescendingPriority();

            foreach (var sector in orderedList)
            {
                ProcessIndustrySector(sector, stockpile, tradeGoodsLibrary);
            }
        }
    }
}
