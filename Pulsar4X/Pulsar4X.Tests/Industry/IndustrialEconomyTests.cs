using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using System.Diagnostics;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Industrial Economy Tests")]
    class IndustrialEconomyTests
    {
        private Game _game;
        private EntityManager _entityManager;
        private Entity _faction;
        private Entity _workerSpecies;

        [SetUp]
        public void Init()
        {
            var gameSettings = new NewGameSettings();
            gameSettings.MaxSystems = 10;
            _game = new Game(gameSettings);
            StaticDataManager.LoadData("Pulsar4x", _game);
            _entityManager = new EntityManager(_game);

            _faction = FactionFactory.CreateFaction(_game, "Plebians");
            _workerSpecies = SpeciesFactory.CreateSpeciesHuman(_faction, _entityManager);
        }

        [TearDown]
        public void Cleanup()
        {
            _game = null;
            _entityManager = null;
            _faction = null;
            _workerSpecies = null;
        }

        [Test]
        public void BatchesShouldTreatNegativeAmountsAsPositive()
        {
            var theBatch = new BatchTradeGoods();
            var cookies = SetupCookieTradeGood();

            theBatch.AddTradeGood(cookies, -999);
            var cookieCount = theBatch.FullItems[cookies.ID];

            Assert.AreEqual(cookieCount, 999);
        }

        [Test]
        public void ForACargoHasRequiredItemCheckTheResultShouldBeFalseIfItDoesnotContainTheTargetCargoType()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new CargoStorageDB();

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);
        }

        [Test]
        public void ForACargoHasRequiredItemCheckTheResultShouldBeFalseIfItDoesnotContainTheTargetCargo()
        {
            var cookies = SetupCookieTradeGood();
            var biscuits = SetupCookieTradeGood();
            biscuits.CargoTypeID = cookies.CargoTypeID;

            var cookiePile = new CargoStorageDB();
            StorageSpaceProcessor.AddCargo(cookiePile, biscuits, 1);

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);
        }

        [Test]
        public void ForACargoHasRequiredItemCheckTheResultShouldBeTrueIfYouHaveTheExactNeededAmount()
        {
            var cookies = SetupCookieTradeGood();

            var cookiePile = new CargoStorageDB();
            StorageSpaceProcessor.AddCargo(cookiePile, cookies, 7);

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 6 }
            };
            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);
            Assert.IsTrue(hasCookies);

            cookieCheck[cookies] = 7;
            hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);
            Assert.IsTrue(hasCookies);

            cookieCheck[cookies] = 8;
            hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);
            Assert.IsFalse(hasCookies);
        }

        [Test]
        public void CivilianIndustryShouldBeAbleToProduceTradeGoods()
        {
            var cookies = SetupCookieTradeGood();

            var tradeGoodsDefinitions = new Dictionary<Guid, TradeGoodSD>
            {
                { cookies.ID, cookies }
            };

            var cookieClickery = SetupCookieClickeryIndustry(cookies);

            var cookieSector = new IndustrySector(cookieClickery);

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsFalse(hasCookies);

            var industry = new IndustryProcessor(tradeGoodsDefinitions);
            industry.ProcessIndustrySector(cookieSector, cookiePile);

            hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);

            Assert.IsTrue(hasCookies);
        }

        [Test]
        public void IndustryShouldBeAbleToConsumeCargoInOrderToProduce()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new Dictionary<Guid, TradeGoodSD>
            {
                { cookies.ID, cookies },
                { flour.ID, flour }
            };

            var cookieBakeries = SetupCookiesFromFlourIndustry(cookies, flour);

            var cookieSector = new IndustrySector(cookieBakeries);

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1800 }
            };

            var industry = new IndustryProcessor(tradeGoodsDefinitions);

            StorageSpaceProcessor.AddCargo(cookiePile, flour, 2);
            industry.ProcessIndustrySector(cookieSector, cookiePile);

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);
            Assert.IsTrue(hasCookies);

            var flourCheck = new Dictionary<ICargoable, int>
            {
                { flour, 1 }
            };
            var hasFlour = StorageSpaceProcessor.HasReqiredItems(cookiePile, flourCheck);
            Assert.IsFalse(hasFlour);
        }

        [Test]
        public void IfAnIndustryHasInsufficientStockpileToCoverItsConsumptionThenItShouldProduceAnythingAndNotConsumeAnything()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new Dictionary<Guid, TradeGoodSD>
            {
                { cookies.ID, cookies },
                { flour.ID, flour }
            };

            var cookieBakeries = SetupCookiesFromFlourIndustry(cookies, flour);

            var cookieSector = new IndustrySector(cookieBakeries);

            var cookiePile = new CargoStorageDB();
            cookiePile.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1800 }
            };

            var industry = new IndustryProcessor(tradeGoodsDefinitions);

            StorageSpaceProcessor.AddCargo(cookiePile, flour, 1);
            industry.ProcessIndustrySector(cookieSector, cookiePile);

            var hasCookies = StorageSpaceProcessor.HasReqiredItems(cookiePile, cookieCheck);
            Assert.IsFalse(hasCookies);

            var flourCheck = new Dictionary<ICargoable, int>
            {
                { flour, 1 }
            };
            var hasFlour = StorageSpaceProcessor.HasReqiredItems(cookiePile, flourCheck);
            Assert.IsTrue(hasFlour);
        }

        private TradeGoodSD SetupCookieTradeGood()
        {
            var cookies = new TradeGoodSD
            {
                Name = "Cookies",
                Description = "Tastes like carpal tunnel and time.",
                ID = Guid.NewGuid(),
                CargoTypeID = Guid.NewGuid(),
                Mass = 1
            };

            return cookies;
        }

        private IndustrySD SetupCookieClickeryIndustry(TradeGoodSD cookies)
        {
            var recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(cookies, 1);

            var clickedCookieRecipe = new BatchRecipe(new BatchTradeGoods(), recipeResult);

            var cookieClickery = new IndustrySD
            {
                Name = "Cookie Clickery",
                Description = "It is like a bakery, but with less flour and more eldritch horrors.",
                ID = Guid.NewGuid(),
                BatchRecipe = clickedCookieRecipe
            };

            return cookieClickery;
        }

        private TradeGoodSD SetupFlourTradeGood()
        {
            var flour = new TradeGoodSD
            {
                Name = "Flour",
                Description = "Smells like misspelled flowers.",
                ID = Guid.NewGuid(),
                CargoTypeID = Guid.NewGuid(),
                Mass = 1000
            };

            return flour;
        }

        private IndustrySD SetupCookiesFromFlourIndustry(TradeGoodSD cookies, TradeGoodSD flour)
        {
            var recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(flour, 2);

            var recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(cookies, 1800);

            var bakedCookieRecipe = new BatchRecipe(recipeCost, recipeResult);

            var bakery = new IndustrySD
            {
                Name = "Cookie Bakery",
                Description = "It is like Grandma, but with less wrinkles. Uses flour and spit to make cookies.",
                ID = Guid.NewGuid(),
                BatchRecipe = bakedCookieRecipe
            };

            return bakery;
        }
    }
}
