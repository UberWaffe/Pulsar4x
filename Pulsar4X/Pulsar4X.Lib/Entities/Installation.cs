﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Pulsar4X.Entities;
using Pulsar4X.Entities.Components;

namespace Pulsar4X.Entities
{
    public class Installation : GameEntity
    {
        public enum InstallationType
        {
            AutomatedMine,
            CivilianMiningComplex,
            CommercialShipyard,
            ConstructionFactory,
            ConventionalIndustry,
            ConvertCIToConstructionFactory,
            ConvertCIToFighterFactory,
            ConvertCIToFuelRefinery,
            ConvertCIToMine,
            ConvertCIToOrdnanceFactory,
            ConvertMineToAutomated,
            DeepSpaceTrackingStation,
            FighterFactory,
            FinancialCentre,
            FuelRefinery,
            GeneticModificationCentre,
            GroundForceTrainingFacility,
            Infrastructure,
            MaintenanceFacility,
            MassDriver,
            MilitaryAcademy,
            Mine,
            NavalShipyardComplex,
            OrdnanceFactory,
            ResearchLab,
            SectorCommand,
            Spaceport,
            TerraformingInstallation,
            InstallationCount
        }

        public class ShipyardInformation : GameEntity
        {
            public class ShipyardTask
            {
                
                /// <summary>
                /// What is being done to this ship?
                /// </summary>
                public Constants.ShipyardInfo.Task CurrentTask { get; set; }

                /// <summary>
                /// What Ship is being repaired/refitted/scrapped. Construction makes a new ship that does not exist yet.
                /// </summary>
                public ShipTN CurrentShip { get; set; }

                /// <summary>
                /// How close to completion is this task?
                /// </summary>
                public decimal Progress { get; set; }

                /// <summary>
                /// What TG will this ship be placed into when finished. aside from scrapping operations of course.
                /// </summary>
                public TaskGroupTN AssignedTaskGroup { get; set; }

                /// <summary>
                /// How much will this task cost?
                /// </summary>
                public decimal Cost { get; set; }

                /// <summary>
                /// Estimate of when this task will be completed.
                /// </summary>
                public DateTime CompletionDate { get; set; }

                /// <summary>
                /// What is the Annual Build Rate for this ship.
                /// </summary>
                public int ABR { get; set; }

                /// <summary>
                /// How should tasks be done in the event of a resource shortage.
                /// </summary>
                public int Priority { get; set; }

                /// <summary>
                /// Is this task paused or not?
                /// </summary>
                public bool Paused { get; set; }

                /// <summary>
                /// The mineral cost of this task.
                /// </summary>
                private decimal[] m_aiMinerialsCost;
                public decimal[] minerialsCost
                {
                    get
                    {
                        return m_aiMinerialsCost;
                    }
                    set
                    {
                        m_aiMinerialsCost = value;
                    }
                }

                /// <summary>
                /// If this is a new ship construction, what ship do we want to construct?
                /// If this is a refit task, what are we refitting the ship to?
                /// </summary>
                public ShipClassTN ConstructRefitTarget { get; set;}

                /// <summary>
                /// What is the name of this ship going to be if this is a newly constructed ship?
                /// </summary>
                public String Title { get; set; }


