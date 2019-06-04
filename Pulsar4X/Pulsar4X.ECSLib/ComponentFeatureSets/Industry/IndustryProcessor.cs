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
            var productionResult = theSector.ProductionResult();

            foreach (var goodEntry in consumptionResult.FullItems)
            {
                var good = _tradeGoodsDefinitions[goodEntry.Key];
                StorageSpaceProcessor.RemoveCargo(stockpile, good, goodEntry.Value);
            }

            foreach (var goodEntry in productionResult.FullItems)
            {
                var good = _tradeGoodsDefinitions[goodEntry.Key];
                var freeCapacity = StorageSpaceProcessor.GetFreeCapacity(stockpile, good);
                var actualAmountToAdd = Math.Min(goodEntry.Value, freeCapacity);

                StorageSpaceProcessor.AddCargo(stockpile, good, actualAmountToAdd);
            }
        }
    }
}
