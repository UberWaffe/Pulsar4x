using System;
using System.Collections.Generic;

namespace Pulsar4X.ECSLib
{
    public class CargoAndServices
    {
        #region Properties
        public CargoStorageDB AvailableCargo { get; private set;  } = new CargoStorageDB();
        public Dictionary<Guid, long> AvailableServices { get; private set; } = new Dictionary<Guid, long>();

        private TradeGoodLibrary _tradeGoodsLibrary;
        #endregion

        #region Constructors
        public CargoAndServices(TradeGoodLibrary tradeGoodsLibrary)
            : this(new CargoStorageDB(), new Dictionary<Guid, long>(), tradeGoodsLibrary)
        {
            
        }

        public CargoAndServices(CargoStorageDB currentCargo, Dictionary<Guid, long> currentAvailableServices, TradeGoodLibrary tradeGoodsLibrary)
        {
            AvailableCargo = currentCargo;
            AvailableServices = currentAvailableServices;
            _tradeGoodsLibrary = tradeGoodsLibrary;
        }
        #endregion

        public long CalculateMaximumBatchesPossibleWithAvailableInputs(BatchTradeGoods consumptionRequirementsOfOneBatch)
        {
            var finalBatchesCount = long.MaxValue;
            
            foreach (var goodEntry in consumptionRequirementsOfOneBatch.Items)
            {
                var good = _tradeGoodsLibrary.Get(goodEntry.Key);
                var amountNeeded = goodEntry.Value;

                var stockpiledAmount = StorageSpaceProcessor.GetAmount(AvailableCargo, good.CargoTypeID, good.ID);
                var countOfBatchesWorthOfStockpile = stockpiledAmount / amountNeeded;

                finalBatchesCount = Math.Min(finalBatchesCount, countOfBatchesWorthOfStockpile);

                if (finalBatchesCount <= 0)
                    return 0;
            }

            foreach (var serviceEntry in consumptionRequirementsOfOneBatch.Services)
            {
                var amountNeeded = serviceEntry.Value;

                var availableAmount = AvailableServices[serviceEntry.Key];
                var countOfBatchesWorthOfService = availableAmount / amountNeeded;

                finalBatchesCount = Math.Min(finalBatchesCount, countOfBatchesWorthOfService);

                if (finalBatchesCount <= 0)
                    return 0;
            }

            return finalBatchesCount;
        }

        public long CalculateMaximumBatchesPossibleWithOutputStorage(BatchTradeGoods productionResultsOfOneBatch)
        {
            var finalBatchesCount = long.MaxValue;
            
            foreach (var goodEntry in productionResultsOfOneBatch.Items)
            {
                var good = _tradeGoodsLibrary.Get(goodEntry.Key);
                var spaceNeededForThisGoodForOneBatch = goodEntry.Value;

                var freeCapacity = StorageSpaceProcessor.GetFreeCapacity(AvailableCargo, good);
                var countOfBatchesWorthOfSpace = freeCapacity / spaceNeededForThisGoodForOneBatch;

                finalBatchesCount = Math.Min(finalBatchesCount, countOfBatchesWorthOfSpace);
            }

            return finalBatchesCount;
        }

        public long ChangeService(Guid service, long amount)
        {
            AvailableServices.SafeValueAdd(service, amount);
            return AvailableServices[service];
        }

    }
}