                /// <summary>
                /// Constructor for task.
                /// ABR = Normal shipbuilding rate x (1+(((Class Size / 100) - 1)/2)) 
                /// Scrap Formula is ((0.25 * ShipCost) / ABR) * Year = days involved.
                /// </summary>
                /// <param name="Ship">Ship to repair/refit/scrap. build will be handled elsewhere.</param>
                /// <param name="TargetTG">Target TG which the ship will be put back in to, or in the case of a scrap operation taken from.</param>
                /// <param name="PopulationBuildRate">What is the currently selected shipyard capable of building? population and faction should factor into this number.</param>
                /// <param name="ConstructOrRefitTarget">If a new ship is being built, or an old ship is being refitted, what target design are we interested in?</param>
                public ShipyardTask(ShipTN Ship, Constants.ShipyardInfo.Task TaskToPerform, TaskGroupTN TargetTG, int PopulationBuildRate, String TitleToUse, ShipClassTN ConstructOrRefitTarget=null)
                {
                    m_aiMinerialsCost = new decimal[(int)Constants.Minerals.MinerialNames.MinerialCount];

                    CurrentShip = Ship;
                    CurrentTask = TaskToPerform;
                    Progress = 0.0m;
                    Priority = 0;
                    Paused = false;
                    ConstructRefitTarget = ConstructOrRefitTarget;

                    ABR = 1;

                    AssignedTaskGroup = TargetTG;

                    if(AssignedTaskGroup != null)
                        AssignedTaskGroup.IsInShipyard = true;

                    Cost = 0.0m;

                    /// <summary>
                    /// This is only relevant for new ship construction.
                    /// </summary>
                    Title = TitleToUse;

                    switch (TaskToPerform)
                    {
                        case Constants.ShipyardInfo.Task.Construction:
                            ABR = (int)Math.Round(PopulationBuildRate * (1.0f + (((ConstructRefitTarget.SizeHS / 100.0f) - 1.0f) / 2.0f)));
                            Cost = ConstructRefitTarget.BuildPointCost;
                            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                            {
                                m_aiMinerialsCost[mineralIterator] = ConstructRefitTarget.minerialsCost[mineralIterator];
                            }        
                            break;
                        case Constants.ShipyardInfo.Task.Repair:
                            ABR = (int)Math.Round(PopulationBuildRate * (1.0f + (((Ship.ShipClass.SizeHS / 100.0f) - 1.0f) / 2.0f)));
                            /// <summary>
                            /// Repair will just cost money. Two reasons. 1st: Laziness. I'd have to get all the component costs as well.
                            /// 2nd:Ships require continuous resource outlays via routine maintenance, that should and will cover the actual cost of repairing the ship above and beyond the money required.
                            /// </summary>
                            for(int componentIterator = 0; componentIterator < Ship.DestroyedComponents.Count; componentIterator++)
                            {
                                ushort ID = Ship.DestroyedComponents[componentIterator];
                                ComponentTypeTN CType = Ship.DestroyedComponentsType[componentIterator];
                                Cost = Cost + Ship.GetDamagedComponentsRepairCost(ID,CType);
                            }

                            if(Ship.ShipArmor.isDamaged == true)
                            {
                                int totalDamage = 0;
                                int max = Ship.ShipArmor.armorDef.cNum * Ship.ShipArmor.armorDef.depth;
                                foreach (KeyValuePair<ushort, ushort> pair in Ship.ShipArmor.armorDamage)
                                {
                                    totalDamage = totalDamage + pair.Value;
                                }

                                float damageFraction = ((float)totalDamage / (float)max);

                                Cost = Cost + ((decimal)damageFraction * Ship.ShipArmor.armorDef.cost);
                            }

                            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                            {
                                m_aiMinerialsCost[mineralIterator] = 0.0m;
                            }
                               
                            break;
                        case Constants.ShipyardInfo.Task.Refit:
                            ABR = (int)Math.Round(PopulationBuildRate * (1.0f + (((ConstructRefitTarget.SizeHS / 100.0f) - 1.0f) / 2.0f)));
                            Cost = ConstructRefitTarget.BuildPointCost;
                            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                            {
                                m_aiMinerialsCost[mineralIterator] = ConstructRefitTarget.minerialsCost[mineralIterator];
                            }
                            break;
                        case Constants.ShipyardInfo.Task.Scrap:
                            ABR = (int)Math.Round(PopulationBuildRate * (1.0f + (((Ship.ShipClass.SizeHS / 100.0f) - 1.0f) / 2.0f)));
                            Cost = Ship.ShipClass.BuildPointCost * -0.25m;
                            for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                            {
                                m_aiMinerialsCost[mineralIterator] = Ship.ShipClass.minerialsCost[mineralIterator] * -0.25m;
                            }
                            break;
                    }
                    /// <summary>
                    /// How long will this retool take?
                    /// </summary>
                    float DaysInYear = (float)Constants.TimeInSeconds.RealYear / (float)Constants.TimeInSeconds.Day; 
                    float YearsOfProduction = (float)Math.Abs(Cost) / (float)ABR;
                    DateTime EstTime = GameState.Instance.GameDateTime;
                    if (YearsOfProduction < Constants.Colony.TimerYearMax)
                    {
                        int TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                        TimeSpan TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                        EstTime = EstTime.Add(TS);
                    }
                    CompletionDate = EstTime;
                }

                /// <summary>
                /// On tech upgrades the ABR for the task will go up, and completion time will go down.
                /// Small ships in big yards get no bonus to their construction size, but larger ships should build faster due to the larger scale of the yard with more space and more people working on it.
                /// </summary>
                /// <param name="PopulationBaseBuildRate">The Faction's ship build rate</param>
                public void UpdateABR(int PopulationBaseBuildRate)
                {
#warning ABR should be updated on new technology discoveries.
                    switch (CurrentTask)
                    {
                        case Constants.ShipyardInfo.Task.Construction:
                        case Constants.ShipyardInfo.Task.Refit:
                            ABR = (int)Math.Round(PopulationBaseBuildRate * (1.0f + (((ConstructRefitTarget.SizeHS / 100.0f) - 1.0f) / 2.0f)));
                            break;
                        case Constants.ShipyardInfo.Task.Repair:
                        case Constants.ShipyardInfo.Task.Scrap:
                            ABR = (int)Math.Round(PopulationBaseBuildRate * (1.0f + (((CurrentShip.ShipClass.SizeHS / 100.0f) - 1.0f) / 2.0f)));
                            break;
                    }
                }

