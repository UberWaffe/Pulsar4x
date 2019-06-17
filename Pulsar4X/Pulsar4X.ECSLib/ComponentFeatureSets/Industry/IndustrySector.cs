using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json.Serialization;
using Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage;

namespace Pulsar4X.ECSLib
{
    public class IndustrySector
    {
        #region parameters
        private IndustrySD _data;

        public long NumberOfIndustry { get; private set; }

        public long RemainingWorkCapacity { get; private set; }

        public long Priority => _data.Priority;
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

        private long ReduceRemainingWorkCapacity(long amount)
        {
            RemainingWorkCapacity -= Math.Abs(amount);
            RemainingWorkCapacity = Math.Max(RemainingWorkCapacity, 0);

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

        public CargoAndServices ProcessBatches(CargoAndServices stockpile, ICargoDefinitionsLibrary library)
        {
            var maximumRecipeBatchesToProcess = CalculateMaximumBatchesPossible(stockpile);
            if (maximumRecipeBatchesToProcess <= 0)
                return stockpile;

            var consumptionResult = GetInputForCount(maximumRecipeBatchesToProcess);
            foreach (var goodEntry in consumptionResult.Items)
            {
                StorageSpaceProcessor.RemoveCargo(stockpile.AvailableCargo, library.GetCargo(goodEntry.Key), goodEntry.Value);
            }
            foreach (var servieEntry in consumptionResult.Services)
            {
                stockpile.AvailableServices[servieEntry.Key] -= servieEntry.Value;
            }

            var productionResult = GetOutputForCount(maximumRecipeBatchesToProcess);
            foreach (var goodEntry in productionResult.Items)
            {
                var freeCapacity = StorageSpaceProcessor.GetAvailableSpace(stockpile.AvailableCargo, goodEntry.Key, library);
                var actualAmountToAdd = Math.Min(goodEntry.Value, freeCapacity.FreeCapacityItem);

                StorageSpaceProcessor.AddCargo(stockpile.AvailableCargo, library.GetCargo(goodEntry.Key), actualAmountToAdd);
            }
            foreach (var servieEntry in productionResult.Services)
            {
                stockpile.ChangeService(servieEntry.Key, servieEntry.Value);
            }

            ReduceRemainingWorkCapacity(GetTotalWorkEffortForCount(maximumRecipeBatchesToProcess));

            return stockpile;
        }

        private long CalculateMaximumBatchesPossible(CargoAndServices stockpile)
        {
            var finalBatchesCount = long.MaxValue;
            finalBatchesCount = Math.Min(finalBatchesCount, CalculateMaximumBatchesPossibleWithOutputStorage(stockpile));
            finalBatchesCount = Math.Min(finalBatchesCount, CalculateMaximumBatchesPossibleWithAvailableInputs(stockpile));
            finalBatchesCount = Math.Min(finalBatchesCount, CalculateMaximumBatchesPossibleWithCurrentWorkCapacity());

            return finalBatchesCount;
        }
        
        private long CalculateMaximumBatchesPossibleWithAvailableInputs(CargoAndServices stockpile)
        {
            var consumptionRequirementsOfOneBatch = GetInputForCount(1);
            return stockpile.CalculateMaximumBatchesPossibleWithAvailableInputs(consumptionRequirementsOfOneBatch);
        }

        private long CalculateMaximumBatchesPossibleWithOutputStorage(CargoAndServices stockpile)
        {
            var productionResultsOfOneBatch = GetOutputForCount(1);
            return stockpile.CalculateMaximumBatchesPossibleWithOutputStorage(productionResultsOfOneBatch);
        }

        private long CalculateMaximumBatchesPossibleWithCurrentWorkCapacity()
        {
            var workPerBatch = GetRecipe().WorkRequired;
            if (workPerBatch <= 0)
                return long.MaxValue;

            var finalBatchesCount = RemainingWorkCapacity / workPerBatch;
            
            return finalBatchesCount;
        }

        #endregion
    }


}