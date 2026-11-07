using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace LT_Education
{
    public partial class LT_EducationBehaviour : CampaignBehaviorBase
    {

        public static LT_EducationBehaviour Instance { get; set; }

        private bool _debug = false;

        private static GauntletLayer? _gauntletLayer;
        private static GauntletMovieIdentifier? _gauntletMovie;
        private static EducationPopupVM? _popupVM;
        private static ScreenBase? _popupScreen;
        
        readonly int _booksInMod = 36;              // how many books are actually implemented into the mod
        IEnumerable<ItemObject>? _bookList;         // all education books in the game
        List<Hero>? _vendorList;                    // all book vendors in the game
        private Hero? _pendingRelationVendor;       // vendor greeting relation change, applied in the greeting consequence
        private int _pendingRelationChange;

        List<ItemObject> _tradeRoster = new();      // Item Roster for book trade

        public readonly int ScoutingLevelToSeeScholarIcons = 200;    // how advanced party scout should be to see scholar icons?

        private const int DefaultMinINTToRead = 3;  // minimum INT to be able to learn to read
        private const int DefaultTrainingRest = 10; // hours of rest required between scholar trainings

        private float _canRead;
        private int _minINTToRead = DefaultMinINTToRead;
        private readonly int _readPrice = 10;       // price to learn to read /h
        private int _lastHourOfLearning;            // to keep track for paid hours

        bool _readingInMenu = false;                // mark that we are reading in menu to handle special village case

        private int _bookInProgress;
        private float[] _bookProgress;

        // Companion Education
        private List<LTECompanionEducationData> _companionEducationData;
        public LTECompanions _LTECompanions;

        // Scholars
        readonly int _totalScholars = 72;
        Settlement[] _scholarSettlements = new Settlement[72];
        private CampaignTime _startTimeOfTraining;
        private int _trainingInterrupted = 0;
        private bool _inTraining = false;
        private readonly int _trainingDuration = 8;
        private int _trainingRest = DefaultTrainingRest;
        private int _trainingScholarIndex = -1;
        private List<Hero> _trainingHeroList = new();

        // Smithing Parts
        private List<CraftingPiece> _currentWeaponPartsInventory;
        private List<CraftingPiece> _lockedWeaponParts;
        
        public bool _bannerKingsActive = false;

        public LT_EducationBehaviour()
        {

            Instance = this;

            // drop UI state left over from a previous game in this session
            ResetPopupState();
            LT.UI.LTUIManager.Instance.Reset();

            this._canRead = 0;
            this._bookInProgress = -1;
            this._bookProgress = new float[100];

            for (int i = 0; i < 100; i++)
            {
                this._bookProgress[i] = 0f;
            }

            this._vendorList = new List<Hero>{};

            this._companionEducationData = new List<LTECompanionEducationData>();

            this._LTECompanions = new LTECompanions(this._companionEducationData);
        }



        public override void RegisterEvents()
        {
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnGameLoaded));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(this.OnSessionLaunched));

            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, HourlyTickEvent);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTickEvent);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTickEvent);

            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, new Action<MenuCallbackArgs>(this.OnGameMenuOpened));

            CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener(this, new Action<Hero>(this.OnHeroComesOfAge));

            CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanVendorDie);

            //CampaignEvents.SettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnSettlementEntered));
            //CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, new Action<MobileParty, Settlement, Hero>(this.OnAfterSettlementEntered));  
            //CampaignEvents.OnWorkshopChangedEvent.AddNonSerializedListener(this, OnWorkshopChangedEvent);
            //CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, DailyTickTownEvent);
            //CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, new Action<IMission>(this.OnMissionEnded));
            //CampaignEvents.LocationCharactersAreReadyToSpawnEvent.AddNonSerializedListener(this, new Action<Dictionary<string, int>>(this.LocationCharactersAreReadyToSpawn));             // OnTavernEntered
        }


        [CommandLineFunctionality.CommandLineArgumentFunction("debug", "lteducation")]
        public static string ConsoleDebug(List<string> args)
        {
            if (args.Count < 1)
            {
                return "You must provide an argument";
            }

            if (args[0] == "1")
            {
                if (Instance == null) return $"Debug failed";

                Instance._debug = true;
                Instance._minINTToRead = 2;
                Instance._trainingRest = 0;

                //Hero.MainHero.ChangeHeroGold(-1000000);

                return $"Debug enabled";
            }
            else if (args[0] == "2")
            {

                if (Instance == null || Instance._vendorList == null) return $"Debug failed";

                foreach (Hero vendor in Instance._vendorList)
                {
                    KillCharacterAction.ApplyByDeathMarkForced(vendor, true);
                }               

                return $"Vendors are dead :(";
            }
            else 
            {
                if (Instance == null) return $"Debug failed";

                Instance._debug = false;
                Instance._minINTToRead = DefaultMinINTToRead;
                Instance._trainingRest = DefaultTrainingRest;
                return $"Debug disabled";
            }
        }


        private void OnGameLoaded(CampaignGameStarter starter)
        {
            //Logger.IM("Game loaded");

            int size = _bookProgress.Length;
            // array length fix from previous versions to fit more books/scrolls
            if (size < 100)
            {
                Array.Resize(ref _bookProgress, 100);
            }

            // add [Read] to the read books
            MarkReadBooks();

            this._companionEducationData ??= new List<LTECompanionEducationData>();
            this._trainingHeroList ??= new List<Hero>();

            this._LTECompanions = new LTECompanions(this._companionEducationData);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {

            InitBookList();
            if (_bookList == null) return;

            InitVendors();
            if (_vendorList == null) return;

            InitScholars();

            AddDialogs(starter);
            AddGameMenus(starter);

        }


        //private void LocationCharactersAreReadyToSpawn(Dictionary<string, int> unusedUsablePointCount)
        //{
        //    Settlement settlement = PlayerEncounter.LocationEncounter.Settlement;
        //    if (settlement.IsTown && CampaignMission.Current != null)
        //    {
        //        Location location = CampaignMission.Current.Location;
        //        if (location != null && location.StringId == "tavern")
        //        {

        //            Logger.IMGreen("We are in the Tavern");

        //            //location.AddLocationCharacters(new CreateLocationCharacterDelegate(TavernEmployeesCampaignBehavior.CreateRansomBroker), settlement.Culture, LocationCharacter.CharacterRelations.Neutral, 1);
        //            //return;

        //        }
        //    }
        //}

        //private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        //{
            //if (MobileParty.MainParty.CurrentSettlement == settlement) { 
            //    Logger.IMGreen("Welcome to " + settlement.Name.ToString());
            //}
        //}


        //private void OnAfterSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        //{

        //}


        private void OnGameMenuOpened(MenuCallbackArgs obj)
        {
            //Logger.IM("OnGameMenuOpened");
            RelocateVendorInTownLocation();
        }

        //private void OnMissionEnded(IMission obj)
        //{
        //    Logger.IM("OnMissionEnded");
        //}


        private void WeeklyTickEvent()
        {                     
            if (_vendorList == null || _bookList == null) return;
            
            // CreateVendors already restocked everyone; a second restock would remove 3 more books
            bool replaced = ReplaceDeadVendors();

            RelocateVendorsToOtherTowns();

            if (!replaced) GiveBooksToVendors();
        }


        private void DailyTickEvent()
        {
            MoveScholars();

            LTEducationTutelage.TutelageRun();

            this._LTECompanions.ProcessCompanionsEducation();

            ReadPlayerBookPassive();

            if (_debug) ShowVendorLocations();

            RefreshWeaponPartsCurrentInventory();
        }

        private void HourlyTickEvent()
        {
            //Logger.IM("1h passed");
            ReadPlayerBookActive();

            //string soundEvent = "event:/ui/notification/levelup";
            //MBInformationManager.AddQuickInformation(new TextObject(soundEvent), 0, Hero.MainHero.CharacterObject, soundEvent);

            //Campaign.Current.CampaignInformationManager.NewMapNoticeAdded(new LTECanReadMapNotification(new TextObject("TEST")));

            //RefreshWeaponPartsCurrentInventory();
        }


        private void OnHeroComesOfAge(Hero hero)
        {
            if (hero.Clan != Clan.PlayerClan) return;
            this._LTECompanions.CompanionComesOfAge(hero);
        }


        // Save keys:
        // LTEducation_bookInProgress, LTEducation_bookProgress, LTEducation_canRead, LTEducation_scholarSettlements,
        // LTEducation_companionEducationData, LTEducation_inTraining, LTEducation_startTimeOfTraining,
        // LTEducation_trainingInterrupted, LTEducation_trainingScholarIndex, LTEducation_trainingHeroList
        public override void SyncData(IDataStore dataStore)
        {          
            dataStore.SyncData<int>("LTEducation_bookInProgress", ref this._bookInProgress);
            dataStore.SyncData<float[]>("LTEducation_bookProgress", ref this._bookProgress);
            dataStore.SyncData<float>("LTEducation_canRead", ref this._canRead);

            dataStore.SyncData<Settlement[]>("LTEducation_scholarSettlements", ref this._scholarSettlements);

            dataStore.SyncData<List<LTECompanionEducationData>>("LTEducation_companionEducationData", ref this._companionEducationData);

            // scholar training in progress (so a save made during training resumes it correctly)
            dataStore.SyncData<bool>("LTEducation_inTraining", ref this._inTraining);
            dataStore.SyncData<CampaignTime>("LTEducation_startTimeOfTraining", ref this._startTimeOfTraining);
            dataStore.SyncData<int>("LTEducation_trainingInterrupted", ref this._trainingInterrupted);
            dataStore.SyncData<int>("LTEducation_trainingScholarIndex", ref this._trainingScholarIndex);
            dataStore.SyncData<List<Hero>>("LTEducation_trainingHeroList", ref this._trainingHeroList);
        }


    }


}