                /// <summary>
                /// Suspend this task.
                /// </summary>
                public void Pause()
                {
                    Paused = true;
                }

                /// <summary>
                /// Continue with this task.
                /// </summary>
                public void UnPause()
                {
                    Paused = false;
                }

                /// <summary>
                /// Priority being -1 as per Pause() means that this task should not be in production.
                /// </summary>
                /// <returns>True if paused, false if not paused.</returns>
                public bool IsPaused()
                {
                    return Paused;
                }

                /// <summary>
                /// Increment priority by 1.
                /// </summary>
                public void IncrementPriority()
                {
                    Priority = Priority + 1;
                }

                /// <summary>
                /// reduce the priority of this ship build. 0 is the minimum priority short of pausing construction.
                /// </summary>
                public void DecrementPriority()
                {
                    if(Priority > 0)
                        Priority = Priority - 1;
                }
            }

            public class ShipyardActivity
            {
                /// <summary>
                /// What task is this shipyard currently set to accomplish.
                /// </summary>
                public Constants.ShipyardInfo.ShipyardActivity Activity { get; set; }

                /// <summary>
                /// If the current activity is a retool, how far along are we?
                /// </summary>
                public ShipClassTN TargetOfRetool { get; set; }

                /// <summary>
                /// How far along with our current activity is this shipyard?
                /// </summary>
                public decimal Progress { get; set; }

                /// <summary>
                /// How much will this activity cost to perform?
                /// </summary>
                public decimal CostOfActivity { get; set; }

                /// <summary>
                /// The mineral cost of this activity.
                /// </summary>
                private decimal[] m_aiMinerialsCost;
                public decimal[] minerialsCost
                {
                    get
                    {
                        return m_aiMinerialsCost;
                    }
                    set
                    {
                        m_aiMinerialsCost = value;
                    }
                }

                /// <summary>
                /// Estimate of when this complex will finish its current task.
                /// </summary>
                public DateTime CompletionDate { get; set; }

                /// <summary>
                /// Expand capacity until this limit is reached if the appropriate activity is set.
                /// </summary>
                public int CapExpansionLimit { get; set; }

                /// <summary>
                /// Is this build activity paused or under construction?
                /// </summary>
                public bool Paused { get; set; }


                /// <summary>
                /// Default Constructor.
                /// </summary>
                public ShipyardActivity()
                {
                    m_aiMinerialsCost = new decimal[(int)Constants.Minerals.MinerialNames.MinerialCount];
                    Activity = Constants.ShipyardInfo.ShipyardActivity.NoActivity;
                    Progress = 0.0m;
                    CostOfActivity = 0.0m;

                    TargetOfRetool = null;
                    CapExpansionLimit = -1;

                    Paused = false;
                }

                /// <summary>
                /// Constructor for Cap expansion orders. These are open ended.
                /// </summary>
                /// <param name="NewActivity">new activity to set.</param>
                public ShipyardActivity(Constants.ShipyardInfo.ShipyardActivity NewActivity)
                {
                    m_aiMinerialsCost = new decimal[(int)Constants.Minerals.MinerialNames.MinerialCount];
                    Activity = NewActivity;

                    TargetOfRetool = null;
                    CapExpansionLimit = -1;

                    Paused = false;
                }

                /// <summary>
                /// Constructor for Activity.
                /// </summary>
                /// <param name="NewActivity">New activity for this shipyard.</param>
                /// <param name="Cost">Cost of the activity. This will depend on the shipyard, so can't be calculated here in all cases.</param>
                /// <param name="minCost">Mineral cost of activity.</param>
                /// <param name="CompDate">Estimated completion date.</param>
                /// <param name="RetoolTarget">Shipclass to retool this shipyard to.</param>
                /// <param name="CapLimit">Expand this shipard until CapLimit tons. if -1 then this should not be cap expansion until X tons</param>
                public ShipyardActivity(Constants.ShipyardInfo.ShipyardActivity NewActivity, decimal Cost, decimal [] minCost, DateTime CompDate, ShipClassTN RetoolTarget=null)
                {
                    m_aiMinerialsCost = new decimal[(int)Constants.Minerals.MinerialNames.MinerialCount];
                    Activity = NewActivity;
                    Progress = 0.0m;
                    TargetOfRetool = null;
                    CapExpansionLimit = -1;

                    CostOfActivity = Cost;

                    CompletionDate = CompDate;

                    for (int mineralIterator = 0; mineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; mineralIterator++)
                    {
                        m_aiMinerialsCost[mineralIterator] = minCost[mineralIterator];
                    }

                    if (RetoolTarget != null && NewActivity == Constants.ShipyardInfo.ShipyardActivity.Retool)
                    {
                        TargetOfRetool = RetoolTarget;
                    }

                    Paused = false;
                }
            }

