using System;
using Pulsar4X.Vectors;

namespace Pulsar4X.ECSLib
{

    public class ChangeCurrentOrbitCommand : EntityCommand
    {
        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        NewtonionMoveDB _db;

        public static void CreateCommand(Game game, Entity faction, Entity orderEntity, DateTime actionDateTime, Vector2 expendDeltaV_AU)
        {
            var cmd = new ChangeCurrentOrbitCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,

            };

            cmd._db = new NewtonionMoveDB();
            cmd._db.ActionOnDateTime = actionDateTime;
            cmd._db.DeltaVToExpend_AU = expendDeltaV_AU;
            


            game.OrderHandler.HandleOrder(cmd);
        }

        internal override void ActionCommand(Game game)
        {
            if (!IsRunning)
            {
                Entity parentEntity = EntityCommanding.GetDataBlob<OrbitDB>().Parent;
                Vector2 newVector = OrbitProcessor.InstantaneousOrbitalVelocityVector(EntityCommanding.GetDataBlob<OrbitDB>(), _db.ActionOnDateTime);
                newVector += _db.DeltaVToExpend_AU;
                var spdmps = Distance.AuToMt( newVector.Length());
                Vector3 newVector3d = new Vector3(newVector.X, newVector.Y,0);
                OrbitDB newOrbit = OrbitDB.FromVector(parentEntity, EntityCommanding, newVector3d, _db.ActionOnDateTime);
                /*
                if (newOrbit.Periapsis > targetSOI)
                {
                    //TODO: find who's SOI we're currently in and create an orbit for that;
                }
                if (newOrbit.Apoapsis > targetSOI)
                {
                    //TODO: change orbit to new parent at SOI change
                }
                */


                EntityCommanding.SetDataBlob(newOrbit);
                newOrbit.SetParent(parentEntity);

            }
        }

        internal override bool IsFinished()
        {
            if (IsRunning)
                return true;
            else
                return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
                return true;
            else
                return false;
        }
    }

    public class TransitToOrbitCommand : EntityCommand
    {
        internal override int ActionLanes => 1;
        internal override bool IsBlocking => true;

        Entity _factionEntity;
        Entity _entityCommanding;
        internal override Entity EntityCommanding { get { return _entityCommanding; } }

        public Guid TargetEntityGuid { get; set; }

        private Entity _targetEntity;
        public Vector3 TargetOffsetPosition_AU;
        public DateTime TransitStartDateTime;
        public Vector3 ExpendDeltaV;

        TranslateMoveDB _db;


        /// <summary>
        /// Creates the transit cmd.
        /// </summary>
        /// <param name="game">Game.</param>
        /// <param name="faction">Faction.</param>
        /// <param name="orderEntity">Order entity.</param>
        /// <param name="targetEntity">Target entity.</param>
        /// <param name="targetOffsetPos_AU">Target offset position in au.</param>
        /// <param name="transitStartDatetime">Transit start datetime.</param>
        /// <param name="expendDeltaV_AU">Amount of DV to expend to change the orbit in AU/s</param>
        public static void CreateTransitCmd(Game game, Entity faction, Entity orderEntity, Entity targetEntity, Vector3 targetOffsetPos_AU, DateTime transitStartDatetime, Vector3 expendDeltaV_AU)
        {
            var cmd = new TransitToOrbitCommand()
            {
                RequestingFactionGuid = faction.Guid,
                EntityCommandingGuid = orderEntity.Guid,
                CreatedDate = orderEntity.Manager.ManagerSubpulses.StarSysDateTime,
                TargetEntityGuid = targetEntity.Guid,
                TargetOffsetPosition_AU = targetOffsetPos_AU,
                TransitStartDateTime = transitStartDatetime,
                ExpendDeltaV = expendDeltaV_AU,
            };
            game.OrderHandler.HandleOrder(cmd);
        }



        internal override void ActionCommand(Game game)
        {
            
            if (!IsRunning)
            {
                

                (Vector3 pos, DateTime eti) targetIntercept = InterceptCalcs.GetInterceptPosition(_entityCommanding, _targetEntity.GetDataBlob<OrbitDB>(), TransitStartDateTime, TargetOffsetPosition_AU);
                OrbitDB orbitDB = _entityCommanding.GetDataBlob<OrbitDB>();
                Vector3 currentPos = OrbitProcessor.GetAbsolutePosition_AU(orbitDB, TransitStartDateTime);
                var ralPos = OrbitProcessor.GetPosition_AU(orbitDB, TransitStartDateTime);
                var sgp = OrbitMath.CalculateStandardGravityParameter(_entityCommanding.GetDataBlob<MassVolumeDB>().Mass, orbitDB.Parent.GetDataBlob<MassVolumeDB>().Mass);

                Vector2 currentVec = OrbitProcessor.GetOrbitalVector(orbitDB, TransitStartDateTime);
                _db = new TranslateMoveDB(targetIntercept.pos);
                _db.TranslateRalitiveExit_AU = TargetOffsetPosition_AU;
                _db.EntryDateTime = TransitStartDateTime;
                _db.PredictedExitTime = targetIntercept.eti;
                _db.TranslateEntryPoint_AU = currentPos;
                _db.SavedNewtonionVector_AU = currentVec;

                _db.ExpendDeltaV_AU = ExpendDeltaV;
                if (_targetEntity.HasDataBlob<SensorInfoDB>())
                {
                    _db.TargetEntity = _targetEntity.GetDataBlob<SensorInfoDB>().DetectedEntity;
                }
                else
                    _db.TargetEntity = _targetEntity;
                if (EntityCommanding.HasDataBlob<OrbitDB>())
                    EntityCommanding.RemoveDataBlob<OrbitDB>();
                EntityCommanding.SetDataBlob(_db);
                TranslateMoveProcessor.StartNonNewtTranslation(EntityCommanding);
                IsRunning = true;


                var distance = (currentPos - targetIntercept.Item1).Length();
                var distancekm = Distance.AuToKm(distance);

                var time = targetIntercept.Item2 - TransitStartDateTime;

                double spd = _entityCommanding.GetDataBlob<PropulsionAbilityDB>().MaximumSpeed_MS;
                spd = Distance.MToAU(spd);
                var distb = spd * time.TotalSeconds;
                var distbKM = Distance.AuToKm(distb);

                var dif = distancekm - distbKM;
                //Assert.AreEqual(distancekm, distbKM);
            }

        }

        internal override bool IsFinished()
        {
            if (_db.IsAtTarget)
                return true;
            return false;
        }

        internal override bool IsValidCommand(Game game)
        {
            if (CommandHelpers.IsCommandValid(game.GlobalManager, RequestingFactionGuid, EntityCommandingGuid, out _factionEntity, out _entityCommanding))
            {
                if (game.GlobalManager.FindEntityByGuid(TargetEntityGuid, out _targetEntity))
                {
                    return true;
                }
            }
            return false;
        }
    }
}