using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    internal static class IndustryProcessor
    {
        public static void ProcessIndustrySector(IndustrySector theSector, CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var maximumRecipeBatchesToProcess = CalculateMaximumBatchesPossible(theSector, stockpile, tradeGoodsLibrary);

            var consumptionResult = theSector.GetInput(maximumRecipeBatchesToProcess);
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

        private static long CalculateMaximumBatchesPossible(IndustrySector theSector, CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var finalBatchesCount = theSector.NumberOfIndustry;

            var productionResultsOfOneBatch = theSector.GetOutput(1);
            foreach (var goodEntry in productionResultsOfOneBatch.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var spaceNeededForThisGoodForOneBatch = goodEntry.Value;

                var freeCapacity = StorageSpaceProcessor.GetFreeCapacity(stockpile, good);
                var countOfBatchesWorthOfSpace = freeCapacity / spaceNeededForThisGoodForOneBatch;

                finalBatchesCount = Math.Min(finalBatchesCount, countOfBatchesWorthOfSpace);
            }

            return finalBatchesCount;
        }
    }
}
