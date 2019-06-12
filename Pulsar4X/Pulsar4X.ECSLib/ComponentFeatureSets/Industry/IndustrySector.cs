using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json.Serialization;

namespace Pulsar4X.ECSLib
{
    public class IndustrySector
    {
        #region parameters
        private IndustrySD _data;

        public long NumberOfIndustry { get; private set; }

        public long RemainingWorkCapacity { get; private set; }
        #endregion

        #region constructors
        public IndustrySector(IndustrySD instance, long size = 0)
        {
            _data = instance;
            NumberOfIndustry = size;
            RemainingWorkCapacity = StartNewWorkCycle();

            if (_data.BatchRecipe == null) _data.BatchRecipe = new BatchRecipe();
        }
        #endregion

        #region functions
        public void SetCount(int count)
        {
            NumberOfIndustry = count;
            StartNewWorkCycle();
        }

        public long StartNewWorkCycle()
        {
            RemainingWorkCapacity = _data.WorkCapacity * NumberOfIndustry;
            return RemainingWorkCapacity;
        }

        public BatchRecipe GetRecipe()
        {
            return _data.BatchRecipe;
        }

        public BatchTradeGoods GetInputForCount(long count)
        {
            var result = new BatchTradeGoods(GetRecipe().InputGoods);
            result.MultiplyDivideBy(count, 1);

            return result;
        }

        public BatchTradeGoods GetOutputForCount(long count)
        {
            var result = new BatchTradeGoods(GetRecipe().ResultGoods);
            result.MultiplyDivideBy(count, 1);

            return result;
        }

        public long GetTotalWorkEffortForCount(long count)
        {
            return count * GetRecipe().WorkRequired;
        }

        public bool IsIndustry(string name)
        {
            return (_data.Name == name);
        }

        public CargoStorageDB ProcessBatches(CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var maximumRecipeBatchesToProcess = CalculateMaximumBatchesPossible(stockpile, tradeGoodsLibrary);

            var consumptionResult = GetInputForCount(maximumRecipeBatchesToProcess);
            var consumptionGoodsList = new Dictionary<ICargoable, long>();

            foreach (var goodEntry in consumptionResult.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var finalRequiredAmount = goodEntry.Value;

                consumptionGoodsList.Add(good, finalRequiredAmount);
            }

            var hasAllRequiredConsumption = StorageSpaceProcessor.HasRequiredItems(stockpile, consumptionGoodsList);
            if (hasAllRequiredConsumption == false)
                return stockpile;

            foreach (var goodEntry in consumptionResult.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var finalRequiredAmount = goodEntry.Value;

                StorageSpaceProcessor.RemoveCargo(stockpile, good, finalRequiredAmount);
            }

            var productionResult = GetOutputForCount(maximumRecipeBatchesToProcess);
            foreach (var goodEntry in productionResult.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var finalRequiredAmount = goodEntry.Value;

                var freeCapacity = StorageSpaceProcessor.GetFreeCapacity(stockpile, good);
                var actualAmountToAdd = Math.Min(finalRequiredAmount, freeCapacity);

                StorageSpaceProcessor.AddCargo(stockpile, good, actualAmountToAdd);
            }

            ReduceRemainingWorkCapacity(GetTotalWorkEffortForCount(maximumRecipeBatchesToProcess));

            return stockpile;
        }

        private long CalculateMaximumBatchesPossible(CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var finalBatchesCount = long.MaxValue;
            finalBatchesCount = Math.Min(finalBatchesCount, CalculateMaximumBatchesPossibleWithOutputStorage(stockpile, tradeGoodsLibrary));
            finalBatchesCount = Math.Min(finalBatchesCount, CalculateMaximumBatchesPossibleWithInputStockpiles(stockpile, tradeGoodsLibrary));
            finalBatchesCount = Math.Min(finalBatchesCount, CalculateMaximumBatchesPossibleWithCurrentWorkCapacity());

            return finalBatchesCount;
        }

        private long CalculateMaximumBatchesPossibleWithInputStockpiles(CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var finalBatchesCount = long.MaxValue;

            var consumptionRequirementsOfOneBatch = GetInputForCount(1);
            foreach (var goodEntry in consumptionRequirementsOfOneBatch.FullItems)
            {
                var good = tradeGoodsLibrary.Get(goodEntry.Key);
                var amountNeeded = goodEntry.Value;

                var stockpiledAmount = StorageSpaceProcessor.GetAmount(stockpile, good.CargoTypeID, good.ID);
                var countOfBatchesWorthOfStockpile = stockpiledAmount / amountNeeded;

                finalBatchesCount = Math.Min(finalBatchesCount, countOfBatchesWorthOfStockpile);
            }
            return finalBatchesCount;
        }

        private long CalculateMaximumBatchesPossibleWithOutputStorage(CargoStorageDB stockpile, TradeGoodLibrary tradeGoodsLibrary)
        {
            var finalBatchesCount = long.MaxValue;

            var productionResultsOfOneBatch = GetOutputForCount(1);
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

        private long CalculateMaximumBatchesPossibleWithCurrentWorkCapacity()
        {
            var finalBatchesCount = RemainingWorkCapacity / GetRecipe().WorkRequired;
            
            return finalBatchesCount;
        }

        private long ReduceRemainingWorkCapacity(long amount)
        {
            RemainingWorkCapacity -= Math.Abs(amount);
            RemainingWorkCapacity = Math.Max(RemainingWorkCapacity, 0);

            return RemainingWorkCapacity;
        }

        #endregion
    }


}