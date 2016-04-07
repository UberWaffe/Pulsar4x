﻿using NUnit.Framework;
using Pulsar4X.ECSLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pulsar4X.Tests
{
    [TestFixture, Description("Entity Manager Tests")]
    class EntityManagerTests
    {
        private Game _game;
        private AuthenticationToken _smAuthToken;
        private Entity _species1;
        private Dictionary<Entity, long> _pop1;
        private Dictionary<Entity, long> _pop2;

        [SetUp]
        public void Init()
        {
            var settings = new NewGameSettings {GameName = "Test Game", StartDateTime = DateTime.Now, MaxSystems = 1};

            _game = new Game(settings);
            _smAuthToken = new AuthenticationToken(_game.SpaceMaster);
            _game.GenerateSystems(_smAuthToken, 1);
            _species1 = Entity.Create(_game.GlobalManager, new List<BaseDataBlob> {new SpeciesDB(1, 0.1, 1.9, 1.0, 0.4, 4, 14, -15, 45)});
            _pop1 = new Dictionary<Entity, long> { { _species1, 10 } };
            _pop2 = new Dictionary<Entity, long> { { _species1, 5 } };
        }

        [Test]
        [Ignore("Stress Test.")]
        public void CloneStress()
        {
            Entity humanFaction = FactionFactory.CreateFaction(_game, "Humans");
            Entity harbingerShipClass =  ShipFactory.CreateNewShipClass(_game, humanFaction, "Harbinger");
            for (int i = 0; i < 1000000; i++)
            {
                harbingerShipClass.Clone(_game.GlobalManager);
            }
        }


        [Test]
        public void CreateEntity()
        {
            // create entity with no data blobs:
            Entity testEntity = Entity.Create(_game.GlobalManager);
            Assert.IsTrue(testEntity.IsValid);
            Assert.AreSame(_game.GlobalManager, testEntity.Manager);

            // Check the mask.
            Assert.AreEqual(EntityManager.BlankDataBlobMask(), testEntity.DataBlobMask);

            // Create entity with existing datablobs:
            var dataBlobs = new List<BaseDataBlob> {new OrbitDB(), new ColonyDB(_pop1)};
            testEntity = Entity.Create(_game.GlobalManager, dataBlobs);
            Assert.IsTrue(testEntity.IsValid);

            // Check the mask.
            ComparableBitArray expectedMask = EntityManager.BlankDataBlobMask();
            int orbitTypeIndex = EntityManager.GetTypeIndex<OrbitDB>();
            int colonyTypeIndex = EntityManager.GetTypeIndex<ColonyDB>();
            expectedMask[orbitTypeIndex] = true;
            expectedMask[colonyTypeIndex] = true;

            Assert.AreEqual(expectedMask, testEntity.DataBlobMask);

            // Create entity with existing datablobs, but provide an empty list:
            dataBlobs.Clear();
            testEntity = Entity.Create(_game.GlobalManager, dataBlobs);
            Assert.IsTrue(testEntity.IsValid);
        }

        [Test]
        public void SetDataBlobs()
        {
            Entity testEntity = Entity.Create(_game.GlobalManager);
            testEntity.SetDataBlob(new OrbitDB());
            testEntity.SetDataBlob(new ColonyDB(_pop1));
            testEntity.SetDataBlob(new PositionDB(0, 0, 0, Guid.Empty), EntityManager.GetTypeIndex<PositionDB>());

            // test bad input:
            Assert.Catch(typeof(ArgumentNullException), () =>
            {
                testEntity.SetDataBlob((OrbitDB)null); // should throw ArgumentNullException
            });
        }

        [Test]
        public void GetDataBlobsByEntity()
        {
            Entity testEntity = PopulateEntityManager();  // make sure we have something in there.

            // Get all DataBlobs of a specific entity.
            ReadOnlyCollection<BaseDataBlob> dataBlobs = testEntity.DataBlobs;
            Assert.AreEqual(2, dataBlobs.Count);

            // empty entity mean empty list.
            testEntity = Entity.Create(_game.GlobalManager);  // create empty entity.
            dataBlobs = testEntity.DataBlobs;
            Assert.AreEqual(0, dataBlobs.Count);
        }

        [Test]
        public void GetDataBlobByEntity()
        {
            Entity testEntity = PopulateEntityManager();

            // Get the Population DB of a specific entity.
            ColonyDB popDB = testEntity.GetDataBlob<ColonyDB>();
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            AtmosphereDB atmoDB = testEntity.GetDataBlob<AtmosphereDB>();
            Assert.IsNull(atmoDB);

            // test with invalid data blob type
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                testEntity.GetDataBlob<BaseDataBlob>();
            });

            // and again for the second lookup type:
            // Get the Population DB of a specific entity.
            int typeIndex = EntityManager.GetTypeIndex<ColonyDB>();
            popDB = testEntity.GetDataBlob<ColonyDB>(typeIndex);
            Assert.IsNotNull(popDB);

            // get a DB we know the entity does not have:
            typeIndex = EntityManager.GetTypeIndex<AtmosphereDB>();
            atmoDB = testEntity.GetDataBlob<AtmosphereDB>(typeIndex);
            Assert.IsNull(atmoDB);

            // test with invalid type index:
            Assert.Catch(typeof(ArgumentOutOfRangeException), () =>
            {
                testEntity.GetDataBlob<AtmosphereDB>(-42);
            });

            // test with invalid T vs type at typeIndex
            typeIndex = EntityManager.GetTypeIndex<ColonyDB>();
            Assert.Catch(typeof(InvalidCastException), () =>
            {
                testEntity.GetDataBlob<SystemBodyDB>(typeIndex);
            });
        }

        [Test]
        public void RemoveEntities()
        {
            Entity testEntity = PopulateEntityManager();

            // lets check the entity at index testEntity
            ReadOnlyCollection<BaseDataBlob> testList = testEntity.DataBlobs;
            Assert.AreEqual(2, testList.Count);  // should have 2 datablobs.

            // Remove an entity.
            testEntity.Destroy();

            // now lets see if the entity is still there:
            testList = testEntity.DataBlobs;
            Assert.AreEqual(0, testList.Count);  // should have 0 datablobs.
   
            Assert.IsFalse(testEntity.IsValid);

            // Try to get a bad mask.
            Assert.AreEqual(testEntity.DataBlobMask, EntityManager.BlankDataBlobMask());

            // Now try to remove the entity. Again.
            Assert.Catch<InvalidOperationException>(testEntity.Destroy);
        }

        [Test]
        public void RemoveDataBlobs()
        {
            // a little setup:
            Entity testEntity = Entity.Create(_game.GlobalManager);
            testEntity.SetDataBlob(new ColonyDB(_pop1));

            Assert.IsTrue(testEntity.GetDataBlob<ColonyDB>() != null);  // check that it has the data blob
            testEntity.RemoveDataBlob<ColonyDB>();                     // Remove a data blob
            Assert.IsTrue(testEntity.GetDataBlob<ColonyDB>() == null); // now check that it doesn't

            // now lets try remove it again:
            Assert.Catch<InvalidOperationException>(testEntity.RemoveDataBlob<ColonyDB>);

            // cannot remove baseDataBlobs, invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                testEntity.RemoveDataBlob<BaseDataBlob>();  
            });


            // reset:
            testEntity.SetDataBlob(new ColonyDB(_pop1));
            int typeIndex = EntityManager.GetTypeIndex<ColonyDB>();

            Assert.IsTrue(testEntity.GetDataBlob<ColonyDB>() != null);  // check that it has the data blob
            testEntity.RemoveDataBlob(typeIndex);              // Remove a data blob
            Assert.IsTrue(testEntity.GetDataBlob<ColonyDB>() == null); // now check that it doesn't

            // now lets try remove it again:
            Assert.Catch<InvalidOperationException>(() => testEntity.RemoveDataBlob(typeIndex));

            // and an invalid typeIndex:
            Assert.Catch(typeof(ArgumentException), () => testEntity.RemoveDataBlob(-42));

            // now lets try an invalid entity:
            testEntity.Destroy();
            Assert.Catch<InvalidOperationException>(() => testEntity.RemoveDataBlob(typeIndex));

        }

        [Test]
        public void EntityLookup()
        {
            PopulateEntityManager();

            // Find all entities with a specific DataBlob.
            List<Entity> entities = _game.GlobalManager.GetAllEntitiesWithDataBlob<ColonyDB>(_smAuthToken);
            Assert.AreEqual(2, entities.Count);

            // again, but look for a datablob that no entity has:
            entities = _game.GlobalManager.GetAllEntitiesWithDataBlob<AtmosphereDB>(_smAuthToken);
            Assert.AreEqual(0, entities.Count);

            // check with invalid data blob type:
            Assert.Catch(typeof(KeyNotFoundException), () => _game.GlobalManager.GetAllEntitiesWithDataBlob<BaseDataBlob>(_smAuthToken));

            // now lets lookup using a mask:
            ComparableBitArray dataBlobMask = EntityManager.BlankDataBlobMask();
            dataBlobMask.Set(EntityManager.GetTypeIndex<ColonyDB>(), true);
            dataBlobMask.Set(EntityManager.GetTypeIndex<OrbitDB>(), true);
            entities = _game.GlobalManager.GetAllEntitiesWithDataBlobs(_smAuthToken, dataBlobMask);
            Assert.AreEqual(2, entities.Count);

            // and with a mask that will not match any entities:
            dataBlobMask.Set(EntityManager.GetTypeIndex<AtmosphereDB>(), true);
            entities = _game.GlobalManager.GetAllEntitiesWithDataBlobs(_smAuthToken, dataBlobMask);
            Assert.AreEqual(0, entities.Count);

            // and an empty mask:
            dataBlobMask = EntityManager.BlankDataBlobMask();
            entities = _game.GlobalManager.GetAllEntitiesWithDataBlobs(_smAuthToken, dataBlobMask);
            Assert.AreEqual(3, entities.Count); // this is counter intuitive... but it is what happens.

            // test bad mask:
            ComparableBitArray badMask = new ComparableBitArray(4242); // use a big number so we never rach that many data blobs.
            Assert.Catch(typeof(ArgumentException), () => _game.GlobalManager.GetAllEntitiesWithDataBlobs(_smAuthToken, badMask));

            Assert.Catch(typeof(ArgumentNullException), () => _game.GlobalManager.GetAllEntitiesWithDataBlobs(_smAuthToken, null));


            // now lets just get the one entity:
            Entity testEntity = _game.GlobalManager.GetFirstEntityWithDataBlob<ColonyDB>(_smAuthToken);
            Assert.IsTrue(testEntity.IsValid);

            // lookup an entity that does not exist:
            testEntity = _game.GlobalManager.GetFirstEntityWithDataBlob<AtmosphereDB>(_smAuthToken);
            Assert.IsFalse(testEntity.IsValid);

            // try again with incorrect type:
            Assert.Catch(typeof(KeyNotFoundException), () =>
            {
                _game.GlobalManager.GetFirstEntityWithDataBlob<BaseDataBlob>(_smAuthToken);
            });


            // now lets just get the one entity, but use a different function to do it:
            int type = EntityManager.GetTypeIndex<ColonyDB>();
            testEntity = _game.GlobalManager.GetFirstEntityWithDataBlob(_smAuthToken, type);
            Assert.IsTrue(testEntity.IsValid);

            // lookup an entity that does not exist:
            type = EntityManager.GetTypeIndex<AtmosphereDB>();
            testEntity = _game.GlobalManager.GetFirstEntityWithDataBlob(_smAuthToken, type);
            Assert.IsFalse(testEntity.IsValid);

            // try again with incorrect type index:
            Assert.AreEqual(Entity.InvalidEntity, _game.GlobalManager.GetFirstEntityWithDataBlob(_smAuthToken, 4242));
        }

        [Test]
        public void EntityGuid()
        {
            Entity foundEntity;
            Entity testEntity = Entity.Create(_game.GlobalManager);

            Assert.IsTrue(testEntity.IsValid);
            // Check Guid local lookup.
            Assert.IsTrue(_game.GlobalManager.TryGetEntityByGuid(testEntity.Guid, out foundEntity));
            Assert.IsTrue(testEntity == foundEntity);
            
            // Check Guid global lookup.
            Assert.IsTrue(_game.GlobalManager.FindEntityByGuid(testEntity.Guid, out foundEntity));
            Assert.AreEqual(testEntity, foundEntity);

            // and a removed entity:
            testEntity.Destroy();
            Assert.IsFalse(testEntity.IsValid);

            // Check bad Guid lookups.
            Assert.IsFalse(_game.GlobalManager.TryGetEntityByGuid(Guid.Empty, out foundEntity));
            Assert.IsFalse(_game.GlobalManager.FindEntityByGuid(Guid.Empty, out foundEntity));
        }

        [Test]
        public void EntityTransfer()
        {
            EntityManager manager2 = _game.GetSystems(_smAuthToken).First().SystemManager;

            PopulateEntityManager();

            // Get an entity from the manager.
            Entity testEntity = _game.GlobalManager.GetFirstEntityWithDataBlob<OrbitDB>(_smAuthToken);
            // Ensure we got a valid entity.
            Assert.IsTrue(testEntity.IsValid);
            // Store it's datablobs for later.
            ReadOnlyCollection<BaseDataBlob> testEntityDataBlobs = testEntity.DataBlobs;
            
            // Store the current GUID.
            Guid entityGuid = testEntity.Guid;

            // Try to transfer to a null Manager.
            Assert.Catch<ArgumentNullException>(() => testEntity.Transfer(null));

            // Transfer the entity to a Entity.CreateManager
            testEntity.Transfer(manager2);

            // Ensure the original manager no longer has the entity.
            Entity foundEntity;
            Assert.IsFalse(_game.GlobalManager.TryGetEntityByGuid(entityGuid, out foundEntity));

            // Ensure the new manager has the entity.
            Assert.IsTrue(testEntity.Manager == manager2);
            Assert.IsTrue(manager2.TryGetEntityByGuid(entityGuid, out foundEntity));
            Assert.AreSame(testEntity, foundEntity);

            // Get the transferredEntity's datablobs.
            ReadOnlyCollection<BaseDataBlob> transferredEntityDataBlobs = testEntity.DataBlobs;
            
            // Compare the old datablobs with the new datablobs.
            foreach (BaseDataBlob testEntityDataBlob in testEntityDataBlobs)
            {
                bool matchFound = false;
                foreach (BaseDataBlob transferredDataBlob in transferredEntityDataBlobs)
                {
                    if (ReferenceEquals(testEntityDataBlob, transferredDataBlob))
                    {
                        matchFound = true;
                        break;
                    }
                }
                Assert.IsTrue(matchFound);
            }

            // Try to transfer an invalid entity.
            testEntity.Destroy();
            Assert.Catch<InvalidOperationException>(() => testEntity.Transfer(_game.GlobalManager));

        }

        [Test]
        public void TypeIndexTest()
        {
            int typeIndex;
            Assert.Catch<ArgumentNullException>(() => EntityManager.TryGetTypeIndex(null, out typeIndex));
            Assert.Catch<KeyNotFoundException>(() => EntityManager.GetTypeIndex<BaseDataBlob>());

            Assert.IsTrue(EntityManager.TryGetTypeIndex(typeof(OrbitDB), out typeIndex));
            Assert.AreEqual(EntityManager.GetTypeIndex<OrbitDB>(), typeIndex);
        }

        #region Extra Init Stuff

        /// <summary>
        /// This functions creates 3 entities with a total of 5 data blobs (3 orbits and 2 populations).
        /// </summary>
        /// <returns>It returns a reference to the first entity (containing 1 orbit and 1 pop)</returns>
        private Entity PopulateEntityManager()
        {
            // Clear out any previous test results.
            _game.GlobalManager.Clear();

            // Create an entity with individual DataBlobs.
            Entity testEntity = Entity.Create(_game.GlobalManager);
            testEntity.SetDataBlob(new OrbitDB());
            testEntity.SetDataBlob(new ColonyDB(_pop1));

            // Create an entity with a DataBlobList.
            var dataBlobs = new List<BaseDataBlob> { new OrbitDB() };
            Entity.Create(_game.GlobalManager, dataBlobs);

            // Create one more, just for kicks.
            dataBlobs = new List<BaseDataBlob> { new OrbitDB(), new ColonyDB(_pop2) };
            Entity.Create(_game.GlobalManager, dataBlobs);

            return testEntity;
        }

        #endregion

    }
}
