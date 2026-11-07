# LT_Education – Features & Agent Reference

Single campaign behaviour (`LT_EducationBehaviour`, partial over several files) plus UI. No MCM: all tuning values are fields/consts in code.
Target: net472, C# 12, x64, Nullable enabled. Game dependency v1.4.7 (`_Module\SubModule.xml`). Version in `LT_Education.csproj` `<Version>`.

## Features

```
| Feature | Gate | Files |
|---|---|---|
| Learn to read (town scholar, 10 gold/h, progress/h = (INT-4)*0.2+1, INT capped at 10) | INT >= 3 (`_minINTToRead`), only in towns; villages/castles show a "go to town" menu | `LTEducationMenus.cs`, `LTEducationBooks.cs` |
| Books (18, one per skill, ~+30 skill, less at high skill, 0 above 300) and scrolls (18, x3 faster, ~+10) | player `_canRead >= 100` | `LTEducationBooks.cs`, `_Module\ModuleData\book_items.xml` |
| Active reading in "Manage your education" > Start reading (wait menu, hourly) | town/castle, or within 1.05 of last visited village; not prisoner, not besieging, not in army | `LTEducationBooks.cs` (`ReadPlayerBookActive`), `LTEducationMenus.cs` |
| Passive reading +0.5%/day | book selected, book still held, not prisoner | `LTEducationBooks.cs` (`ReadPlayerBookPassive`) |
| Read books get " [Read]" name suffix (LTE00531), applied on load and on finish | book progress 100 | `LTEducationBooks.cs` (`MarkBookItemAsRead`) |
| Companion literacy: learn per day (INT-4)*0.333+1 (INT capped at 10); read per day (100/(80-INT*5))*0.75 %, scrolls x3; optional auto-select next book | companion in main party; INT >= `_minINTToRead` | `LTECompanions.cs`, `LTECompanionEducationData.cs` |
| Books held by readers who died/left the clan return to the main party | daily | `LTECompanions.cs` (`ReturnBooksFromHeroesWhoLeftClan`) |
| Child coming of age in player clan can read immediately | INT >= `_minINTToRead` | `LTECompanions.cs` (`CompanionComesOfAge`) |
| "Can read!" map notification (opens Book Stash) | companion finishes learning to read | `UI\MapNotification\LTEMapNotification.cs` |
| Book Stash window (`LTEducationBookStashVM`: prev/next hero, party books with Read button via `LTEducationBookVM`, stop reading, auto-read checkbox) | education menu "Book Stash", map notification, Alt+F12 on campaign map (not in menus) | `UI\BookStash\LTEBookStashVM.cs`, `UI\BookStash\LTEBookVM.cs`, `UI\LTMapView.cs`, `UI\LTUIManager.cs`, `SubModule.cs` |
| Book vendors Eadric/Ingeborg/Ahsan: special heroes, 8-12 books, trade needs >= 1000 gold; never die of old age (`CanHeroDieEvent`) | always; weekly: dead vendors replaced (also on load), living vendors move to 1 of 4 nearest towns + restock | `LTEducationVendors.cs`, `LTEducationDialogs.cs`, `LTEducationBooks.cs` (`GiveBooksToVendors`), `_Module\ModuleData\characters.xml` |
| Vendor spot in town by hour: >15 tavern, >7 center, else lordshall | on every game menu open, player in a town | `LTEducationVendors.cs` (`RelocateVendorInTownLocation`) |
| Vendor greeting relation: Eadric +4..5, Ingeborg +9..10 (if last meeting > 7 days), Ahsan -3 each time | applied in greeting consequence | `LTEducationVendors.cs`, `LTEducationDialogs.cs` |
| Tavern keeper: vendor whereabouts (5 gold; same town or 10 nearest towns) | gold >= 5 | `LTEducationDialogs.cs` |
| Tavern keeper: scholars in 20 nearest settlements (15 gold), optional map tracking | gold >= 15 | `LTEducationDialogs.cs`, `LTEducationScholars.cs` |
| 72 scholars (18 skills x 4 levels), 250/500/1000/2000 gold/h, 8h training, 10h rest; XP each = price/5*8*(1-0.2*interrupts) | scholar in current settlement; enough gold; rest elapsed | `LTEducationScholars.cs`, `LTEducationMenus.cs` |
| Scholar migration: 72/7 = 10 scholars move daily | daily | `LTEducationScholars.cs` (`MoveScholars`) |
| Map nameplate book icon for settlements with a scholar or vendor | party scout Scouting >= 200 (`ScoutingLevelToSeeScholarIcons`) | `UI\NameplateIcon\SettlementNameplateVMMixin.cs`, `UI\NameplateIcon\SettlementNameplateItemExtension.cs` |
| Tutelage: daily, best party member per skill trains others; charm boosts, relation < -50 blocks, trainee SOC+INT scale | >= 2 unwounded heroes in main party | `LTEducationTutelage.cs` |
| Weapon part teaching (blacksmith/armorer/weaponsmith dialog), up to 10 daily offers; tier prices 2k/20k/50k/130k/300k, minus trade/smithing/charm (skill/15 % each, cap 300), halved unless BannerKings | conversation with smith occupations; offers refreshed daily and on first smith talk after load | `LTEWeaponParts.cs`, `LTEducationDialogs.cs` |
| Learning rate floor 0.05 | always (Harmony Postfix) | `LTEducationHarmony.cs` |
| Console: `lteducation.debug 1` (debug, min INT 2, no rest), `2` (kill vendors), other (debug off, restore 3 / 10h) | console | `LT_EducationBehaviour.cs` (`ConsoleDebug`) |
| Popup (`EducationPopupVM`) when the player finishes a book/scroll or learns to read | player only | `UI\Popup\LTEducationPopup.cs`, `UI\Popup\LTEducationPopupVM.cs` |
```

