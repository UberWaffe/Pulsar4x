﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System;

namespace Pulsar4X.ECSLib
{
    [DebuggerDisplay("{" + nameof(DefaultName) + "}")]
    public class NameDB : BaseDataBlob, ISensorCloneMethod
    {

        /// <summary>
        /// Each faction can have a different name for whatever entity has this blob.
        /// </summary>
        [JsonProperty]
        private readonly Dictionary<Entity, string> _names = new Dictionary<Entity, string>();

        [PublicAPI]
        public string DefaultName => _names[Entity.InvalidEntity];

        public NameDB() { }

        public NameDB(string defaultName)
        {
            _names.Add(Entity.InvalidEntity, defaultName);
        }

        #region Cloning Interface.

        public NameDB(NameDB nameDB)
        {
            _names = new Dictionary<Entity, string>(nameDB._names);
        }

        public override object Clone()
        {
            return new NameDB(this);
        }

        #endregion

        [PublicAPI]
        public string GetName(Entity requestingFaction)
        {
            string name;
            if (!_names.TryGetValue(requestingFaction, out name))
            {
                // Entry not found for the specific entity.
                // Return guid instead. TODO: call an automatic naming function
                name = OwningEntity.Guid.ToString();
                SetName(requestingFaction, name);

            }
            return name;
        }

        public string GetName(Entity requestingFaction, Game game, AuthenticationToken auth)
        {

            if (game.GetPlayerForToken(auth).AccessRoles[requestingFaction] < AccessRole.Intelligence)
                requestingFaction = Entity.InvalidEntity;
            return GetName(requestingFaction);
        }

        [PublicAPI]
        public void SetName(Entity requestingFaction, string specifiedName)
        {
            if (_names.ContainsKey(requestingFaction))
            {
                _names[requestingFaction] = specifiedName;
            }
            else
            {
                _names.Add(requestingFaction, specifiedName);
            }
        }

        public BaseDataBlob Clone(SensorInfoDB sensorInfo)
        {
            return new NameDB(this, sensorInfo);
        }

        NameDB(NameDB db, SensorInfoDB sensorInfo)
        {            
            _names.Add(Entity.InvalidEntity, db.DefaultName);
            _names.Add(sensorInfo.Faction, db.GetName(sensorInfo.Faction));
        }
    }
}