            /// <summary>
            /// Shipyard tonnage.
            /// </summary>
            public int Tonnage { get; set; }

            /// <summary>
            /// Shipyard slipway count.
            /// </summary>
            public int Slipways { get; set; }

            /// <summary>
            /// What ships are being built at this shipyard. This should never exceed slipways.
            /// </summary>
            public BindingList<ShipyardTask> BuildingShips { get; set; }

            /// <summary>
            /// What if any modifications is this shipyard performing?
            /// </summary>
            public ShipyardActivity CurrentActivity { get; set; }

            /// <summary>
            /// What shipclass are we set to build?
            /// </summary>
            public ShipClassTN AssignedClass { get; set; }

            /// <summary>
            /// How quickly this shipyard complex can complete activities and construct ships. This may be modified by technology and governor bonuses.
            /// ModRate = Base(240) * ((Size-1000)/1000) * 40
            /// SY 560 is 1.4 * 400
            /// For Shipyard modification: AnnualSYProd = (ModRate / 200) * 834
            /// </summary>
            public int ModRate { get; set; }

            /// <summary>
            /// What type of shipyard is this?
            /// </summary>
            public Constants.ShipyardInfo.SYType ShipyardType { get; set; }

            /// <summary>
            /// Constructor for shipyard information
            /// </summary>
            /// <param name="Type"></param>
            public ShipyardInformation(Faction CurrentFaction, Constants.ShipyardInfo.SYType Type, int Number)
            {
                switch (Type)
                {
                    case Constants.ShipyardInfo.SYType.Commercial:
                        Name = "Commercial Yard #" + Number.ToString();
                        Tonnage = (Constants.ShipyardInfo.BaseShipyardTonnage * Constants.ShipyardInfo.NavalToCommercialRatio);
                        break;
                    case Constants.ShipyardInfo.SYType.Naval:
                        Name = "Naval Yard #" + Number.ToString();
                        Tonnage = Constants.ShipyardInfo.BaseShipyardTonnage;
                        break;
                    default:
                        Name = "Unknown Shipyard Type #" + Number.ToString();
                        break;
                }
                
                ShipyardType = Type;
                AssignedClass = null;

                BuildingShips = new BindingList<ShipyardTask>();
                CurrentActivity = new ShipyardActivity();

                Slipways = 1;
                AssignedClass = null;

                UpdateModRate(CurrentFaction);
            }

