﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulsar4X.ECSLib
{
    public static class ComponentInstanceFactory
    {

        internal static Entity NewInstanceFromDesignEntity(Entity design, Guid factionID, EntityManager manager)
        {

            List<BaseDataBlob> blobs = new List<BaseDataBlob>();
            ComponentInstanceInfoDB info = new ComponentInstanceInfoDB(design);
            NameDB name = new NameDB(design.GetDataBlob<NameDB>().DefaultName);
            name.SetName(factionID, design.GetDataBlob<NameDB>().GetName(factionID));

            blobs.Add(info);
            blobs.Add(name);
            blobs.Add(new DesignInfoDB(design));
            // Added because each component instance needs its own copy of this datablob

            //Components have a mass and volume.
            MassVolumeDB mvDB = (MassVolumeDB)design.GetDataBlob<MassVolumeDB>().Clone();
            blobs.Add(mvDB);

            Entity newInstance = new Entity(manager, factionID, blobs);
            return newInstance;
        }
    }



}
