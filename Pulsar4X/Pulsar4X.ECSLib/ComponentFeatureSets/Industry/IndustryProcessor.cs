using System;
using System.Collections.Generic;
using System.Linq;


namespace Pulsar4X.ECSLib
{
    internal class IndustryProcessor
    {
        private Dictionary<Guid, TradeGoodSD> _tradeGoodsDefinitions;

        public IndustryProcessor(Dictionary<Guid, TradeGoodSD> tradeGoods)
        {
            _tradeGoodsDefinitions = tradeGoods;
        }

        public void ProcessIndustrySector(IndustrySector theSector, CargoStorageDB stockpile)
        {
            var consumptionResult = theSector.ConsumptionResult();
            var consumptionGoodsList = new Dictionary<ICargoable, long>();
            var batchCount = theSector.NumberOfIndustry;

            foreach (var goodEntry in consumptionResult.FullItems)
            {
                var good = _tradeGoodsDefinitions[goodEntry.Key];
                var finalRequiredAmount = goodEntry.Value * batchCount;

                consumptionGoodsList.Add(good, finalRequiredAmount);
            }

            var hasAllRequiredConsumption = StorageSpaceProcessor.HasRequiredItems(stockpile, consumptionGoodsList);
            if (hasAllRequiredConsumption == false)
                return;

            foreach (var goodEntry in consumptionResult.FullItems)
            {
                var good = _tradeGoodsDefinitions[goodEntry.Key];
                var finalRequiredAmount = goodEntry.Value * batchCount;

                StorageSpaceProcessor.RemoveCargo(stockpile, good, finalRequiredAmount);
            }

            var productionResult = theSector.ProductionResult();
            foreach (var goodEntry in productionResult.FullItems)
            {
                var good = _tradeGoodsDefinitions[goodEntry.Key];
                var finalRequiredAmount = goodEntry.Value * batchCount;

                var freeCapacity = StorageSpaceProcessor.GetFreeCapacity(stockpile, good);
                var actualAmountToAdd = Math.Min(finalRequiredAmount, freeCapacity);

                StorageSpaceProcessor.AddCargo(stockpile, good, actualAmountToAdd);
            }
        }
    }
}