            /// <summary>
            /// Set the activity that this shipyard complex will undertake.
            /// </summary>
            /// <param name="NewActivity">New Activity to perform.</param>
            /// <param name="RetoolTarget">Retool target if any</param>
            /// <param name="CapLimit">Capacity Expansion Limit if any.</param>
            public void SetShipyardActivity(Faction CurrentFaction, Constants.ShipyardInfo.ShipyardActivity NewActivity, ShipClassTN RetoolTarget = null, int CapLimit = -1)
            {
                decimal[] mCost = new decimal[(int)Constants.Minerals.MinerialNames.MinerialCount];
                float DaysInYear = (float)Constants.TimeInSeconds.RealYear / (float)Constants.TimeInSeconds.Day;

                /// <summary>
                /// These are declared here since each switch case is considered to be on the same level of scope and just putting them in the first case doesn't seem quite right.
                /// </summary>
                float YearsOfProduction = 0.0f;
                int TimeToBuild = 0;
                DateTime EstTime;
                TimeSpan TS;

                /// <summary>
                /// If an order for adding a set amount of tons is canceled while in progress, credit the tons that were built to the shipyard.
                /// </summary>
                if(CurrentActivity.Progress != 0.0m && CurrentActivity.Activity != Constants.ShipyardInfo.ShipyardActivity.NoActivity)
                {
                    int TonsToAdd = 0;
                    switch (CurrentActivity.Activity)
                    {
                        case Constants.ShipyardInfo.ShipyardActivity.Add500Tons:
                            TonsToAdd = (int)Math.Round((float)CurrentActivity.Progress * 500.0f);
                            break;
                        case Constants.ShipyardInfo.ShipyardActivity.Add1000Tons:
                            TonsToAdd = (int)Math.Round((float)CurrentActivity.Progress * 1000.0f);
                            break;
                        case Constants.ShipyardInfo.ShipyardActivity.Add2000Tons:
                            TonsToAdd = (int)Math.Round((float)CurrentActivity.Progress * 2000.0f);
                            break;
                        case Constants.ShipyardInfo.ShipyardActivity.Add5000Tons:
                            TonsToAdd = (int)Math.Round((float)CurrentActivity.Progress * 5000.0f);
                            break;
                        case Constants.ShipyardInfo.ShipyardActivity.Add10000Tons:
                            TonsToAdd = (int)Math.Round((float)CurrentActivity.Progress * 10000.0f);
                            break;
                    }

                    if (TonsToAdd != 0)
                    {
                        AddTonnage(CurrentFaction, Tonnage);
                    }
                }

                switch (NewActivity)
                {
                    case Constants.ShipyardInfo.ShipyardActivity.NoActivity:
                        CurrentActivity = new ShipyardActivity();
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.AddSlipway:
                        decimal TotalSlipwayCost = 0.0m;
                        for (int MineralIterator = 0; MineralIterator < Constants.Minerals.NO_OF_MINERIALS; MineralIterator++)
                        {
                            if (Constants.ShipyardInfo.MineralCostOfExpansion[MineralIterator] != 0)
                            {
                                int Adjustment = 1;
                                if (ShipyardType == Constants.ShipyardInfo.SYType.Naval)
                                    Adjustment = 10;

                                /// <summary>
                                /// Formula for tonnage expansion. This also appears in the constants file for now.
                                /// </summary>
                                decimal value = Constants.ShipyardInfo.MineralCostOfExpansion[MineralIterator] * (Tonnage / Constants.ShipyardInfo.TonnageDenominator) * Slipways * Adjustment;
                                mCost[MineralIterator] = value;
                                TotalSlipwayCost = TotalSlipwayCost + value;
                            }
                        }

                        YearsOfProduction = (float)TotalSlipwayCost / CalcAnnualSYProduction();
                        EstTime = GameState.Instance.GameDateTime;
                        if (YearsOfProduction < Constants.Colony.TimerYearMax)
                        {
                            TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                            TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                            EstTime = EstTime.Add(TS);
                        }

                        CurrentActivity = new ShipyardActivity(NewActivity, TotalSlipwayCost, mCost, EstTime);
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.Add500Tons:
                        SetExpansion(NewActivity, 500, mCost, DaysInYear);
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.Add1000Tons:
                        SetExpansion(NewActivity, 1000, mCost, DaysInYear);
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.Add2000Tons:
                        SetExpansion(NewActivity, 2000, mCost, DaysInYear);
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.Add5000Tons:
                        SetExpansion(NewActivity, 5000, mCost, DaysInYear);
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.Add10000Tons:
                        SetExpansion(NewActivity, 10000, mCost, DaysInYear);
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.Retool:
                        /// <summary>
                        /// One free retool. Hypothetically this shipyard was built with this shipclass in mind.
                        /// </summary>
                        if (AssignedClass == null && RetoolTarget != null)
                        {
                            AssignedClass = RetoolTarget;
                            if (AssignedClass.IsLocked == false)
                                AssignedClass.IsLocked = true;
                        }
                        /// <summary>
                        /// Lengthy retool process as the shipyard converts to build the new vessel.
                        /// </summary>
                        else if(AssignedClass != null && RetoolTarget != null)
                        {
                            /// <summary>
                            /// Caclulate the cost of this retool:
                            /// </summary>
                            decimal CostToRetool = 0.5m * RetoolTarget.BuildPointCost + ((0.25m * RetoolTarget.BuildPointCost) * Slipways);
                            mCost[(int)Constants.Minerals.MinerialNames.Duranium] = CostToRetool / 2.0m;
                            mCost[(int)Constants.Minerals.MinerialNames.Neutronium] = CostToRetool / 2.0m;

                            /// <summary>
                            /// How long will this retool take?
                            /// </summary>
                            YearsOfProduction = (float)CostToRetool / CalcAnnualSYProduction();
                            EstTime = GameState.Instance.GameDateTime;
                            if (YearsOfProduction < Constants.Colony.TimerYearMax)
                            {
                                TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                                TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                                EstTime = EstTime.Add(TS);
                            }
                            CurrentActivity = new ShipyardActivity(NewActivity, CostToRetool, mCost, EstTime, RetoolTarget);

                            if (RetoolTarget.IsLocked == false)
                                RetoolTarget.IsLocked = true;
                        }
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.CapExpansion:
                        CurrentActivity = new ShipyardActivity(NewActivity);
                        break;
                    case Constants.ShipyardInfo.ShipyardActivity.CapExpansionUntilX:
                        if (CapLimit > Tonnage)
                        {
                            SetExpansion(NewActivity, (CapLimit - Tonnage), mCost, DaysInYear);
                            CurrentActivity.CapExpansionLimit = CapLimit;
                        }
                        break;
                }
            }

            /// <summary>
            /// Get the annual production with regards to shipyard modification for the selected shipyard.
            /// </summary>
            /// <param name="SYInfo">Shipyard to calculate this for</param>
            /// <returns></returns>
            public float CalcAnnualSYProduction()
            {
#warning does not take technology or governor bonus into account.
                float AnnualSYProd = ((float)ModRate / (float)Constants.ShipyardInfo.BaseModRateDenominator) * (float)Constants.ShipyardInfo.BaseModProd;

                return AnnualSYProd;
            }

