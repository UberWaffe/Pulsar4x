using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Pulsar4X.ECSLib;
using System.Diagnostics;
using Pulsar4X.ECSLib.ComponentFeatureSets.CargoStorage;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Industrial Economy Tests")]
    class IndustrialEconomyTests
    {
        private const int _metricTon = 1000;
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
        public void BatchTradeGoods_When_GivenANegativeNumber_ShouldGive_AbsoluteOfInput()
        {
            var theBatch = new BatchTradeGoods();
            var cookies = SetupCookieTradeGood();

            theBatch.AddTradeGood(cookies, -999);
            var cookieCount = theBatch.Items[cookies.ID];

            Assert.AreEqual(cookieCount, 999);
        }
        
        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingRecipesWithNInputs_Should_OutputProduct()
        {
            var cookies = SetupCookieTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies });

            var cookieClickery = SetupCookieClickeryIndustry(cookies);

            var cookieSector = new IndustrySector(cookieClickery, 1);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1 }
            };

            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, cookieCheck);
            Assert.IsFalse(hasCookies);

            cookiePile = cookieSector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, cookieCheck);
            Assert.IsTrue(hasCookies);
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingRecipes_Should_ConsumeInputsAndOutputProduct()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies, flour });

            var cookieBakeries = SetupCookiesFromFlourIndustry(cookies, flour);

            var cookieSector = new IndustrySector(cookieBakeries, 1);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.AvailableCargo.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1800 }
            };
            
            StorageSpaceProcessor.AddCargo(cookiePile.AvailableCargo, flour, 2);
            cookiePile = cookieSector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, cookieCheck);
            Assert.IsTrue(hasCookies);

            var flourCheck = new Dictionary<ICargoable, int>
            {
                { flour, 1 }
            };
            var hasFlour = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, flourCheck);
            Assert.IsFalse(hasFlour);
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingRecipeWithInsufficientInputsForEvenOneBatch_Should_NotConsumeOrProduceAnything()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies, flour });

            var cookieBakeries = SetupCookiesFromFlourIndustry(cookies, flour);

            var cookieSector = new IndustrySector(cookieBakeries, 1);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.AvailableCargo.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1800 }
            };
            
            StorageSpaceProcessor.AddCargo(cookiePile.AvailableCargo, flour, 1);
            cookiePile = cookieSector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, cookieCheck);
            Assert.IsFalse(hasCookies);

            var flourCheck = new Dictionary<ICargoable, int>
            {
                { flour, 1 }
            };
            var hasFlour = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, flourCheck);
            Assert.IsTrue(hasFlour);
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingRecipeWithInsufficientInputsForAllBatches_Should_ProcessAsManyBatchesAsPossible()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies, flour });

            var cookieBakeries = SetupCookiesFromFlourIndustry(cookies, flour);

            var cookieSector = new IndustrySector(cookieBakeries, 3);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.AvailableCargo.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            
            StorageSpaceProcessor.AddCargo(cookiePile.AvailableCargo, flour, (2 * 2) + 1);
            cookiePile = cookieSector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            var finalHasCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1800 * 2 },
                { flour, 1 }
            };
            Assert.IsTrue(CargoHasExactNumbers(cookiePile.AvailableCargo, finalHasCheck));
        }

        [Test]
        public void IndustryProcessor_SingleSectorWithMultipleIndustryCount_When_ProcessingRecipe_Should_ProcessRecipeMultipleTimesAccordingToCount()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies, flour });

            var cookieBakeries = SetupCookiesFromFlourIndustry(cookies, flour);

            var cookieSector = new IndustrySector(cookieBakeries, 100);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.AvailableCargo.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            var cookieCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 180000 }
            };
            
            StorageSpaceProcessor.AddCargo(cookiePile.AvailableCargo, flour, 200);
            cookiePile = cookieSector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            var hasCookies = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, cookieCheck);
            Assert.IsTrue(hasCookies);

            var flourCheck = new Dictionary<ICargoable, int>
            {
                { flour, 1 }
            };
            var hasFlour = StorageSpaceProcessor.HasRequiredItems(cookiePile.AvailableCargo, flourCheck);
            Assert.IsFalse(hasFlour);
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingWhenInsufficientOutputStorageIsPresent_Should_OnlyConsumeAndProduceWhatCanBeStored()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies, flour });

            var cookieBakeries = SetupCookiesFromFlourIndustry(cookies, flour);

            var cookieSector = new IndustrySector(cookieBakeries, 100);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 1800 * 50, FreeCapacityKg = 1800 * 50 });

            StorageSpaceProcessor.AddCargo(cookiePile.AvailableCargo, flour, 2 * 100);
            cookiePile = cookieSector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            var finalHasCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 1800 * 50 },
                { flour, 2 * 50 }
            };
            Assert.IsTrue(CargoHasExactNumbers(cookiePile.AvailableCargo, finalHasCheck));
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingBatches_Should_ProcessBatchesAsManyTimesAsItHasWorkCapacityFor()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies, flour });

            var slowBakeries = SetupSlowCookiesFromFlourIndustry(cookies, flour);

            var slowBakerySector = new IndustrySector(slowBakeries, 1);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            StorageSpaceProcessor.AddCargo(cookiePile.AvailableCargo, flour, 9999999999 );
            cookiePile = slowBakerySector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            var finalHasCheck = new Dictionary<ICargoable, int>
            {
                { cookies, 2700 * 11 }
            };
            Assert.IsTrue(CargoHasExactNumbers(cookiePile.AvailableCargo, finalHasCheck));
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingBatches_Should_HaveTheCorrectWorkCapacityRemainingAfterProcessingBatches()
        {
            var cookies = SetupCookieTradeGood();
            var flour = SetupFlourTradeGood();

            var tradeGoodsDefinitions = new TradeGoodLibrary(new List<TradeGoodSD>() { cookies, flour });

            var slowBakeries = SetupSlowCookiesFromFlourIndustry(cookies, flour);

            var slowBakerySector = new IndustrySector(slowBakeries, 1);

            var cookiePile = new CargoAndServices(tradeGoodsDefinitions);
            cookiePile.AvailableCargo.StoredCargoTypes.Add(flour.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });
            cookiePile.AvailableCargo.StoredCargoTypes.Add(cookies.CargoTypeID, new CargoTypeStore() { MaxCapacityKg = 9999999999999, FreeCapacityKg = 9999999999999 });

            StorageSpaceProcessor.AddCargo(cookiePile.AvailableCargo, flour, 9999999999);
            cookiePile = slowBakerySector.ProcessBatches(cookiePile, tradeGoodsDefinitions);

            Assert.AreEqual(2, slowBakerySector.RemainingWorkCapacity);
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_ProcessingMultipleDifferentBatchesAndHasWorkCapacity_Should_OutputTheCorrectResulstFromAllRecipes()
        {
            Assert.Fail();
        }

        [Test]
        public void IndustryProcessor_AllSectors_When_Processing_Should_ProcessRecipeMultipleTimesAccordingToCount()
        {
            Entity colonyEntity = Entity.Create(_game.GlobalManager, _faction.Guid);

            var goodsLibrary = new TradeGoodLibrary(SetupStandardTradeGoods());
            var colonyStorage = SetupPlanetaryCargo(goodsLibrary);
            var industryLibrary = SetupStandardIndustries(goodsLibrary);

            var industryList = industryLibrary.GetAll();

            var allSectors = new IndustryAllSectors(industryList);

            var recycleCount = 157;
            allSectors.SetCount("Recycling and Transmutation", recycleCount);

            StorageSpaceProcessor.AddCargo(colonyStorage.AvailableCargo, goodsLibrary.GetTradeGood("Waste"), recycleCount * 100);
            /* Results in 
             * "Common N-Elements" => 98
             * "Rare N-Elements" => 2
             */

            var refineCount = 77;
            allSectors.SetCount("Refining", refineCount);

            StorageSpaceProcessor.AddCargo(colonyStorage.AvailableCargo, goodsLibrary.GetTradeGood("Sustenance"), refineCount * 40 + 1);
            /*
             * Needs
             * "Common N-Elements" => 98
             * "Rare N-Elements" => 2
             * "Sustenance" => 40
             * Results in 
             * "Materials" => 100
             */

            var heavyCount = 31;
            allSectors.SetCount("Heavy Industry", heavyCount);
            /*
             * Needs
             * "Materials" => 100
             * Results in 
             * "Technology" => 100
             */

            IndustryProcessor.ProcessAllIndustrySectors(allSectors, colonyStorage, goodsLibrary);

            var finalHasCheck = new Dictionary<ICargoable, int>
            {
                { goodsLibrary.GetTradeGood("Sustenance"), 1 },
                { goodsLibrary.GetTradeGood("Common N-Elements"), (recycleCount - refineCount) * 98 },
                { goodsLibrary.GetTradeGood("Rare N-Elements"), (recycleCount - refineCount) * 2 },
                { goodsLibrary.GetTradeGood("Materials"), (refineCount - heavyCount) * 100 },
                { goodsLibrary.GetTradeGood("Technology"), heavyCount * 100 }
            };
            Assert.IsTrue(CargoHasExactNumbers(colonyStorage.AvailableCargo, finalHasCheck));
        }

        [Test]
        public void IndustryProcessor_AllSectors_When_Processing_Should_ProcessInOrderOfPriority()
        {
            Assert.Fail();
        }

        [Test]
        public void IndustryProcessor_AllSectors_When_Processing_Should_AttemptToUseAllAvailableWorkCapacity()
        {
            Assert.Fail();
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_Processing_Should_IncreaseServicesAvailableAccordingToRecipeOutput()
        {
            var internet = SetupTheInternetzIsForKhorneService();

            var serviceDefs = new EconomyServiceLibrary(new List<EconomyServiceSD>() { internet });

            var theIsp = SetupInternetServiceProvider(internet);

            var informationSector = new IndustrySector(theIsp, 1);

            var totallyEmptyCargo = new CargoAndServices(null);

            totallyEmptyCargo = informationSector.ProcessBatches(totallyEmptyCargo, new TradeGoodLibrary(new List<TradeGoodSD>()));

            var theCheckList = new Dictionary<Guid, int>() { { internet.ID, 357 } };
            Assert.IsTrue(ServicesHasExactNumbers(totallyEmptyCargo, theCheckList));
        }

        [Test]
        public void IndustryProcessor_SingleSector_When_Processing_Should_DecreaseServicesAvailableAccordingToRecipeInput()
        {
            var internet = SetupTheInternetzIsForKhorneService();
            var financial = SetupFinancialService();

            var serviceDefs = new EconomyServiceLibrary(new List<EconomyServiceSD>() { internet, financial });
            
            var theBanks = SetupFinancialServicesProvider(internet, financial);

            var financialSector = new IndustrySector(theBanks, 1);

            var totallyEmptyCargo = new CargoAndServices(null);
            totallyEmptyCargo.AvailableServices.SafeValueReplace(internet.ID, 24);

            totallyEmptyCargo = financialSector.ProcessBatches(totallyEmptyCargo, new TradeGoodLibrary(new List<TradeGoodSD>()));

            var theCheckList = new Dictionary<Guid, int>() {
                { internet.ID, 4 },
                { financial.ID, 10 }
            };
            Assert.IsTrue(ServicesHasExactNumbers(totallyEmptyCargo, theCheckList));
        }

        [Test]
        public void IndustryLibrary_OnGetByName_InvalidEntry_Should_ThrowException()
        {
            var theGoods = new TradeGoodLibrary(SetupStandardTradeGoods());
            var industryLibrary = SetupStandardIndustries(theGoods);

            Assert.Throws<Exception>(() => industryLibrary.Get("No such industry"));
        }

        [Test]
        public void IndustryLibrary_OnGetByGuid_InvalidEntry_Should_ThrowException()
        {
            var theGoods = new TradeGoodLibrary(SetupStandardTradeGoods());
            var industryLibrary = SetupStandardIndustries(theGoods);

            Assert.Throws<Exception>(() => industryLibrary.Get(Guid.NewGuid()));
        }

        [Test]
        public void BatchRecipeLibrary_OnGetByName_InvalidEntry_Should_ThrowException()
        {
            var theGoods = new TradeGoodLibrary(SetupStandardTradeGoods());
            var recipeLibrary = SetupStandardRecipes(theGoods);

            Assert.Throws<Exception>(() => recipeLibrary.Get("This trade good doesn't exist"));
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

        private List<TradeGoodSD> SetupStandardTradeGoods(Nullable<Guid> cargoType = null)
        {
            var goodList = new List<TradeGoodSD>();

            var theGood = new TradeGoodSD
            {
                Name = "Sustenance",
                Description = "Food, water, breathable air, clothing, any abundant elements (Hydrogen, oxygen, Carbon, etc.), simple products from abundant elements (hydrocarbons, etc.), dealing with pollution byproducts (cleaning waste water, scrubbing air pollution, etc.), etc.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Common N-Elements",
                Description = "Aluminium, Silicon, Iron, any abundant element that requires effort to process or extract, etc.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Rare N-Elements",
                Description = "Gold, Silver, Uranium, etc.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Waste",
                Description = "Waste (non bio-degradable, or at least not easily recyclable). Bio-degradable handling is part of sustenance handling.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Materials",
                Description = "Alloys, Plastics, Chemicals, etc.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Technology",
                Description = "Industrial machinery, vehicles, industrial electronics, engines, etc.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Consumer Goods",
                Description = "Electronics, Furniture, etc.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Art and Artifacts",
                Description = "Great artworks or Alien artifacts. Generates more wealth than normal when traded.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Luxuries",
                Description = "Luxuries goods, luxury foods, art, music, etc.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "High Tech",
                Description = "Industrial machinery, vehicles, industrial electronics, engines, etc. technologically advanced enough to manipulate matter on an atomic level.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Forge Tech",
                Description = "Industrial machinery, vehicles, industrial electronics, engines, etc. technologically advanced enough to directly manipulate and convert energy fields.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            theGood = new TradeGoodSD
            {
                Name = "Omega Tech",
                Description = "Industrial machinery, vehicles, industrial electronics, engines, etc. technologically advanced enough to manipulate gravity and other fundamental forces.",
                ID = Guid.NewGuid(),
                CargoTypeID = cargoType ?? Guid.NewGuid(),
                Mass = 5 * _metricTon
            };
            goodList.Add(theGood);

            return goodList;
        }

        private CargoAndServices SetupPlanetaryCargo(ICargoDefinitionsLibrary theGoods)
        {
            var theStorage = new CargoAndServices(theGoods);

            foreach (var goodEntry in theGoods.GetAllList())
            {
                if (!theStorage.AvailableCargo.StoredCargoTypes.ContainsKey(goodEntry.CargoTypeID))
                {
                    theStorage.AvailableCargo.StoredCargoTypes.Add(goodEntry.CargoTypeID, new CargoTypeStore());
                    theStorage.AvailableCargo.StoredCargoTypes[goodEntry.CargoTypeID].MaxCapacityKg = 1000000 * _metricTon;
                    theStorage.AvailableCargo.StoredCargoTypes[goodEntry.CargoTypeID].FreeCapacityKg = 1000000 * _metricTon;
                }
                StorageSpaceProcessor.AddCargo(theStorage.AvailableCargo, goodEntry, 0);
            }
            
            return theStorage;
        }

        private IndustrySD SetupCookieClickeryIndustry(TradeGoodSD cookies)
        {
            var recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(cookies, 1);

            var clickedCookieRecipe = new BatchRecipe("1", new BatchTradeGoods(), recipeResult);

            var cookieClickery = new IndustrySD
            {
                Name = "Cookie Clickery",
                Description = "It is like a bakery, but with less flour and more eldritch horrors.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = clickedCookieRecipe
            };

            return cookieClickery;
        }

        private IndustrySD SetupCookiesFromFlourIndustry(TradeGoodSD cookies, TradeGoodSD flour)
        {
            var recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(flour, 2);

            var recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(cookies, 1800);

            var bakedCookieRecipe = new BatchRecipe("1", recipeCost, recipeResult);

            var bakery = new IndustrySD
            {
                Name = "Cookie Bakery",
                Description = "It is like Grandma, but with less wrinkles. Uses flour and spit to make cookies.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = bakedCookieRecipe
            };

            return bakery;
        }

        private IndustrySD SetupSlowCookiesFromFlourIndustry(TradeGoodSD cookies, TradeGoodSD flour)
        {
            var recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(flour, 2);

            var recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(cookies, 2700);

            var bakedCookieRecipe = new BatchRecipe("1", recipeCost, recipeResult, 999, 5);

            var bakery = new IndustrySD
            {
                Name = "Cookie Bakery",
                Description = "It is like Grandma, but with less wrinkles. Uses flour and spit to slow bake cookies.",
                ID = Guid.NewGuid(),
                WorkCapacity = 57,
                BatchRecipe = bakedCookieRecipe
            };

            return bakery;
        }

        private BatchRecipeLibrary SetupStandardRecipes(ICargoDefinitionsLibrary theGoods)
        {
            var result = new BatchRecipeLibrary();

            // MaterialsFromElements
            // ---------------------------------------------------------------------------
            var recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Common N-Elements"), 98);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Rare N-Elements"), 2);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Sustenance"), 40);

            var recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(theGoods.GetTradeGood("Materials"), 100);

            var materialsRecipe = new BatchRecipe("MaterialsFromElements", recipeCost, recipeResult, 950);

            result.Add(materialsRecipe);

            // RecyclingNelementsFromWaste
            // ---------------------------------------------------------------------------
            recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Waste"), 100);

            recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(theGoods.GetTradeGood("Common N-Elements"), 98);
            recipeResult.AddTradeGood(theGoods.GetTradeGood("Rare N-Elements"), 2);

            materialsRecipe = new BatchRecipe("RecyclingNelementsFromWaste", recipeCost, recipeResult, 1000);

            result.Add(materialsRecipe);

            // Technology
            // ---------------------------------------------------------------------------
            recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Materials"), 100);

            recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(theGoods.GetTradeGood("Technology"), 100);

            materialsRecipe = new BatchRecipe("Technology", recipeCost, recipeResult, 900);

            result.Add(materialsRecipe);

            // ConsumerGoods
            // ---------------------------------------------------------------------------
            recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Materials"), 50);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Technology"), 50);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Sustenance"), 5);

            recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(theGoods.GetTradeGood("Consumer Goods"), 100);

            materialsRecipe = new BatchRecipe("ConsumerGoods", recipeCost, recipeResult, 890);

            result.Add(materialsRecipe);

            // Luxuries
            // ---------------------------------------------------------------------------
            recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Materials"), 46);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Rare N-Elements"), 4);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Sustenance"), 50);

            recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(theGoods.GetTradeGood("Luxuries"), 100);

            materialsRecipe = new BatchRecipe("Luxuries", recipeCost, recipeResult, 880);

            result.Add(materialsRecipe);

            // ArtworkAndCreativeWorks
            // ---------------------------------------------------------------------------
            recipeCost = new BatchTradeGoods();
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Materials"), 20);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Rare N-Elements"), 20);
            recipeCost.AddTradeGood(theGoods.GetTradeGood("Sustenance"), 60);

            recipeResult = new BatchTradeGoods();
            recipeResult.AddTradeGood(theGoods.GetTradeGood("Art and Artifacts"), 100);

            materialsRecipe = new BatchRecipe("ArtworkAndCreativeWorks", recipeCost, recipeResult, 870);

            result.Add(materialsRecipe);

            return result;
        }

        private IndustryLibrary SetupStandardIndustries(ICargoDefinitionsLibrary theGoods)
        {
            var result = new IndustryLibrary();
            var recipes = SetupStandardRecipes(theGoods);

            // Refining
            // ---------------------------------------------------------------------------
            var theIndustry = new IndustrySD
            {
                Name = "Refining",
                Description = "Processes common N-Elements and rare N-Elements into Materials.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = recipes.Get("MaterialsFromElements"),
                Priority = 950
            };

            result.Add(theIndustry);

            // Recycling and Transmutation
            // ---------------------------------------------------------------------------
            theIndustry = new IndustrySD
            {
                Name = "Recycling and Transmutation",
                Description = "Recycles waste back into the constituent common and rare N-Elements.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = recipes.Get("RecyclingNelementsFromWaste"),
                Priority = 1000
            };

            result.Add(theIndustry);

            // Heavy Industry
            // ---------------------------------------------------------------------------
            theIndustry = new IndustrySD
            {
                Name = "Heavy Industry",
                Description = "Manufactures technology and components.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = recipes.Get("Technology"),
                Priority = 900
            };

            result.Add(theIndustry);

            // Consumer Industry
            // ---------------------------------------------------------------------------
            theIndustry = new IndustrySD
            {
                Name = "Consumer Industry",
                Description = "Manufactures consumer goods for use by the general populace.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = recipes.Get("ConsumerGoods"),
                Priority = 890
            };

            result.Add(theIndustry);

            return result;
        }

        private bool CargoHasExactNumbers (CargoStorageDB theCargo, Dictionary<ICargoable, int> theCheckList)
        {
            Assert.IsTrue(StorageSpaceProcessor.HasRequiredItems(theCargo, theCheckList));

            foreach (var checkEntry in theCheckList)
            {
                var doesNotHaveCheck = new Dictionary<ICargoable, int>
                {
                    { checkEntry.Key, checkEntry.Value + 1 }
                };

                Assert.IsFalse(StorageSpaceProcessor.HasRequiredItems(theCargo, doesNotHaveCheck));
            }

            return true;
        }

        private bool ServicesHasExactNumbers(CargoAndServices theServices, Dictionary<Guid, int> theCheckList)
        {
            foreach (var checkEntry in theCheckList)
            {
                Assert.AreEqual(checkEntry.Value, theServices.AvailableServices[checkEntry.Key]);
            }

            return true;
        }

        private EconomyServiceSD SetupTheInternetzIsForKhorneService()
        {
            var internet = new EconomyServiceSD
            {
                Name = "Internetz",
                Description = "Bytes for the byte god! Servers for the server room! [REDACTED] for the [REDACTED]!",
                ID = Guid.NewGuid()
            };

            return internet;
        }

        private EconomyServiceSD SetupFinancialService()
        {
            var financials = new EconomyServiceSD
            {
                Name = "Financial Services",
                Description = "Digital money and trades.",
                ID = Guid.NewGuid()
            };

            return financials;
        }

        private IndustrySD SetupInternetServiceProvider(EconomyServiceSD theInternet)
        {
            var recipeResult = new BatchTradeGoods();
            recipeResult.ChangeService(theInternet, 357);

            var internetHosting = new BatchRecipe("1", new BatchTradeGoods(), recipeResult);

            var isp = new IndustrySD
            {
                Name = "Internet Service Provider",
                Description = "Converts electricity to heat using transistors and charges you for it.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = internetHosting
            };

            return isp;
        }

        private IndustrySD SetupFinancialServicesProvider(EconomyServiceSD theInternet, EconomyServiceSD financials)
        {
            var recipeCost = new BatchTradeGoods();
            recipeCost.ChangeService(theInternet, 20);

            var recipeResult = new BatchTradeGoods();
            recipeResult.ChangeService(financials, 10);

            var financialTransactions = new BatchRecipe("1", recipeCost, recipeResult);

            var theBanks = new IndustrySD
            {
                Name = "Financial Institutions",
                Description = "Moves digital numbers around. Apparently very important.",
                ID = Guid.NewGuid(),
                WorkCapacity = 1,
                BatchRecipe = financialTransactions
            };

            return theBanks;
        }

    }

    public class TradeGoodLibrary : ICargoDefinitionsLibrary
    {
        private Dictionary<Guid, ICargoable> _definitions;
        private Dictionary<Guid, TradeGoodSD> _goods;

        #region Not Implemented
        public Dictionary<Guid, ICargoable> GetAll()
        {
            throw new NotImplementedException();
        }

        public object GetAny(Guid id)
        {
            throw new NotImplementedException();
        }
        
        public ProcessedMaterialSD GetMaterial(string name)
        {
            throw new NotImplementedException();
        }

        public ProcessedMaterialSD GetMaterial(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Guid, ProcessedMaterialSD> GetMaterials()
        {
            throw new NotImplementedException();
        }

        public List<ProcessedMaterialSD> GetMaterialsList()
        {
            throw new NotImplementedException();
        }

        public MineralSD GetMineral(string name)
        {
            throw new NotImplementedException();
        }

        public MineralSD GetMineral(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Dictionary<Guid, MineralSD> GetMinerals()
        {
            throw new NotImplementedException();
        }

        public List<MineralSD> GetMineralsList()
        {
            throw new NotImplementedException();
        }

        public bool IsMaterial(Guid id)
        {
            throw new NotImplementedException();
        }

        public bool IsMineral(Guid id)
        {
            throw new NotImplementedException();
        }

        public bool IsOther(Guid id)
        {
            throw new NotImplementedException();
        }
        
        public void LoadDefinitions(List<MineralSD> minerals, List<ProcessedMaterialSD> processedMaterials, List<TradeGoodSD> goodsCargo, List<ICargoable> otherCargo)
        {
            throw new NotImplementedException();
        }

        public void LoadMaterialsDefinitions(List<ProcessedMaterialSD> materials)
        {
            throw new NotImplementedException();
        }

        public void LoadMineralDefinitions(List<MineralSD> minerals)
        {
            throw new NotImplementedException();
        }

        public void LoadOtherDefinitions(List<ICargoable> otherCargo)
        {
            throw new NotImplementedException();
        }
        #endregion


        public ICargoable GetCargo(Guid id)
        {
            return _definitions[id];
        }

        public Guid GetCargoType(Guid id)
        {
            return _definitions[id].CargoTypeID;
        }

        public List<ICargoable> GetAllList()
        {
            return _definitions.Values.ToList();
        }

        public ICargoable GetOther(string nameOfCargo)
        {
            if (_definitions.Values.Any(tg => tg.Name == nameOfCargo))
            {
                return _definitions.Values.Single(tg => tg.Name == nameOfCargo);
            }

            throw new Exception("Cargo item with the name " + nameOfCargo + " not found in TradeGoodLibrary. Was the trade good properly loaded?");
        }

        public ICargoable GetOther(Guid guidOfCargo)
        {
            return _definitions[guidOfCargo];
        }



        #region Trade Goods
        public TradeGoodLibrary(List<TradeGoodSD> tradeGoods)
        {
            _definitions = new Dictionary<Guid, ICargoable>();
            _goods = new Dictionary<Guid, TradeGoodSD>();

            LoadTradeGoodsDefinitions(tradeGoods);
        }

        public bool IsTradeGood(Guid id)
        {
            return _goods.ContainsKey(id);
        }

        public TradeGoodSD GetTradeGood(string name)
        {
            var result = GetOther(name);
            return _goods[result.ID];
        }

        public TradeGoodSD GetTradeGood(Guid guid)
        {
            var result = GetOther(guid);
            return _goods[result.ID];
        }

        public Dictionary<Guid, TradeGoodSD> GetTradeGoods()
        {
            return _goods;
        }

        public List<TradeGoodSD> GetTradeGoodsList()
        {
            return _goods.Values.ToList();
        }

        public void LoadTradeGoodsDefinitions(List<TradeGoodSD> goodsCargo)
        {
            if (goodsCargo != null)
            {
                foreach (var entry in goodsCargo)
                {
                    _definitions[entry.ID] = entry;
                    _goods[entry.ID] = entry;
                }
            }
        }
        #endregion
    }
}