## Entry points

- `SubModule.OnSubModuleLoad`: `new Harmony("lt_education").PatchAll()`, UIExtender `"lt_education"` register + enable.
- `SubModule.OnGameStart`: adds `LT_EducationBehaviour` (campaign game type only).
- `SubModule.OnGameInitializationFinished`: sets `_bannerKingsActive` if module `BannerKings` is loaded.
- `SubModule.OnApplicationTick`: Alt (held) + F12 (pressed) on `MapState`, not a menu state → Book Stash.
- Behaviour events: `OnGameLoaded`, `OnSessionLaunched` (init books, vendors, scholars, dialogs, menus), hourly (`ReadPlayerBookActive`), daily (scholars move, tutelage, companions, passive reading, weapon part offers), weekly (vendors move + restock), `GameMenuOpened` (vendor spot), `HeroComesOfAge`, `CanHeroDieEvent` (vendors immune to old age).
- Behaviour constructor resets static popup state and `LTUIManager.Instance.Reset()` (new/loaded game).

## Save data (`LT_EducationBehaviour.SyncData`)

| Key | Type | Notes |
|---|---|---|
| `LTEducation_bookInProgress` | int | -1 = none |
| `LTEducation_bookProgress` | float[100] | resized to 100 on load for old saves |
| `LTEducation_canRead` | float | 0..100 |
| `LTEducation_scholarSettlements` | Settlement[72] | |
| `LTEducation_companionEducationData` | List<LTECompanionEducationData> | null-guarded on load |
| `LTEducation_inTraining` | bool | added 1.11.4 |
| `LTEducation_startTimeOfTraining` | CampaignTime | added 1.11.4 |
| `LTEducation_trainingInterrupted` | int | added 1.11.4 |
| `LTEducation_trainingScholarIndex` | int | added 1.11.4 |
| `LTEducation_trainingHeroList` | List<Hero> | added 1.11.4, null-guarded on load |

- `CustomSaveDefiner` (`LTECompanionEducationData.cs`), base id 1885468684: class `LTECompanionEducationData` = 1, `LTECanReadMapNotification` = 10, container `List<LTECompanionEducationData>`. Append only.
- `LTECompanionEducationData` saveable fields 1-5: Id (MBGUID), CanRead, BookProgress (float[100]), BookInProgress, AutoRead.
- Do not rename `LT_EducationBehaviour` (save data-store id).

## Book indexing

- Index 1-18 books, 19-36 scrolls, skill order: OneHanded, TwoHanded, Polearm, Bow, Crossbow, Throwing, Riding, Athletics, Smithing, Scouting, Tactics, Roguery, Charm, Leadership, Trade, Steward, Medicine, Engineering.
- `_booksInMod = 36`. `GetBookIndex` / `GetBookStringId` map index ↔ `education_book_<skill><1|2>` (`ahthletics` typo is the real item id). Unknown → 1.
- `education_book_test` (not merchandise) is the first item in `book_items.xml`; never index books by list position — use `GetBookItem(index)`.
- A reader's current book is moved from the party roster into `Hero.SpecialItems`; `HeroStopReadingAndReturnBookToParty` returns it and always clears the reading state.

## Scholars

