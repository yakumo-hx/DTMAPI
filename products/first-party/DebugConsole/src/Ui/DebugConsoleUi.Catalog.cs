using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DTMAPI.Abstractions;

namespace DTMAPI.DebugConsole
{
    internal sealed partial class DebugConsoleUi
    {
        private void BuildItemsTab()
        {
            if (inventoryApi == null)
            {
                HideUnusedCatalogCells(0);
                AddText(panelRoot!, "DTMAPI.DebugConsole.Items.Missing", T("debug.missing.inventory", "Inventory debug API is not available."), 18, Color(1f, 0.72f, 0.55f, 1f), TextAnchorMiddleLeft, 28, -104, 660, 32);
                return;
            }

            float top = layout?.Breakpoint == DebugConsoleBreakpoint.Wide ? -96f : -142f;
            int pageSize = layout?.CatalogPageSize ?? 35;
            int columns = layout?.CatalogColumns ?? 5;
            EnsureCatalogCellPool(pageSize);
            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Items.Title", T("debug.section.items", "Items"), 28, top, 220, 28);
            CreateInput(panelRoot!, "DTMAPI.DebugConsole.Search", searchText, value =>
            {
                searchText = value ?? string.Empty;
                itemPage = 0;
                sourcePage = 0;
                dirtyRegions |= UiDirtyRegion.Catalog;
            }, 112, top + 2, 390, Math.Max(36, (float)(layout?.MinimumClickSize ?? 44d)));

            if (string.IsNullOrWhiteSpace(sourceFilter))
                sourceFilter = SourceFilterBase;
            string normalizedSearch = (searchText ?? string.Empty).Trim();
            string normalizedSource = (sourceFilter ?? string.Empty).Trim();

            bool monsterCategory = category.Equals("Monster", StringComparison.OrdinalIgnoreCase);
            bool animalCategory = category.Equals("Animal", StringComparison.OrdinalIgnoreCase);
            bool virtualCategory = monsterCategory || animalCategory;
            IReadOnlyList<SpawnCatalogOption> monsterEntries =
                monsterCategory
                    ? advancedApi?.GetMonsterCatalog() ??
                        Array.Empty<SpawnCatalogOption>()
                    : Array.Empty<SpawnCatalogOption>();
            IReadOnlyList<AnimalCatalogOption> animalEntries =
                animalCategory
                    ? advancedApi?.GetAnimalCatalog() ??
                        Array.Empty<AnimalCatalogOption>()
                    : Array.Empty<AnimalCatalogOption>();
            InventoryDebugPage page = inventoryApi.GetItems(
                new InventoryDebugQuery
                {
                    SearchText = virtualCategory
                        ? string.Empty
                        : normalizedSearch,
                    Category = virtualCategory
                        ? string.Empty
                        : category ?? string.Empty,
                    SourceId = normalizedSource,
                    IncludeUnavailable = true,
                    Page = virtualCategory ? 0 : itemPage,
                    PageSize = pageSize
                });

            float sourceX = 28;
            float categoryX = 198;
            float gridX = 388;
            float filterTop = top - 34;
            float buttonHeight = Math.Max(36, (float)(layout?.MinimumClickSize ?? 44d));
            float buttonStep = buttonHeight + 4;
            int sourcePageSize = Math.Max(
                4,
                Math.Min(16, (int)Math.Floor(((layout?.LogicalHeight ?? 900d) - 260d) / buttonStep)));
            float filterPagerY = filterTop - sourcePageSize * buttonStep - 34;
            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Items.SourceTitle", T("debug.items.sourceColumn", "Source"), sourceX, filterTop, 150, 26);
            InventoryDebugSourceGroup[] sources =
                GetUnifiedCatalogSources(
                    page.Sources,
                    monsterEntries,
                    animalEntries);
            int sourceTotalPages = Math.Max(1, (int)Math.Ceiling(sources.Length / (double)sourcePageSize));
            sourcePage = Math.Max(0, Math.Min(sourcePage, sourceTotalPages - 1));
            int sourceIndex = 0;
            int sourceStart = sourcePage * sourcePageSize;
            int sourceEnd = Math.Min(
                sources.Length,
                sourceStart + sourcePageSize);
            for (int sourceOffset = sourceStart;
                sourceOffset < sourceEnd;
                sourceOffset++)
            {
                InventoryDebugSourceGroup source = sources[sourceOffset];
                InventoryDebugSourceGroup selectedSource = source;
                bool selected = selectedSource.Id.Equals(sourceFilter, StringComparison.OrdinalIgnoreCase);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Source." + sourceIndex, FormatSourceButtonLabel(selectedSource), () =>
                {
                    sourceFilter = selectedSource.Id;
                    itemPage = 0;
                    dirtyRegions |= UiDirtyRegion.Catalog;
                }, selected ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.13f, 0.15f, 0.17f, 1f), selectedSource.Count > 0 ? Color(1f, 1f, 1f, 1f) : Color(0.55f, 0.60f, 0.62f, 1f), sourceX, filterTop - 30 - sourceIndex * buttonStep, 154, buttonHeight);
                sourceIndex++;
            }
            if (sourceTotalPages > 1)
            {
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Source.Prev", "<", () =>
                {
                    sourcePage = Math.Max(0, sourcePage - 1);
                    dirtyRegions |= UiDirtyRegion.Catalog;
                }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), sourceX, filterPagerY, 44, buttonHeight);
                AddText(panelRoot!, "DTMAPI.DebugConsole.Source.Page", (sourcePage + 1) + "/" + sourceTotalPages, 13, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleCenter, sourceX + 48, filterPagerY, 58, buttonHeight);
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Source.Next", ">", () =>
                {
                    sourcePage = Math.Min(sourceTotalPages - 1, sourcePage + 1);
                    dirtyRegions |= UiDirtyRegion.Catalog;
                }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), sourceX + 110, filterPagerY, 44, buttonHeight);
            }

            AddSectionLabel(panelRoot!, "DTMAPI.DebugConsole.Items.CategoryTitle", T("debug.items.categoryColumn", "Category"), categoryX, filterTop, 160, 26);
            bool wrapCategories =
                layout?.Breakpoint != DebugConsoleBreakpoint.Wide ||
                (layout?.LogicalHeight ?? 900d) < 900d;
            float categoryGap = 4f;
            float categoryButtonWidth = wrapCategories
                ? (160f - categoryGap) / 2f
                : 160f;
            CreateButton(panelRoot!, "DTMAPI.DebugConsole.Category.All", T("debug.items.categoryAll", "All categories"), () =>
            {
                category = string.Empty;
                itemPage = 0;
                dirtyRegions |= UiDirtyRegion.Catalog;
            }, string.IsNullOrWhiteSpace(category) ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), categoryX, filterTop - 30, 160, buttonHeight);

            int catIndex = 0;
            foreach (string cat in CatalogCategoryIds)
            {
                string selectedCat = cat;
                bool selected = selectedCat.Equals(category, StringComparison.OrdinalIgnoreCase);
                int categoryColumn = wrapCategories ? catIndex % 2 : 0;
                int categoryRow = wrapCategories ? catIndex / 2 : catIndex;
                CreateButton(panelRoot!, "DTMAPI.DebugConsole.Category." + catIndex, FormatCategoryLabel(selectedCat), () =>
                {
                    category = selectedCat;
                    itemPage = 0;
                    dirtyRegions |= UiDirtyRegion.Catalog;
                }, selected ? Color(0.10f, 0.36f, 0.34f, 1f) : Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), categoryX + categoryColumn * (categoryButtonWidth + categoryGap), filterTop - 30 - (categoryRow + 1) * buttonStep, categoryButtonWidth, buttonHeight);
                catIndex++;
            }

            int totalItems;
            int totalPages;
            int currentPage;
            if (monsterCategory)
            {
                var slice = new CatalogPageAccumulator<SpawnCatalogOption>(
                    itemPage,
                    pageSize);
                foreach (SpawnCatalogOption option in monsterEntries)
                {
                    if (!CatalogSourceMatches(
                        normalizedSource,
                        SourceFilterBase,
                        isModSource: false) ||
                        !CatalogMatches(
                        normalizedSearch,
                        option.Id,
                        option.DisplayName,
                        option.Category))
                    {
                        continue;
                    }
                    slice.Add(option);
                }
                IReadOnlyList<SpawnCatalogOption> entries = slice.Complete();
                totalItems = slice.TotalItems;
                totalPages = slice.TotalPages;
                itemPage = slice.Page;
                currentPage = slice.Page;
                int index = 0;
                foreach (SpawnCatalogOption option in entries)
                {
                    int col = index % columns;
                    int row = index / columns;
                    float pitch = (float)DebugConsoleLayout.CatalogCellPitch;
                    float size = (float)DebugConsoleLayout.CatalogCellSize;
                    BindSpawnCell(
                        option,
                        index,
                        gridX + col * pitch,
                        filterTop - 30 - row * pitch,
                        size,
                        size);
                    index++;
                }
            }
            else if (animalCategory)
            {
                var slice = new CatalogPageAccumulator<AnimalCatalogOption>(
                    itemPage,
                    pageSize);
                foreach (AnimalCatalogOption option in animalEntries)
                {
                    if (!CatalogSourceMatches(
                        normalizedSource,
                        option.SourceId,
                        option.IsModSource) ||
                        !CatalogMatches(
                        normalizedSearch,
                        option.AnimalId,
                        option.DisplayName,
                        option.State.ToString(),
                        option.SourceDisplayName,
                        option.SourceId))
                    {
                        continue;
                    }
                    slice.Add(option);
                }
                IReadOnlyList<AnimalCatalogOption> entries = slice.Complete();
                totalItems = slice.TotalItems;
                totalPages = slice.TotalPages;
                itemPage = slice.Page;
                currentPage = slice.Page;
                int index = 0;
                foreach (AnimalCatalogOption option in entries)
                {
                    int col = index % columns;
                    int row = index / columns;
                    float pitch = (float)DebugConsoleLayout.CatalogCellPitch;
                    float size = (float)DebugConsoleLayout.CatalogCellSize;
                    BindAnimalCell(
                        option,
                        index,
                        gridX + col * pitch,
                        filterTop - 30 - row * pitch,
                        size,
                        size);
                    index++;
                }
            }
            else
            {
                itemPage = page.Page;
                totalItems = page.TotalItems;
                totalPages = page.TotalPages;
                currentPage = page.Page;
                int index = 0;
                foreach (InventoryDebugItem item in page.Items)
                {
                    int col = index % columns;
                    int row = index / columns;
                    float pitch = (float)DebugConsoleLayout.CatalogCellPitch;
                    float size = (float)DebugConsoleLayout.CatalogCellSize;
                    BindItemCell(
                        item,
                        index,
                        gridX + col * pitch,
                        filterTop - 30 - row * pitch,
                        size,
                        size);
                    index++;
                }
            }

            HideUnusedCatalogCells(Math.Min(pageSize, totalItems - currentPage * pageSize));

            AddText(panelRoot!, "DTMAPI.DebugConsole.Items.Count", string.Format(T("debug.catalog.count", "{0} entries | page {1}/{2}"), totalItems, currentPage + 1, totalPages), 14, Color(0.78f, 0.82f, 0.84f, 1f), TextAnchorMiddleLeft, 520, top + 2, 220, 34);
            AddText(panelRoot!, "DTMAPI.DebugConsole.Items.ClickHint", T("debug.catalog.leftRightHint", "Left-click: 1 | Right-click: 10"), 12, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleLeft, 738, top + 2, 242, 34);

            float pitchWidth = (float)DebugConsoleLayout.CatalogCellPitch;
            float pagerY = -(float)(
                (layout?.LogicalHeight ?? 900d) -
                DebugConsoleLayout.CatalogPagerBottomInset);
            CreateButton(panelRoot!, "DTMAPI.DebugConsole.Items.Prev", "<", () =>
            {
                itemPage = Math.Max(0, itemPage - 1);
                dirtyRegions |= UiDirtyRegion.Catalog;
            }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), gridX + Math.Max(0, columns * pitchWidth - 190), pagerY, 46, buttonHeight);
            AddText(panelRoot!, "DTMAPI.DebugConsole.Items.Page", (currentPage + 1) + "/" + totalPages, 13, Color(0.70f, 0.78f, 0.80f, 1f), TextAnchorMiddleCenter, gridX + Math.Max(52, columns * pitchWidth - 138), pagerY, 80, buttonHeight);
            CreateButton(panelRoot!, "DTMAPI.DebugConsole.Items.Next", ">", () =>
            {
                itemPage = Math.Min(totalPages - 1, itemPage + 1);
                dirtyRegions |= UiDirtyRegion.Catalog;
            }, Color(0.13f, 0.15f, 0.17f, 1f), Color(1f, 1f, 1f, 1f), gridX + Math.Max(138, columns * pitchWidth - 52), pagerY, 46, buttonHeight);
        }

        private InventoryDebugSourceGroup[] GetUnifiedCatalogSources(
            IReadOnlyList<InventoryDebugSourceGroup> itemSources,
            IReadOnlyList<SpawnCatalogOption> selectedMonsterEntries,
            IReadOnlyList<AnimalCatalogOption> selectedAnimalEntries)
        {
            if (unifiedCatalogSources != null)
                return unifiedCatalogSources;
            IReadOnlyList<SpawnCatalogOption> monsterEntries =
                selectedMonsterEntries.Count > 0
                    ? selectedMonsterEntries
                    : advancedApi?.GetMonsterCatalog() ??
                        Array.Empty<SpawnCatalogOption>();
            IReadOnlyList<AnimalCatalogOption> animalEntries =
                selectedAnimalEntries.Count > 0
                    ? selectedAnimalEntries
                    : advancedApi?.GetAnimalCatalog() ??
                        Array.Empty<AnimalCatalogOption>();
            unifiedCatalogSources = BuildUnifiedCatalogSources(
                itemSources,
                monsterEntries,
                animalEntries);
            return unifiedCatalogSources;
        }

        private static bool CatalogMatches(
            string search,
            string value1,
            string value2,
            string value3)
        {
            return search.Length == 0 ||
                ContainsCatalogSearch(value1, search) ||
                ContainsCatalogSearch(value2, search) ||
                ContainsCatalogSearch(value3, search);
        }

        private static bool CatalogMatches(
            string search,
            string value1,
            string value2,
            string value3,
            string value4,
            string value5)
        {
            return CatalogMatches(search, value1, value2, value3) ||
                ContainsCatalogSearch(value4, search) ||
                ContainsCatalogSearch(value5, search);
        }

        private static bool ContainsCatalogSearch(
            string value,
            string search) =>
            (value ?? string.Empty).IndexOf(
                search,
                StringComparison.OrdinalIgnoreCase) >= 0;

        private static bool CatalogSourceMatches(
            string selected,
            string optionSourceId,
            bool isModSource)
        {
            if (selected.Length == 0)
                return true;
            if (selected.Equals(
                    SourceFilterBase,
                    StringComparison.OrdinalIgnoreCase))
            {
                return !isModSource;
            }
            if (selected.Equals(
                    SourceFilterMods,
                    StringComparison.OrdinalIgnoreCase))
            {
                return isModSource;
            }
            return isModSource &&
                (optionSourceId ?? string.Empty).Equals(
                    selected,
                    StringComparison.OrdinalIgnoreCase);
        }

        private InventoryDebugSourceGroup[] BuildUnifiedCatalogSources(
            IReadOnlyList<InventoryDebugSourceGroup> itemSources,
            IReadOnlyCollection<SpawnCatalogOption> monsterEntries,
            IReadOnlyCollection<AnimalCatalogOption> animalEntries)
        {
            var sources = (itemSources ??
                    Array.Empty<InventoryDebugSourceGroup>())
                .Select(CloneSourceGroup)
                .ToList();
            AnimalCatalogOption[] modAnimals = animalEntries
                .Where(option => option.IsModSource)
                .ToArray();
            AddCatalogSourceCount(
                sources,
                new InventoryDebugSourceGroup
                {
                    Id = SourceFilterBase,
                    DisplayName = T("debug.items.sourceBase", "Base"),
                    SourceKind = "Vanilla"
                },
                monsterEntries.Count +
                    animalEntries.Count(option => !option.IsModSource));
            AddCatalogSourceCount(
                sources,
                new InventoryDebugSourceGroup
                {
                    Id = SourceFilterMods,
                    DisplayName = T("debug.items.sourceMods", "Mods"),
                    SourceKind = "Mod",
                    IsModSource = true,
                    Enabled = modAnimals.All(option =>
                        option.SourceEnabled),
                    EnablementKnown = modAnimals.All(option =>
                        option.SourceEnablementKnown)
                },
                modAnimals.Length);
            foreach (IGrouping<string, AnimalCatalogOption> group in modAnimals
                .Where(option =>
                    !string.IsNullOrWhiteSpace(option.SourceId))
                .GroupBy(
                    option => option.SourceId,
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    group => group.First().SourceDisplayName,
                    StringComparer.OrdinalIgnoreCase))
            {
                AnimalCatalogOption first = group.First();
                AddCatalogSourceCount(
                    sources,
                    new InventoryDebugSourceGroup
                    {
                        Id = group.Key,
                        DisplayName = FirstText(
                            first.SourceDisplayName,
                            group.Key),
                        SourceKind = FirstText(
                            first.SourceKind,
                            "DTMAPI"),
                        IsModSource = true,
                        Enabled = group.All(option =>
                            option.SourceEnabled),
                        EnablementKnown = group.All(option =>
                            option.SourceEnablementKnown),
                        WorkshopId = first.WorkshopId
                    },
                    group.Count());
            }
            return sources.ToArray();
        }

        private static InventoryDebugSourceGroup CloneSourceGroup(
            InventoryDebugSourceGroup source) =>
            new InventoryDebugSourceGroup
            {
                Id = source.Id,
                DisplayName = source.DisplayName,
                SourceKind = source.SourceKind,
                IsModSource = source.IsModSource,
                Count = source.Count,
                Enabled = source.Enabled,
                EnablementKnown = source.EnablementKnown,
                WorkshopId = source.WorkshopId
            };

        private static void AddCatalogSourceCount(
            List<InventoryDebugSourceGroup> sources,
            InventoryDebugSourceGroup candidate,
            int additionalCount)
        {
            InventoryDebugSourceGroup? existing = sources.FirstOrDefault(
                source => source.Id.Equals(
                    candidate.Id,
                    StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                candidate.Count = Math.Max(0, additionalCount);
                sources.Add(candidate);
                return;
            }
            existing.Count += Math.Max(0, additionalCount);
            existing.Enabled = existing.Enabled && candidate.Enabled;
            existing.EnablementKnown =
                existing.EnablementKnown && candidate.EnablementKnown;
            if (string.IsNullOrWhiteSpace(existing.DisplayName))
                existing.DisplayName = candidate.DisplayName;
            if (string.IsNullOrWhiteSpace(existing.SourceKind))
                existing.SourceKind = candidate.SourceKind;
            if (!existing.WorkshopId.HasValue)
                existing.WorkshopId = candidate.WorkshopId;
        }

        private void BuildDebugSidePanel(
            float x,
            bool includeWorld,
            bool includeAdvanced)
        {
            bool wide = layout?.Breakpoint == DebugConsoleBreakpoint.Wide;
            float sidePanelWidth = wide
                ? 420f
                : Math.Max(
                    420f,
                    (float)(layout?.LogicalWidth ?? 900d) - 56f);
            float minimumButton = (float)(layout?.MinimumClickSize ?? 44d);
            float controlHeight = Math.Max(36f, minimumButton);
            float cursorY = wide ? -92f : -142f;
            if (includeWorld)
            {
                AddSectionLabel(
                    panelRoot!,
                    "DTMAPI.DebugConsole.Time.Title",
                    T("debug.section.time", "Time"),
                    x,
                    cursorY,
                    68,
                    28);
                if (timeApi == null)
                {
                    AddText(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Time.Missing",
                        T("debug.missing.time", "Time debug API is not available."),
                        13,
                        Color(1f, 0.72f, 0.55f, 1f),
                        TextAnchorMiddleLeft,
                        x + 70,
                        cursorY,
                        sidePanelWidth - 70,
                        28);
                }
                else
                {
                    TimeDebugState time = timeApi.GetState();
                    string timeLabel = string.Format(
                        T("debug.time.state", "{0}/{1}/{2} {3:00}:{4:00}  {5}"),
                        time.Year,
                        time.Month,
                        time.Day,
                        time.Hour,
                        time.Minute,
                        FirstText(time.CurrentWeatherName, time.CurrentWeatherId));
                    AddText(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Time.State",
                        timeLabel,
                        13,
                        Color(0.90f, 0.96f, 0.96f, 1f),
                        TextAnchorMiddleLeft,
                        x + 70,
                        cursorY,
                        sidePanelWidth - 70,
                        28);
                    float timeActionY = cursorY - 28f;
                    AddText(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Time.Period",
                        time.Period,
                        12,
                        Color(0.70f, 0.78f, 0.80f, 1f),
                        TextAnchorMiddleLeft,
                        x,
                        timeActionY,
                        sidePanelWidth - 220f,
                        controlHeight);
                    float saveWidth = Math.Max(112f, minimumButton);
                    float nextWidth = Math.Max(96f, minimumButton);
                    float nextX = x + sidePanelWidth - nextWidth;
                    if (instantSaveApi != null)
                    {
                        InstantSaveDebugState save = instantSaveApi.GetState();
                        string saveLabel = saveConfirmationArmed &&
                            DateTimeOffset.UtcNow <= saveConfirmationExpiresAtUtc
                                ? T("debug.save.confirm", "Confirm save")
                                : T("debug.save.here", "Save here");
                        float saveX = x + sidePanelWidth - saveWidth;
                        nextX = saveX - nextWidth - 4f;
                        CreateButton(
                            panelRoot!,
                            "DTMAPI.DebugConsole.Save.Here",
                            saveLabel,
                            SaveHere,
                            save.CanSave
                                ? Color(0.12f, 0.40f, 0.30f, 1f)
                                : Color(0.18f, 0.18f, 0.18f, 1f),
                            save.CanSave
                                ? Color(1f, 1f, 1f, 1f)
                                : Color(0.55f, 0.60f, 0.62f, 1f),
                            saveX,
                            timeActionY,
                            saveWidth,
                            controlHeight);
                    }
                    CreateButton(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Time.Next",
                        T("debug.time.next", "Next period"),
                        SkipTime,
                        Color(0.12f, 0.28f, 0.42f, 1f),
                        Color(1f, 1f, 1f, 1f),
                        nextX,
                        timeActionY,
                        nextWidth,
                        controlHeight);
                }

                cursorY -= 28f + controlHeight + 2f;
                AddSectionLabel(
                    panelRoot!,
                    "DTMAPI.DebugConsole.Speed.Title",
                    T("debug.section.speed", "Move"),
                    x,
                    cursorY,
                    62,
                    28);
                MovementDebugState speed = movementApi == null
                    ? new MovementDebugState()
                    : movementApi.GetState();
                AddText(
                    panelRoot!,
                    "DTMAPI.DebugConsole.Speed.State",
                    string.Format(
                        T("debug.speed.state", "Speed {0:0.#}x"),
                        speed.Multiplier),
                    13,
                    Color(0.90f, 0.96f, 0.96f, 1f),
                    TextAnchorMiddleLeft,
                    x + 64f,
                    cursorY,
                    116f,
                    controlHeight);
                double[] multipliers = { 1, 2, 3, 4 };
                float speedGap = 4f;
                float speedButtonWidth = Math.Max(44f, minimumButton);
                float speedButtonsWidth =
                    multipliers.Length * speedButtonWidth +
                    (multipliers.Length - 1) * speedGap;
                float speedStartX = x + sidePanelWidth - speedButtonsWidth;
                for (int i = 0; i < multipliers.Length; i++)
                {
                    double value = multipliers[i];
                    CreateButton(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Speed." + value.ToString("0.#"),
                        value.ToString("0.#") + "x",
                        () => SetMovementSpeed(value),
                        Math.Abs(speed.Multiplier - value) < 0.01
                            ? Color(0.10f, 0.36f, 0.34f, 1f)
                            : Color(0.13f, 0.15f, 0.17f, 1f),
                        Color(1f, 1f, 1f, 1f),
                        speedStartX + i * (speedButtonWidth + speedGap),
                        cursorY,
                        speedButtonWidth,
                        controlHeight);
                }

                cursorY -= controlHeight + 6f;
                AddSectionLabel(
                    panelRoot!,
                    "DTMAPI.DebugConsole.Weather.Title",
                    T("debug.section.weather", "Weather"),
                    x,
                    cursorY,
                    68,
                    28);
                WeatherPanelSnapshot? weatherSnapshot = null;
                if (weatherApi == null)
                {
                    AddText(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Weather.Missing",
                        T("debug.missing.weather", "Weather debug API is not available."),
                        13,
                        Color(1f, 0.72f, 0.55f, 1f),
                        TextAnchorMiddleLeft,
                        x + 70,
                        cursorY,
                        sidePanelWidth - 70,
                        28);
                }
                else
                {
                    weatherSnapshot = weatherApi.GetPanelSnapshot();
                    WeatherDebugState state = weatherSnapshot.State;
                    string legend =
                        "  ● " + T("debug.weather.badge.current", "Current") +
                        "  ◆ " + T("debug.weather.badge.forecast", "Forecast");
                    AddText(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Weather.State",
                        LocalizeWeatherName(
                            state.CurrentWeatherId,
                            FirstText(
                                state.CurrentWeatherName,
                                T("debug.weather.unknown", "Weather"))) +
                            legend,
                        11,
                        Color(0.90f, 0.96f, 0.96f, 1f),
                        TextAnchorMiddleLeft,
                        x + 70,
                        cursorY,
                        sidePanelWidth - 70,
                        28);
                }

                float weatherButtonsY = cursorY - 28f;
                if (weatherSnapshot != null)
                {
                    WeatherDebugOption[] weathers = weatherSnapshot.Options.ToArray();
                    const int weatherColumns = 7;
                    const float weatherGap = 4f;
                    float weatherWidth =
                        (sidePanelWidth - weatherGap * (weatherColumns - 1)) /
                        weatherColumns;
                    for (int i = 0; i < weathers.Length; i++)
                    {
                        WeatherDebugOption weather = weathers[i];
                        bool available = weatherSnapshot.IsAvailable(weather.Id);
                        string markers =
                            (weather.IsCurrent ? "●" : string.Empty) +
                            (weather.IsCurrentDayForecast ? "◆" : string.Empty) +
                            (!available ? "×" : string.Empty);
                        string label =
                            (markers.Length == 0 ? string.Empty : markers + " ") +
                            LocalizeWeatherName(weather);
                        int weatherIndex = i;
                        CreateButton(
                            panelRoot!,
                            "DTMAPI.DebugConsole.Weather.Set." + weatherIndex,
                            label,
                            () =>
                            {
                                if (available)
                                    SetWeather(weather);
                                else
                                    SetStatusMessage(
                                        T("debug.items.unavailable", "unavailable"),
                                        false);
                            },
                            !available
                                ? Color(0.11f, 0.12f, 0.14f, 1f)
                                : weather.IsCurrent
                                    ? Color(0.10f, 0.36f, 0.34f, 1f)
                                    : weather.IsCurrentDayForecast
                                        ? Color(0.18f, 0.32f, 0.42f, 1f)
                                        : Color(0.12f, 0.28f, 0.42f, 1f),
                            available
                                ? Color(1f, 1f, 1f, 1f)
                                : Color(0.55f, 0.60f, 0.62f, 1f),
                            x + weatherIndex * (weatherWidth + weatherGap),
                            weatherButtonsY,
                            weatherWidth,
                            controlHeight);
                    }
                }

                cursorY = weatherButtonsY - controlHeight - 6f;
                AddSectionLabel(
                    panelRoot!,
                    "DTMAPI.DebugConsole.Teleport.Title",
                    T("debug.section.teleport", "Teleport"),
                    x,
                    cursorY,
                    68,
                    28);
                TeleportDestination[] destinations = Array.Empty<TeleportDestination>();
                if (teleportApi == null)
                {
                    AddText(
                        panelRoot!,
                        "DTMAPI.DebugConsole.Teleport.Missing",
                        T("debug.missing.teleport", "Teleport debug API is not available."),
                        13,
                        Color(1f, 0.72f, 0.55f, 1f),
                        TextAnchorMiddleLeft,
                        x + 70,
                        cursorY,
                        sidePanelWidth - 70,
                        controlHeight);
                }
                else
                {
                    destinations = teleportApi.GetDestinations().ToArray();
                }

                cursorY -= controlHeight + 2f;
                if (destinations.Length > 0)
                {
                    const float teleportGap = 4f;
                    int teleportColumns = wide
                        ? 4
                        : Math.Max(
                            4,
                            Math.Min(
                                8,
                                (int)Math.Floor(
                                    (sidePanelWidth + teleportGap) /
                                    (Math.Max(100f, minimumButton) + teleportGap))));
                    float teleportWidth =
                        (sidePanelWidth -
                            teleportGap * (teleportColumns - 1)) /
                        teleportColumns;
                    for (int i = 0; i < destinations.Length; i++)
                    {
                        TeleportDestination destination = destinations[i];
                        int col = i % teleportColumns;
                        int row = i / teleportColumns;
                        string reason = destination.IsUnlocked
                            ? string.Empty
                            : TranslateTeleportUnavailable(destination.Source);
                        string label =
                            (reason.Length == 0 ? string.Empty : "× ") +
                            LocalizeTeleportName(destination);
                        int destinationIndex = i;
                        CreateButton(
                            panelRoot!,
                            "DTMAPI.DebugConsole.Teleport.Go." + destinationIndex,
                            label,
                            () =>
                            {
                                if (destination.IsUnlocked)
                                    Teleport(destination);
                                else
                                    SetStatusMessage(reason, false);
                            },
                            destination.IsUnlocked
                                ? Color(0.20f, 0.24f, 0.44f, 1f)
                                : Color(0.11f, 0.12f, 0.14f, 1f),
                            destination.IsUnlocked
                                ? Color(1f, 1f, 1f, 1f)
                                : Color(0.55f, 0.60f, 0.62f, 1f),
                            x + col * (teleportWidth + teleportGap),
                            cursorY - row * (controlHeight + teleportGap),
                            teleportWidth,
                            controlHeight);
                    }
                    int teleportRows = (int)Math.Ceiling(
                        destinations.Length / (double)teleportColumns);
                    cursorY -=
                        teleportRows * (controlHeight + teleportGap);
                }

                if (includeAdvanced)
                    cursorY -= 6f;
            }
            if (!includeAdvanced)
                return;

            AddSectionLabel(
                panelRoot!,
                "DTMAPI.DebugConsole.Advanced.Title",
                T("debug.section.advanced", "Advanced"),
                x,
                cursorY,
                86,
                28);
            if (advancedApi == null)
            {
                AddText(
                    panelRoot!,
                    "DTMAPI.DebugConsole.Advanced.Missing",
                    T("debug.missing.advanced", "Advanced debug API is not available."),
                    13,
                    Color(1f, 0.72f, 0.55f, 1f),
                    TextAnchorMiddleLeft,
                    x + 88,
                    cursorY,
                    sidePanelWidth - 88,
                    28);
            }
            else
            {
                CreativeModeState creative = advancedApi.GetCreativeModeState();
                string creativeLabel = creative.Enabled
                    ? T("debug.creative.on", "Creative on")
                    : T("debug.creative.off", "Creative off");
                AddText(
                    panelRoot!,
                    "DTMAPI.DebugConsole.Advanced.State",
                    creativeLabel,
                    12,
                    creative.Enabled
                        ? Color(0.74f, 1f, 0.82f, 1f)
                        : Color(0.70f, 0.78f, 0.80f, 1f),
                    TextAnchorMiddleLeft,
                    x + 88,
                    cursorY,
                    sidePanelWidth - 88,
                    28);
                float actionX = x;
                float actionY = cursorY - 28f;
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.Day", T("debug.advanced.day", "+1 day"), () => AdvanceAdvancedTime(AdvancedTimeAdvanceKind.Day, 1), Color(0.12f, 0.28f, 0.42f, 1f), 60f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.Week", T("debug.advanced.week", "+1 week"), () => AdvanceAdvancedTime(AdvancedTimeAdvanceKind.Week, 1), Color(0.12f, 0.28f, 0.42f, 1f), 66f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.Month", T("debug.advanced.month", "+1 month"), () => AdvanceAdvancedTime(AdvancedTimeAdvanceKind.Month, 1), Color(0.12f, 0.28f, 0.42f, 1f), 76f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.Scale4", T("debug.advanced.scale4", "4x time"), () => SetDebugTimeScale(4), Color(0.13f, 0.15f, 0.17f, 1f), 60f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.ScaleReset", T("debug.advanced.scaleReset", "1x"), () => SetDebugTimeScale(1), Color(0.13f, 0.15f, 0.17f, 1f), 48f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.Money", T("debug.advanced.money", "+money"), () => AddDebugMoney(10000), Color(0.14f, 0.34f, 0.20f, 1f), 72f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.TechPoint", T("debug.advanced.techPoint", "+tech"), AddTechnologyPoints, Color(0.18f, 0.28f, 0.44f, 1f), 66f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.UnlockTech", T("debug.advanced.unlockTech", "Tech tree"), UnlockAllTechTrees, Color(0.18f, 0.28f, 0.44f, 1f), 80f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.MatureCrops", T("debug.advanced.matureCrops", "Crops"), MatureAllCrops, Color(0.17f, 0.32f, 0.22f, 1f), 60f, controlHeight);
                CreateWrappedActionButton(ref actionX, ref actionY, x, x + sidePanelWidth, "DTMAPI.DebugConsole.Advanced.Creative", creative.Enabled ? T("debug.advanced.creativeOff", "Creative off") : T("debug.advanced.creativeOn", "Creative on"), ToggleCreativeMode, creative.Enabled ? Color(0.36f, 0.24f, 0.12f, 1f) : Color(0.17f, 0.32f, 0.22f, 1f), 72f, controlHeight);
            }
        }

        private void CreateWrappedActionButton(
            ref float cursorX,
            ref float cursorY,
            float startX,
            float rightEdge,
            string name,
            string label,
            Action action,
            object background,
            float preferredWidth,
            float height)
        {
            const float gap = 6f;
            float width = Math.Max(
                preferredWidth,
                (float)(layout?.MinimumClickSize ?? 44d));
            if (cursorX > startX && cursorX + width > rightEdge)
            {
                cursorX = startX;
                cursorY -= height + gap;
            }
            CreateButton(
                panelRoot!,
                name,
                label,
                action,
                background,
                Color(1f, 1f, 1f, 1f),
                cursorX,
                cursorY,
                width,
                height);
            cursorX += width + gap;
        }

        private void EnsureCatalogCellPool(int capacity)
        {
            if (catalogGridRoot == null)
                return;
            List<object>? previousBinderSink = activeBinderSink;
            activeBinderSink = catalogPoolBinders;
            try
            {
                while (catalogCells.Count < capacity)
                    catalogCells.Add(CreateCatalogCell(catalogGridRoot, catalogCells.Count));
            }
            finally
            {
                activeBinderSink = previousBinderSink;
            }
        }

        private CatalogCell CreateCatalogCell(object parent, int index)
        {
            object rootObject = CreateUiObject("DTMAPI.DebugConsole.CatalogCell." + index, parent);
            object background = AddComponent(rootObject, imageType!);
            object button = AddComponent(rootObject, buttonType!);
            SetProperty(button, "targetGraphic", background);
            ActionBinder click = AddButtonRelay(button);

            object iconRoot = CreateUiObject("DTMAPI.DebugConsole.CatalogCell.Icon." + index, rootObject);
            object iconImage = AddComponent(iconRoot, imageType!);
            SetProperty(iconImage, "preserveAspect", true);
            SetProperty(iconImage, "raycastTarget", false);
            SetRect(iconRoot, Vector2(0, 1), Vector2(0, 1), Vector2(0, 1), Vector2(16, -4), Vector2(52, 42));

            object fallbackText = AddText(rootObject, "DTMAPI.DebugConsole.CatalogCell.Fallback." + index, "-", 24, Color(0.55f, 0.62f, 0.64f, 1f), TextAnchorMiddleCenter, 8, -5, 68, 42);
            object labelText = AddText(rootObject, "DTMAPI.DebugConsole.CatalogCell.Label." + index, string.Empty, 10, Color(1f, 1f, 1f, 1f), TextAnchorMiddleCenter, 4, -47, 76, 34);
            object sourceText = AddText(rootObject, "DTMAPI.DebugConsole.CatalogCell.Source." + index, string.Empty, 9, Color(0.72f, 0.80f, 0.80f, 1f), TextAnchorMiddleCenter, 4, -69, 76, 12);
            object statusText = AddText(rootObject, "DTMAPI.DebugConsole.CatalogCell.Status." + index, string.Empty, 9, Color(1f, 0.68f, 0.50f, 1f), TextAnchorMiddleCenter, 58, -3, 23, 14);
            SetEnumProperty(labelText, "verticalOverflow", 0);
            SetEnumProperty(sourceText, "verticalOverflow", 0);
            SetEnumProperty(statusText, "verticalOverflow", 0);

            object trigger = AddComponent(rootObject, eventTriggerType!);
            PointerActionBinder pointerDown = AddPointerRelay(trigger, "PointerDown", out bool rightClickBound);
            PointerActionBinder pointerEnter = AddPointerRelay(trigger, "PointerEnter", out _);
            PointerActionBinder pointerExit = AddPointerRelay(trigger, "PointerExit", out _);
            RecordRightClickBindingStatus(rightClickBound);
            SetActive(rootObject, false);
            return new CatalogCell(
                this,
                rootObject,
                background,
                click,
                iconRoot,
                iconImage,
                fallbackText,
                GetProperty(fallbackText, "gameObject"),
                labelText,
                GetProperty(labelText, "gameObject"),
                sourceText,
                GetProperty(sourceText, "gameObject"),
                statusText,
                GetProperty(statusText, "gameObject"),
                pointerDown,
                pointerEnter,
                pointerExit);
        }

        private void BindItemCell(InventoryDebugItem item, int index, float x, float y, float w, float h)
        {
            CatalogCell cell = catalogCells[index];
            string name = FirstText(item.DisplayName, item.ChineseName, item.EnglishName, item.Id);
            string hover = FormatItemHover(item);
            float tooltipX = x > 700 ? x - 356 : x + w + 10;
            cell.BindItem(item, hover, tooltipX, y);
            BindCatalogVisual(
                cell,
                ResolveItemSprite(item.Id),
                name,
                item.IsModItem ? Truncate(FirstText(item.SourceModTitle, item.SourceId, item.SourceKind), 12) : string.Empty,
                !item.RuntimeLoaded ? T("debug.items.notLoaded.short", "off") : string.Empty,
                item.HasIcon ? "?" : "-",
                item.CanGive ? Color(0.070f, 0.083f, 0.092f, 1f) : Color(0.055f, 0.055f, 0.060f, 1f),
                item.CanGive ? Color(1f, 1f, 1f, 1f) : Color(0.55f, 0.60f, 0.62f, 1f),
                x,
                y,
                w,
                h);
        }

        private void BindSpawnCell(SpawnCatalogOption option, int index, float x, float y, float w, float h)
        {
            CatalogCell cell = catalogCells[index];
            string hover = option.IsAvailable
                ? option.DisplayName + " / " + option.Id
                : TranslateSpawnUnavailable(option.UnavailableReason, isAnimal: false);
            cell.BindMonster(option, hover);
            BindCatalogVisual(
                cell,
                option.Icon,
                option.DisplayName,
                string.Empty,
                option.IsAvailable ? string.Empty : T("debug.items.notLoaded.short", "off"),
                "?",
                option.IsAvailable ? Color(0.080f, 0.075f, 0.090f, 1f) : Color(0.055f, 0.055f, 0.060f, 1f),
                option.IsAvailable ? Color(1f, 1f, 1f, 1f) : Color(0.55f, 0.60f, 0.62f, 1f),
                x,
                y,
                w,
                h);
        }

        private void BindAnimalCell(AnimalCatalogOption option, int index, float x, float y, float w, float h)
        {
            CatalogCell cell = catalogCells[index];
            string state = AnimalStateName(option.State);
            string hover = option.IsAvailable
                ? option.DisplayName + " / " + state + " / " + option.AnimalId
                : TranslateSpawnUnavailable(option.UnavailableReason, isAnimal: true);
            cell.BindAnimal(option, hover);
            BindCatalogVisual(
                cell,
                option.Icon,
                option.DisplayName + "\n" + state,
                string.Empty,
                option.IsAvailable ? string.Empty : T("debug.items.notLoaded.short", "off"),
                "?",
                option.IsAvailable ? Color(0.070f, 0.090f, 0.075f, 1f) : Color(0.055f, 0.055f, 0.060f, 1f),
                option.IsAvailable ? Color(1f, 1f, 1f, 1f) : Color(0.55f, 0.60f, 0.62f, 1f),
                x,
                y,
                w,
                h);
        }

        private void BindCatalogVisual(
            CatalogCell cell,
            object? sprite,
            string label,
            string source,
            string status,
            string fallback,
            object background,
            object textColor,
            float x,
            float y,
            float w,
            float h)
        {
            if (!object.Equals(cell.LastBackground, background))
            {
                SetProperty(cell.Background, "color", background);
                cell.LastBackground = background;
            }
            if (!ReferenceEquals(cell.LastSprite, sprite))
            {
                SetProperty(cell.IconImage, "sprite", sprite);
                cell.LastSprite = sprite;
            }
            SetActive(cell.IconRoot, sprite != null);
            if (!cell.LastFallback.Equals(fallback, StringComparison.Ordinal))
            {
                SetProperty(cell.FallbackText, "text", fallback);
                cell.LastFallback = fallback;
            }
            SetActive(cell.FallbackRoot, sprite == null);
            if (!cell.LastLabel.Equals(label, StringComparison.Ordinal))
            {
                SetProperty(cell.LabelText, "text", label);
                cell.LastLabel = label;
            }
            if (!object.Equals(cell.LastTextColor, textColor))
            {
                SetProperty(cell.LabelText, "color", textColor);
                cell.LastTextColor = textColor;
            }
            bool sourceChanged = !cell.LastSource.Equals(
                source,
                StringComparison.Ordinal);
            if (sourceChanged)
            {
                SetProperty(cell.SourceText, "text", source);
                cell.LastSource = source;
            }
            SetActive(cell.SourceRoot, source.Length > 0);
            if (sourceChanged && cell.LabelRoot != null)
            {
                SetRect(
                    cell.LabelRoot,
                    Vector2(0, 1),
                    Vector2(0, 1),
                    Vector2(0, 1),
                    Vector2(4, -47),
                    Vector2(76, source.Length > 0 ? 20 : 34));
            }
            if (sourceChanged && cell.SourceRoot != null)
            {
                SetRect(
                    cell.SourceRoot,
                    Vector2(0, 1),
                    Vector2(0, 1),
                    Vector2(0, 1),
                    Vector2(4, -69),
                    Vector2(76, 12));
            }
            if (!cell.LastStatus.Equals(status, StringComparison.Ordinal))
            {
                SetProperty(cell.StatusText, "text", status);
                cell.LastStatus = status;
            }
            SetActive(cell.StatusRoot, status.Length > 0);
            float minimum = (float)(layout?.MinimumClickSize ?? h);
            float side = Math.Max(Math.Min(w, h), minimum);
            if (!cell.LayoutAssigned ||
                cell.LastX != x ||
                cell.LastY != y ||
                cell.LastSide != side)
            {
                SetRect(
                    cell.Root,
                    Vector2(0, 1),
                    Vector2(0, 1),
                    Vector2(0, 1),
                    Vector2(x, y),
                    Vector2(side, side));
                cell.LayoutAssigned = true;
                cell.LastX = x;
                cell.LastY = y;
                cell.LastSide = side;
            }
            SetActive(cell.Root, true);
        }

        private void HideUnusedCatalogCells(int visibleCount)
        {
            for (int index = Math.Max(0, visibleCount); index < catalogCells.Count; index++)
            {
                CatalogCell cell = catalogCells[index];
                cell.ClearContext();
                SetActive(cell.Root, false);
            }
        }

        private void ClearRetainedCatalogCells()
        {
            foreach (CatalogCell cell in catalogCells)
            {
                cell.ClearContext();
                SetProperty(cell.IconImage, "sprite", null);
                SetProperty(cell.FallbackText, "text", string.Empty);
                SetProperty(cell.LabelText, "text", string.Empty);
                SetProperty(cell.SourceText, "text", string.Empty);
                SetProperty(cell.StatusText, "text", string.Empty);
                SetActive(cell.IconRoot, false);
                SetActive(cell.FallbackRoot, false);
                SetActive(cell.SourceRoot, false);
                SetActive(cell.StatusRoot, false);
                SetActive(cell.Root, false);
                cell.LastSprite = null;
                cell.LastFallback = string.Empty;
                cell.LastLabel = string.Empty;
                cell.LastSource = string.Empty;
                cell.LastStatus = string.Empty;
            }
        }

        private static void NoOp()
        {
        }

        private static void NoOpPointer(object? value)
        {
        }

        private sealed class CatalogPageAccumulator<T>
            where T : class
        {
            private readonly int requestedPage;
            private readonly int pageSize;
            private readonly List<T> requestedItems;
            private readonly T?[] tail;
            private int tailCount;
            private int tailNext;

            internal CatalogPageAccumulator(int page, int size)
            {
                requestedPage = Math.Max(0, page);
                pageSize = Math.Max(1, size);
                requestedItems = new List<T>(pageSize);
                tail = new T?[pageSize];
            }

            internal int Page { get; private set; }
            internal int TotalItems { get; private set; }
            internal int TotalPages { get; private set; } = 1;

            internal void Add(T item)
            {
                long index = TotalItems;
                TotalItems++;
                long requestedStart = (long)requestedPage * pageSize;
                if (index >= requestedStart &&
                    requestedItems.Count < pageSize)
                {
                    requestedItems.Add(item);
                }
                tail[tailNext] = item;
                tailNext = (tailNext + 1) % pageSize;
                if (tailCount < pageSize)
                    tailCount++;
            }

            internal IReadOnlyList<T> Complete()
            {
                TotalPages = Math.Max(
                    1,
                    (int)Math.Ceiling(TotalItems / (double)pageSize));
                Page = Math.Min(requestedPage, TotalPages - 1);
                if (Page == requestedPage)
                    return requestedItems;

                int resolvedCount = TotalItems - Page * pageSize;
                var resolved = new T[resolvedCount];
                int oldest = tailCount < pageSize ? 0 : tailNext;
                int skip = tailCount - resolvedCount;
                for (int index = 0; index < resolvedCount; index++)
                {
                    resolved[index] = tail[
                        (oldest + skip + index) % pageSize]!;
                }
                return resolved;
            }
        }

    }
}