            /// <summary>
            /// Add tonnage and then update the mod rate.
            /// </summary>
            public void AddTonnage(Faction CurrentFaction, int TonsToAdd)
            {
                Tonnage = Tonnage + TonsToAdd;
                UpdateModRate(CurrentFaction);
            }

            /// <summary>
            /// Modrate should be updated whenever tonnage is added or when tech is advanced.
            /// </summary>
            /// <param name="CurrentFaction"></param>
            public void UpdateModRate(Faction CurrentFaction)
            {
#warning Modrate needs to be updated on tech advancement. also, does the governor have an effect on mod rate?
                int BuildTech = CurrentFaction.FactionTechLevel[(int)Faction.FactionTechnology.ShipProdRate];
                if (BuildTech > Constants.ShipyardInfo.MaxShipProductionRate)
                    BuildTech = Constants.ShipyardInfo.MaxShipProductionRate;

                float TechAdjustment = (float)Constants.ShipyardInfo.ShipProductionRate[BuildTech] / (float)Constants.ShipyardInfo.ShipProductionRate[0];

                ModRate = (int)Math.Round((Constants.ShipyardInfo.BaseModRate + ((((float)Tonnage - (float)Constants.ShipyardInfo.BaseShipyardTonnage) / (float)Constants.ShipyardInfo.BaseShipyardTonnage) *
                                          Constants.ShipyardInfo.ModRateTonnageMultiplier)) * TechAdjustment);
            }

            /// <summary>
            /// How many BP will this shipyard produce for building new ships?
            /// </summary>
            /// <param name="CurrentFaction">Current faction that owns this shipyard</param>
            /// <param name="CurrentPopulation">Current population this shipyard at.</param>
            /// <returns>The build rate for this shipyard.</returns>
            public int CalcShipBuildRate(Faction CurrentFaction, Population CurrentPopulation)
            {
#warning Does not take into effect governor bonus
                int BuildTech = CurrentFaction.FactionTechLevel[(int)Faction.FactionTechnology.ShipProdRate];
                if (BuildTech > Constants.ShipyardInfo.MaxShipProductionRate)
                    BuildTech = Constants.ShipyardInfo.MaxShipProductionRate;
                return Constants.ShipyardInfo.ShipProductionRate[BuildTech];
            }

            /// <summary>
            /// Helper function for repetitive code.
            /// </summary>
            /// <param name="NewActivity">Activity to set</param>
            /// <param name="tons">How many tons should the yard be expanded by?</param>
            /// <param name="mCost">Mineral cost variable that this function will fill out.</param>
            /// <param name="DaysInYear">this should probably be a constant somewhere.</param>
            private void SetExpansion(Constants.ShipyardInfo.ShipyardActivity NewActivity, int tons, decimal [] mCost, float DaysInYear)
            {
                decimal TotalCost = 0.0m;
                for (int MineralIterator = 0; MineralIterator < (int)Constants.Minerals.MinerialNames.MinerialCount; MineralIterator++)
                {

                    if (Constants.ShipyardInfo.MineralCostOfExpansion[MineralIterator] != 0)
                    {
                        int Adjustment = 1;
                        if (ShipyardType == Constants.ShipyardInfo.SYType.Naval)
                        {
                            Adjustment = Constants.ShipyardInfo.NavalToCommercialRatio;
                        }

                        /// <summary>
                        /// Formula for tonnage expansion. This also appears in the constants file for now.
                        /// </summary>
                        decimal value = Constants.ShipyardInfo.MineralCostOfExpansion[MineralIterator] * (tons / Constants.ShipyardInfo.TonnageDenominator) * Slipways * Adjustment;
                        mCost[MineralIterator] = value;
                        TotalCost = TotalCost + value;
                    }
                    else
                        mCost[MineralIterator] = 0.0m;
                }

                float YearsOfProduction = (float)TotalCost / CalcAnnualSYProduction();
                DateTime EstTime = GameState.Instance.GameDateTime;
                if (YearsOfProduction < Constants.Colony.TimerYearMax)
                {
                    int TimeToBuild = (int)Math.Floor(YearsOfProduction * DaysInYear);
                    EstTime = GameState.Instance.GameDateTime;
                    TimeSpan TS = new TimeSpan(TimeToBuild, 0, 0, 0);
                    EstTime = EstTime.Add(TS);
                }

                CurrentActivity = new ShipyardActivity(NewActivity, TotalCost, mCost, EstTime);
            }
        }

        public const int NO_OF_INSTALLATIONS = (int)InstallationType.InstallationCount;

        /// <summary>
        /// Which installation is this?
        /// </summary>
        public InstallationType Type { get; set; }

        /// <summary>
        /// amount of wealth and resource units to build.
        /// </summary>
        public int Cost { get; set; }

        /// <summary>
        /// Number of this installation that a colony has
        /// </summary>
        public float Number { get; set; }