- `_scholarSettlements[i]`: skill = i / 4, level/price = i % 4. Index 0 (`LTE01500`, "Lady Pokesalot") is a valid scholar; "no scholar" is -1.
- Names `LTE01500..LTE01571`, images `lt_education_scholar<i>`.

## Menus / dialogs

- Menus: `education_menu`, `education_reading_menu` (wait), `education_learn_read_menu_town|village|castle`, `education_learn_read_menu_town_progress` (wait), `scholar_menu`, `scholar_training_wait` (wait). Entry option `education_menu` on `town`, `castle`, `village`. `test_popup` only if `_debug` at session launch.
- Dialog ids: `tavernkeeper_book*`, `tavernkeeper_scholars*`, `bookvendor_talk`, `weaponsmith_parts`, `weapon_parts_ok`.

## UI

- `LTMapView` ids: `BookStash` → prefab `LTEBookStash`; `TestPopup` → `LTETestPopup`. Both use `LTEducationBookStashVM`. Layer added to `MapScreen.Instance`.
- Popup prefab `LTEducationBookPopup` (`EducationPopupVM`), layer stored with the screen it was added to.
- Prefab extension appends the book icon to `SettlementNameplateItemSmall|Medium|Large` at `descendant::ListPanel[@Id='EventsListPanel']`, bound to `HasScholar`, `BookIconYOffset` (`SettlementNameplateVMMixin`, mixin on `SettlementNameplateVM`, refresh method `RefreshBindValues`).
- Map notification brush id `lte_can_read` (`_Module\GUI\Brushes\CustomMapNotification.xml`). Sprites: `_Module\GUI\LT_EducationSpriteData.xml`.

## Harmony

- `LearningRatePatch` → `DefaultCharacterDevelopmentModel.CalculateLearningRate(IReadOnlyPropertyOwner<CharacterAttribute>, int, int, SkillObject, bool)` Postfix, `LimitMin(0.05f)`.
- `PopulateNotificationsPatch` → `MapNotificationVM.PopulateTypeDictionary` Postfix, registers notification VM.

## Version-fragile spots (re-check on game updates)

- Reflection field `MapNotificationVM._itemConstructors` (`LTEMapNotification.cs`).
- Private `ICraftingCampaignBehavior` impl method `OpenPart` with 3 params (piece, template, bool) (`LTEWeaponParts.cs`); null-checked, logs on mismatch.
- `ItemObject.Name` private setter via `AccessTools.Property` (`LTEducationBooks.cs`).
- `LocationComplex.GetLocationOfCharacter(Hero)`, `Location.RemoveCharacter(Hero)`, `AgentData`/`LocationCharacter` ctors (`LTEducationVendors.cs`).
- Namespaces are not folder-based: root files `LT_Education`, UI `LT.UI*`, nameplate prefab extension `UI.Extensions`, helpers `LT.Helpers` / `LT.Logger`.
- Harmony attribute targets above (BUTR.Harmony.Analyzer checks them at build).

## Localization

- Legacy string ids `LTE<5 digits>`, never renumber. Known blocks: 00301-00303 vendor names, 01300+ tavern keeper, 014xx weapon parts, 01500-01571 scholar names. New ids: `LTE_` + 7 random chars (`D:\Modding\Bannerlord\Docs\Localization.md`), appended at the end in all files.
- English is the inline default text in code; `_Module\ModuleData\Languages\std_module_strings_xml.xml` is the English table / translator template (not loaded: root `language_data.xml` only adds the English voice file).
- Translations (13): BR, CNs, CNt, DE, FR, IT, JP, KO, PL, RU, SP, TR per the Localization.md table, plus UA (`UA\std_module_strings_xml_ukr.xml`). Same ids, same order as English. PL/RU/UA `language_data.xml` also reference `VoicedLines/voiced_lines_en.xml`.
- Concatenated strings (keep leading/trailing space): `LTE00304`, `LTE00531`, `LTE01210`, `LTE01211`, `LTE01317`; `LTE00000` ("and") must not be empty.
- Use `{=ID}` with `SetTextVariable` for variables; never concatenate values into the TextObject source string.

## Build / deploy

- `dotnet build LT_Education.csproj -c Release` (single TFM net472). Needs `BANNERLORD_GAME_DIR`.
- Bannerlord.BuildResources deploys `_Module\**` + DLL to `$(BANNERLORD_GAME_DIR)\Modules\LT_Education\` and substitutes `$modulename$`, `$version$`.
- Player changelog: `_Module\LTE_Changelog.txt`.
- Logging: `LT.Logger.LTLogger` (`Helpers\LTLogger.cs`).