        /// <summary>
        /// Size of this installation for cargo transfering.
        /// </summary>
        public int Mass { get; set; }

        /// <summary>
        /// What thermal signature does this installation have.
        /// </summary>
        public float ThermalSignature { get; set; }

        /// <summary>
        /// What EM Signature does this installation have.
        /// </summary>
        public float EMSignature { get; set; }

        /// <summary>
        /// All of the information relating to the civilian and naval shipyards.
        /// </summary>
        public BindingList<ShipyardInformation> SYInfo { get; set; }

        /// <summary>
        /// If this installation requires another to be built: CI Conversions and mine conversions. A value of InstallationCount means no required installation
        /// I don't want to cast these as -1s since I'm not really sure how that would work.
        /// </summary>
        public InstallationType RequiredPrerequisitInstallation { get; set; }

        /// <summary>
        /// If this installation represents a conversion of an existing installation, this is the output installation.
        /// </summary>
        public InstallationType OutputInstallation { get; set; }

        /// <summary>
        /// If this installation requires a specific technology to be constructed this will be it. This will be set to Count if not the case.
        /// </summary>
        public Faction.FactionTechnology RequiredTechnology { get; set; }

        /// <summary>
        /// If a technology is required this will be the tech level to check against.
        /// </summary>
        public int RequiredTechLevel { get; set; }


        /// <summary>
        /// Some installations cannot be constructed.
        /// </summary>
        public bool CanBeBuilt { get; set; }

        decimal[] m_aiMinerialsCost;
        public decimal[] MinerialsCost
        {
            get
            {
                return m_aiMinerialsCost;
            }
            set
            {
                m_aiMinerialsCost = value;
            }
        }

        public Installation()
            : base()
        {
            /// <summary>
            /// Id must be present or anything needing it will lose its cookies.
            /// </summary>

            Number = 0;
            Mass = 25000;
            Type = InstallationType.InstallationCount;
            ThermalSignature = 0;
            EMSignature = 0;
            RequiredPrerequisitInstallation = InstallationType.InstallationCount;
            OutputInstallation = InstallationType.InstallationCount;
            RequiredTechnology = Faction.FactionTechnology.Count;
            RequiredTechLevel = -1;
            CanBeBuilt = false;
        }

        public Installation(InstallationType a_eType)
            : base()
        {
            /// <summary>
            /// Id must be present or anything needing it will lose its cookies.
            /// </summary>

            Number = 0;
            Mass = 25000;
            Type = a_eType;
            m_aiMinerialsCost = new decimal[Constants.Minerals.NO_OF_MINERIALS];
            ThermalSignature = 0;
            EMSignature = 0;
            RequiredPrerequisitInstallation = InstallationType.InstallationCount;
            OutputInstallation = InstallationType.InstallationCount;
            RequiredTechnology = Faction.FactionTechnology.Count;
            RequiredTechLevel = -1;
            CanBeBuilt = true;

            switch (a_eType)
            {
                case InstallationType.AutomatedMine:
                    {
                        Name = "Automated Mine";
                        Cost = 240;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 120;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.CivilianMiningComplex:
                    {
                        Name = "Civilian Mining Complex";
                        ThermalSignature = 50;
                        EMSignature = 0;
                        CanBeBuilt = false;
                        break;
                    }
                case InstallationType.CommercialShipyard:
                    {
                        Name = "Commercial Shipyard Complex";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        SYInfo = new BindingList<ShipyardInformation>();

                        /// <summary>
                        /// For base
                        /// </summary>
                        ThermalSignature = 220;
                        EMSignature = 110;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.ConstructionFactory:
                    {
                        Name = "Construction Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 60;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Vendarite] = 30;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.ConventionalIndustry:
                    {
                        Name = "Conventional Industry";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        CanBeBuilt = false;
                        break;
                    }
                case InstallationType.ConvertCIToConstructionFactory:
                    {
                        Name = "Convert CI to Construction Factory";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 10;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 5;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Vendarite] = 5;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.ConstructionFactory;
                        break;
                    }
                case InstallationType.ConvertCIToFighterFactory:
                    {
                        Name = "Convert CI to Fighter Factory";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 5;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 15;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.FighterFactory;
                        break;
                    }
                case InstallationType.ConvertCIToFuelRefinery:
                    {
                        Name = "Convert CI to Fuel Refinery";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 20;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.FuelRefinery;
                        break;
                    }
                case InstallationType.ConvertCIToMine:
                    {
                        Name = "Convert CI to Mine";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 10;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 10;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.Mine;
                        break;
                    }
                case InstallationType.ConvertCIToOrdnanceFactory:
                    {
                        Name = "Convert CI to Ordnance Factory";
                        /// <summary>
                        /// CI can't be built, but can be converted for 20 cost to make other installations.
                        /// </summary>
                        Cost = 20;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 10;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 10;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.ConventionalIndustry;
                        OutputInstallation = InstallationType.OrdnanceFactory;
                        break;
                    }
                case InstallationType.ConvertMineToAutomated:
                    {
                        Name = "Convert mine to Automated";
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 75;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 75;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        RequiredPrerequisitInstallation = InstallationType.Mine;
                        OutputInstallation = InstallationType.AutomatedMine;
                        break;
                    }

                case InstallationType.DeepSpaceTrackingStation:
                    {
                        Name = "DeepSpace Tracking Station";
                        Cost = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 150;
                        ThermalSignature = 5;
                        EMSignature = 0;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.FighterFactory:
                    {
                        Name = "Fighter Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 90;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.FinancialCentre:
                    {
                        Name = "Financial Centre";
                        Cost = 240;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 120;
                        ThermalSignature = 5;
                        EMSignature = 50;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.FuelRefinery:
                    {
                        Name = "Fuel Refinery";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 120;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.GeneticModificationCentre:
                    {
                        Name = "Genetic Modification Centre";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 300;
                        Mass = 50000;
                        ThermalSignature = 10;
                        EMSignature = 50;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.GroundForceTrainingFacility:
                    {
                        Name = "Ground Force Training Facility";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        ThermalSignature = 10;
                        EMSignature = 100;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.Infrastructure:
                    {
                        Name = "Infrastructure";
                        Cost = 2;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 2;
                        Mass = 2500;
                        ThermalSignature = 0.5f;
                        EMSignature = 0.5f;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.MaintenanceFacility:
                    {
                        Name = "Maintenance Facility";
                        Cost = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 75;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 75;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.MassDriver:
                    {
                        Name = "Mass Driver";
                        Cost = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 100;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 100;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 100;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.MilitaryAcademy:
                    {
                        Name = "Military Academy";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 300;
                        Mass = 100000;
                        ThermalSignature = 10;
                        EMSignature = 100;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.Mine:
                    {
                        Name = "Mine";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 60;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corundium] = 60;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.NavalShipyardComplex:
                    {
                        Name = "Naval Shipyard Complex";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Neutronium] = 1200;
                        Mass = 100000;
                        SYInfo = new BindingList<ShipyardInformation>();

                        /// <summary>
                        /// base signatures.
                        /// </summary>
                        ThermalSignature = 220;
                        EMSignature = 110;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.OrdnanceFactory:
                    {
                        Name = "Ordnance Factory";
                        Cost = 120;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 30;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Tritanium] = 90;
                        ThermalSignature = 5;
                        EMSignature = 5;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.ResearchLab:
                    {
                        Name = "Research Lab";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 1200;
                        Mass = 100000;
                        ThermalSignature = 50;
                        EMSignature = 100;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.SectorCommand:
                    {
                        Name = "Sector Command";
                        Cost = 2400;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 600;
                        Mass = 100000;
                        ThermalSignature = 20;
                        EMSignature = 150;
                        RequiredTechnology = Faction.FactionTechnology.ImprovedCommandAndControl;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.Spaceport:
                    {
                        Name = "Spaceport";
                        Cost = 1200;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Corbomite] = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 150;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Mercassium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Uridium] = 300;
                        Mass = 50000;
                        ThermalSignature = 50;
                        EMSignature = 100;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
                case InstallationType.TerraformingInstallation:
                    {
                        Name = "Terraforming Installation";
                        Cost = 600;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Duranium] = 300;
                        m_aiMinerialsCost[(int)Constants.Minerals.MinerialNames.Boronide] = 300;
                        Mass = 50000;
                        ThermalSignature = 100;
                        EMSignature = 25;
                        RequiredTechnology = Faction.FactionTechnology.TransNewtonianTech;
                        RequiredTechLevel = 0;
                        CanBeBuilt = true;
                        break;
                    }
            }
        }


        /// <summary>
        /// Is this installation buildable?
        /// </summary>
        /// <param name="Fact">Faction owner of this installation type.</param>
        /// <param name="Pop">Population on which this installation is to be built.</param>
        /// <returns>Whether or not the installation can be built by Faction Fact on Population Pop</returns>
        public bool IsBuildable(Faction Fact, Population Pop)
        {
            /// <summary>
            /// CI and CMCs are not buildable.
            /// </summary>
            if (CanBeBuilt == false)
            {
                return false;
            }

            /// <summary>
            /// Technology Check
            /// </summary>
            if (RequiredTechnology != Faction.FactionTechnology.Count)
            {
                if (Fact.FactionTechLevel[(int)RequiredTechnology] < RequiredTechLevel)
                {
                    return false;
                }
            }

            /// <summary>
            /// Required installation check.
            /// </summary>
            if (RequiredPrerequisitInstallation != InstallationType.InstallationCount)
            {
                if (Pop.Installations[(int)RequiredPrerequisitInstallation].Number < 1.0f)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
