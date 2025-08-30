using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using ArchitectIcons;
using MainTabWindow_Architect = RimWorld.MainTabWindow_Architect;

namespace BetterArchitect
{
    [StaticConstructorOnStartup]
    [HotSwappable]
    [HarmonyPatch(typeof(ArchitectCategoryTab), nameof(ArchitectCategoryTab.DesignationTabOnGUI))]
    public static class ArchitectCategoryTab_DesignationTabOnGUI_Patch
    {
        private static readonly Dictionary<DesignationCategoryDef, DesignationCategoryDef> selectedCategory = new Dictionary<DesignationCategoryDef, DesignationCategoryDef>();
        private static Vector2 leftPanelScrollPosition, designatorGridScrollPosition, ordersScrollPosition;
        private static DesignationCategoryDef lastMainCategory;
        public static MaterialInfo selectedMaterial;
        private static readonly Texture2D GroupingIcon = ContentFinder<Texture2D>.Get("GroupType");
        private static readonly Texture2D SortType = ContentFinder<Texture2D>.Get("SortType");
        private static readonly Texture2D AscendingIcon = ContentFinder<Texture2D>.Get("SortAscend");
        private static readonly Texture2D DescendingIcon = ContentFinder<Texture2D>.Get("SortDescend");
        private static readonly Texture2D FreeIcon = ContentFinder<Texture2D>.Get("UI/Free");
        private static readonly Texture2D MoreIcon = ContentFinder<Texture2D>.Get("More");
        private static bool IsSpecialCategory(DesignationCategoryDef cat)
        {
            return cat == DefsOf.Orders || cat == DesignationCategoryDefOf.Zone || cat.defName == "Blueprints";
        }

        private static (List<Designator> buildables, List<Designator> orders) SeparateDesignatorsByType(IEnumerable<Designator> allDesignators, DesignationCategoryDef category)
        {
            var buildables = new List<Designator>();
            var orders = new List<Designator>();
            foreach (var designator in allDesignators)
            {
                if (designator is Designator_Build || (designator is Designator_Dropdown dd && dd.Elements.Any(e => e is Designator_Build)))
                {
                    buildables.Add(designator);
                }
                else
                {
                    orders.Add(designator);
                }
            }
            if (IsSpecialCategory(category))
            {
                buildables.AddRange(orders);
                orders.Clear();
            }
            return (buildables, orders);
        }

        public static bool Prefix(ArchitectCategoryTab __instance)
        {
            if (BetterArchitectSettings.hideOnSelection && Find.DesignatorManager.SelectedDesignator != null)
            {
                if (Find.DesignatorManager.SelectedDesignator != null)
                {
                    Find.DesignatorManager.SelectedDesignator.DoExtraGuiControls(0f, (float)(UI.screenHeight - 35) - ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).WinHeight - 270f);
                }
                DoInfoBox(Find.DesignatorManager.SelectedDesignator);
                return false;
            }
            if (lastMainCategory != __instance.def)
            {
                leftPanelScrollPosition = designatorGridScrollPosition = ordersScrollPosition = Vector2.zero;
                lastMainCategory = __instance.def;
            }
            DrawBetterArchitectMenu(__instance);
            return false;
        }

        private static void DrawBetterArchitectMenu(ArchitectCategoryTab tab)
        {
            if (Find.DesignatorManager.SelectedDesignator != null)
            {
                Find.DesignatorManager.SelectedDesignator.DoExtraGuiControls(0f, (float)(UI.screenHeight - 35) - ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).WinHeight - 270f);
            }
            var menuHeight = BetterArchitectSettings.menuHeight;
            var leftWidth = 200f;
            var ordersWidth = 175f;
            var gizmoSize = 75f;
            var gizmoSpacing = 5f;
            var availableWidth = UI.screenWidth - 195f - (((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).RequestedTabSize.x + 10f) - leftWidth - ordersWidth;
            var gizmosPerRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (gizmoSize + gizmoSpacing)));
            var gridWidth = gizmosPerRow * (gizmoSize + gizmoSpacing) + gizmoSpacing + 11;
            var mainRect = new Rect(
                ((MainTabWindow_Architect)MainButtonDefOf.Architect.TabWindow).RequestedTabSize.x + 10f,
                UI.screenHeight - menuHeight - 35f,
                leftWidth + gridWidth + ordersWidth,
                menuHeight);
            var leftRect = new Rect(mainRect.x, mainRect.y + 20f, leftWidth, mainRect.height - 30f);
            var gridRect = new Rect(leftRect.xMax, mainRect.y, gridWidth, mainRect.height);
            var ordersRect = new Rect(gridRect.xMax, mainRect.y + 30f, ordersWidth, mainRect.height - 30f);
            var newColor = new Color(Color.white.r, Color.white.g, Color.white.b, 1f - BetterArchitectSettings.backgroundAlpha);
            Widgets.DrawWindowBackground(mainRect, newColor);
            var allCategories = DefDatabase<DesignationCategoryDef>.AllDefsListForReading
                .Where(d => d.GetModExtension<NestedCategoryExtension>()?.parentCategory == tab.def).ToList();
            allCategories.Add(tab.def);
            var designatorDataList = new List<DesignatorCategoryData>();
            foreach (var category in allCategories)
            {
                var allDesignators = category.ResolvedAllowedDesignators.Where(d => d.Visible).ToList();
                var (buildables, orders) = SeparateDesignatorsByType(allDesignators, category);
                designatorDataList.Add(new DesignatorCategoryData(category, category == tab.def, allDesignators, buildables, orders));
            }

            List<Designator> designatorsToDisplay;
            List<Designator> orderDesignators;
            DesignationCategoryDef categoryForGridAndOrders;

            if (tab.def == DesignationCategoryDefOf.Floors)
            {
                Designator_Build_ProcessInput_Transpiler.shouldSkipFloatMenu = true;
                var floorData = designatorDataList.FirstOrDefault(d => d.def == tab.def);
                var floorSpecificDesignators = new List<Designator>();
                var orderSpecificDesignators = new List<Designator>();
                foreach (var designator in floorData.allDesignators)
                {
                    if (designator is Designator_Dropdown dropdown)
                    {
                        if (dropdown.Elements.OfType<Designator_Place>().Any(x => x.PlacingDef.designatorDropdown?.includeEyeDropperTool == true))
                        {
                            floorSpecificDesignators.Add(dropdown);
                        }
                        else
                        {
                            floorSpecificDesignators.AddRange(dropdown.Elements.OfType<Designator_Build>());
                        }
                    }
                    else if (designator is Designator_Build || designator is Designator_Place)
                    {
                        floorSpecificDesignators.Add(designator);
                    }
                    else
                    {
                        orderSpecificDesignators.Add(designator);
                    }
                }
                orderDesignators = orderSpecificDesignators;
                designatorsToDisplay = DrawMaterialListForFloors(leftRect, floorSpecificDesignators);
                categoryForGridAndOrders = tab.def;
            }
            else
            {
                Designator_Build_ProcessInput_Transpiler.shouldSkipFloatMenu = false;
                var selectedCategory = HandleCategorySelection(leftRect, tab.def, designatorDataList);
                var selectedData = designatorDataList.FirstOrDefault(d => d.def == selectedCategory);
                designatorsToDisplay = selectedData.buildables;
                orderDesignators = selectedData.orders;
                categoryForGridAndOrders = selectedCategory;
            }

            var mouseoverGizmo = DrawDesignatorGrid(gridRect, categoryForGridAndOrders, designatorsToDisplay);
            var orderGizmo = DrawOrdersPanel(ordersRect, orderDesignators);
            if (orderGizmo != null) mouseoverGizmo = orderGizmo;
            DoInfoBox(mouseoverGizmo ?? Find.DesignatorManager.SelectedDesignator);
            if (Event.current.type == EventType.MouseDown && Mouse.IsOver(mainRect)) Event.current.Use();
        }

        private static DesignationCategoryDef HandleCategorySelection(Rect rect, DesignationCategoryDef mainCat, List<DesignatorCategoryData> designatorDataList)
        {
            var allCategories = designatorDataList.Select(d => d.def).ToList();
            selectedCategory.TryGetValue(mainCat, out var currentSelection);

            var mainCategoryData = designatorDataList.FirstOrDefault(d => d.def == mainCat);
            var mainCategoryHasDesignators = mainCategoryData != null && mainCategoryData.buildables.Any(x => x is Designator_Place || x is Designator_Dropdown);
            var subCategories = allCategories.Where(c => c != mainCat).ToList();
            var filteredSubCategories = new List<DesignationCategoryDef>();

            foreach (var cat in subCategories)
            {
                if (IsSpecialCategory(cat))
                {
                    filteredSubCategories.Add(cat);
                }
                else
                {
                    var categoryData = designatorDataList.FirstOrDefault(d => d.def == cat);
                    if (categoryData != null && !categoryData.buildables.NullOrEmpty())
                    {
                        filteredSubCategories.Add(cat);
                    }
                }
            }

            filteredSubCategories = filteredSubCategories.OrderByDescending(cat => cat.order).ToList();
            bool shouldHideMoreCategory = false;
            if (filteredSubCategories.Any())
            {
                bool subCategoriesHaveBuildings = filteredSubCategories.Any(cat =>
                {
                    if (IsSpecialCategory(cat)) return true;
                    var categoryData = designatorDataList.FirstOrDefault(d => d.def == cat);
                    return categoryData != null && !categoryData.buildables.NullOrEmpty();
                });

                shouldHideMoreCategory = subCategoriesHaveBuildings && !mainCategoryHasDesignators;
            }
            if (shouldHideMoreCategory) allCategories.Remove(mainCat);
            var displayCategories = new List<DesignationCategoryDef>();
            displayCategories.AddRange(filteredSubCategories);
            if (!shouldHideMoreCategory) displayCategories.Add(mainCat);
            if (currentSelection == null || !allCategories.Contains(currentSelection))
            {
                if (displayCategories.Any())
                {
                    currentSelection = displayCategories.First();
                }
                else
                {
                    currentSelection = mainCat;
                }
                selectedCategory[mainCat] = currentSelection;
            }

            var outRect = rect.ContractedBy(10f);
            var viewRect = new Rect(0, 0, outRect.width - 16f, displayCategories.Count * 35f);
            Widgets.BeginScrollView(outRect, ref leftPanelScrollPosition, viewRect);
            float curY = 0;

            foreach (var cat in displayCategories)
            {
                var rowRect = new Rect(0, curY, viewRect.width, 36);
                bool isSelected = currentSelection == cat;
                DrawOptionBackground(rowRect, isSelected);
                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (!isSelected) SoundDefOf.Tick_High.PlayOneShotOnCamera();
                    selectedCategory[mainCat] = cat;
                    currentSelection = cat;
                }
                string label = cat.LabelCap;
                Texture2D icon = ArchitectIcons.Resources.FindArchitectTabCategoryIcon(cat.defName);
                if (cat == mainCat && filteredSubCategories.Any())
                {
                    label = "BA.More".Translate();
                    icon = MoreIcon;
                }

                var iconRect = new Rect(rowRect.x + 4f, rowRect.y + 8f, 20f, 20f);
                if (icon != null) Widgets.DrawTextureFitted(iconRect, icon, 1f);
                Text.Font = GameFont.Small;
                var labelRect = new Rect(iconRect.xMax + 8f, rowRect.y, rowRect.width - iconRect.width - 16f, rowRect.height);
                Text.Anchor = TextAnchor.MiddleLeft; Widgets.Label(labelRect, label); Text.Anchor = TextAnchor.UpperLeft;
                curY += rowRect.height + 5;
            }
            Widgets.EndScrollView();
            return currentSelection;
        }

        private static List<Designator> DrawMaterialListForFloors(Rect rect, List<Designator> allFloorDesignators)
        {
            var floorsByMaterial = new Dictionary<MaterialInfo, List<Designator>>();
            var freeKey = new MaterialInfo("BA.Free".Translate(), FreeIcon, Color.white, null);
            foreach (var designator in allFloorDesignators) PopulateMaterials(floorsByMaterial, freeKey, designator);
            var materials = floorsByMaterial.Keys
                .OrderBy(m => m.def == null ? 5 : (m.def.stuffProps?.categories?.Contains(StuffCategoryDefOf.Woody) == true ? 1 : (m.def.stuffProps?.categories?.Contains(StuffCategoryDefOf.Metallic) == true ? 2 : (m.def.stuffProps?.categories?.Contains(StuffCategoryDefOf.Stony) == true ? 3 : 4))))
                .ThenByDescending(m => m.def?.stuffProps?.commonality ?? float.PositiveInfinity).ThenBy(m => m.def?.BaseMarketValue ?? 0).ToList();
            if ((selectedMaterial == null || !materials.Contains(selectedMaterial)) && materials.Any()) selectedMaterial = materials.First();
            var outRect = rect.ContractedBy(10f);
            var viewRect = new Rect(0, 0, outRect.width - 16f, materials.Count * 35f);
            HandleScrollBar(outRect, viewRect, ref leftPanelScrollPosition);
            Widgets.BeginScrollView(outRect, ref leftPanelScrollPosition, viewRect);
            float curY = 0;
            foreach (var material in materials)
            {
                var rowRect = new Rect(0, curY, viewRect.width, 36);
                DrawOptionBackground(rowRect, selectedMaterial?.def == material.def);
                MouseoverSounds.DoRegion(rowRect);
                var iconRect = new Rect(rowRect.x + 4f, rowRect.y + 8f, 24f, 24f);
                GUI.color = material.color;
                Widgets.DrawTextureFitted(iconRect, material.icon, 1f);
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                var labelRect = new Rect(iconRect.xMax + 8f, rowRect.y, rowRect.width - iconRect.width - 16f, rowRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, material.label);
                Text.Anchor = TextAnchor.UpperLeft;
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedMaterial = material;
                }
                curY += rowRect.height + 5;
            }
            Widgets.EndScrollView();
            var result = (selectedMaterial != null && floorsByMaterial.ContainsKey(selectedMaterial)) ? floorsByMaterial[selectedMaterial] : new List<Designator>();
            if (selectedMaterial?.def != null)
            {
                foreach (var designator in result)
                {
                    if (designator is Designator_Build buildDesignator && buildDesignator.entDef is ThingDef thingDef && thingDef.MadeFromStuff)
                    {
                        buildDesignator.SetStuffDef(selectedMaterial.def);
                    }
                }
            }
            return result;
        }

        private static Designator DrawDesignatorGrid(Rect rect, DesignationCategoryDef mainCat, List<Designator> designators)
        {
            var viewControlsRect = new Rect(rect.xMax - 100f, rect.y, 235f, 28f);
            DrawViewControls(viewControlsRect, mainCat, designators);
            if (!BetterArchitectSettings.sortSettingsPerCategory.ContainsKey(mainCat.defName)) BetterArchitectSettings.sortSettingsPerCategory[mainCat.defName] = new SortSettings();
            var settings = BetterArchitectSettings.sortSettingsPerCategory[mainCat.defName];
            SortDesignators(designators, settings);
            var outRect = new Rect(rect.x, rect.y + 30f, rect.width, rect.height - 30f);
            if (designators.NullOrEmpty())
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                var message = "BA.NothingAvailableToBuild".Translate();
                var textSize = Text.CalcSize(message);
                var messageRect = new Rect(
                    outRect.x + (outRect.width - textSize.x) / 2,
                    outRect.y + (outRect.height - textSize.y) / 2,
                    textSize.x,
                    textSize.y
                );
                Widgets.Label(messageRect, message);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                return null;
            }

            bool groupByTechLevel = BetterArchitectSettings.groupByTechLevelPerCategory.ContainsKey(mainCat.defName) ? BetterArchitectSettings.groupByTechLevelPerCategory[mainCat.defName] : false;
            return groupByTechLevel ? DrawGroupedGrid(outRect, designators) : DrawFlatGrid(outRect, designators);
        }

        private static Designator DrawFlatGrid(Rect rect, List<Designator> designators)
        {
            var gizmoSize = 75f;
            var gizmoSpacing = 5f;
            var availableWidth = rect.width - 16f;
            var gizmosPerRow = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (gizmoSize + gizmoSpacing)));
            var rowCount = Mathf.CeilToInt((float)designators.Count / gizmosPerRow);
            var rowHeight = gizmoSize + gizmoSpacing + 5f;
            var viewRect = new Rect(0, 0, rect.width - 16f, rowCount * rowHeight);
            Designator mouseoverGizmo = null;
            HandleScrollBar(rect, viewRect, ref designatorGridScrollPosition);
            Widgets.BeginScrollView(rect, ref designatorGridScrollPosition, viewRect);
            for (int i = 0; i < designators.Count; i++)
            {
                int row = i / gizmosPerRow;
                int col = i % gizmosPerRow;
                var result = designators[i].GizmoOnGUI(new Vector2(col * (gizmoSize + gizmoSpacing), row * rowHeight), gizmoSize, default);
                if (result.State >= GizmoState.Mouseover) mouseoverGizmo = designators[i];
                if (result.State == GizmoState.Interacted) designators[i].ProcessInput(result.InteractEvent);
            }
            Widgets.EndScrollView();
            return mouseoverGizmo;
        }

        private static Designator DrawGroupedGrid(Rect rect, List<Designator> designators)
        {
            var groupedDesignators = designators.GroupBy(GetTechLevelFor).OrderByDescending(g => (int)g.Key).ToList();
            var gizmoSize = 75f;
            var gizmoSpacing = 5f;
            var rowHeight = gizmoSize + gizmoSpacing + 5f;
            var viewRectWidth = rect.width - 16f;
            var gizmosPerRow = Mathf.Max(1, Mathf.FloorToInt(viewRectWidth / (gizmoSize + gizmoSpacing)));

            float totalHeight = 0f;
            foreach (var group in groupedDesignators)
            {
                totalHeight += 28f;
                var groupItems = group.ToList();
                int rowCount = Mathf.CeilToInt((float)groupItems.Count / gizmosPerRow);
                totalHeight += rowCount * rowHeight + gizmoSpacing;
            }

            var viewRect = new Rect(0, 0, viewRectWidth, totalHeight);
            Designator mouseoverGizmo = null;
            float currentY = 0;
            HandleScrollBar(rect, viewRect, ref designatorGridScrollPosition);
            Widgets.BeginScrollView(rect, ref designatorGridScrollPosition, viewRect);
            foreach (var group in groupedDesignators)
            {
                var headerRect = new Rect(0, currentY, viewRect.width, 24f);
                GUI.color = GetColorFor(group.Key);
                Widgets.Label(headerRect, group.Key.ToStringHuman().CapitalizeFirst());
                GUI.color = Color.white;
                currentY += 20;
                Widgets.DrawLineHorizontal(0, currentY, viewRect.width);
                currentY += 10;
                var groupItems = group.ToList();
                for (int i = 0; i < groupItems.Count; i++)
                {
                    int row = i / gizmosPerRow;
                    int col = i % gizmosPerRow;
                    var result = groupItems[i].GizmoOnGUI(new Vector2(col * (gizmoSize + gizmoSpacing), currentY + row * rowHeight), gizmoSize, default);
                    if (result.State >= GizmoState.Mouseover) mouseoverGizmo = groupItems[i];
                    if (result.State == GizmoState.Interacted) groupItems[i].ProcessInput(result.InteractEvent);
                }
                int rowCount = Mathf.CeilToInt((float)groupItems.Count / gizmosPerRow);
                currentY += rowCount * rowHeight + gizmoSpacing;
            }
            Widgets.EndScrollView();
            return mouseoverGizmo;
        }

        private static void DrawViewControls(Rect rect, DesignationCategoryDef mainCat, List<Designator> designators)
        {
            var groupButtonRect = new Rect(rect.x, rect.y, rect.height, rect.height).ExpandedBy(-4f);
            var sortButtonRect = new Rect(groupButtonRect.xMax + 4f, rect.y + 4f, groupButtonRect.height, groupButtonRect.height).ExpandedBy(2f);
            var toggleRect = new Rect(sortButtonRect.xMax + 4f, rect.y + 4f, groupButtonRect.height, groupButtonRect.height).ExpandedBy(2f);
            bool groupByTechLevel = BetterArchitectSettings.groupByTechLevelPerCategory.ContainsKey(mainCat.defName) ? BetterArchitectSettings.groupByTechLevelPerCategory[mainCat.defName] : false;
            if (Widgets.ButtonImage(groupButtonRect, GroupingIcon))
            {
                BetterArchitectSettings.groupByTechLevelPerCategory[mainCat.defName] = !groupByTechLevel;
                BetterArchitectSettings.Save();
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            if (groupByTechLevel) Widgets.DrawHighlight(groupButtonRect);
            TooltipHandler.TipRegion(groupButtonRect, "BA.GroupByTechLevel".Translate());

            if (!BetterArchitectSettings.sortSettingsPerCategory.ContainsKey(mainCat.defName)) BetterArchitectSettings.sortSettingsPerCategory[mainCat.defName] = new SortSettings();
            var settings = BetterArchitectSettings.sortSettingsPerCategory[mainCat.defName];
            if (Widgets.ButtonImage(sortButtonRect, SortType))
            {
                var availableSortOptions = System.Enum.GetValues(typeof(SortBy)).Cast<SortBy>().Where(sortBy =>
                {
                    if (sortBy == SortBy.Default || sortBy == SortBy.Label) return true;
                    var values = designators.Select(d => GetSortValueFor(d, sortBy)).ToList();
                    if (values.All(v => v == null) || values.Distinct().Count() <= 1)
                    {
                        return false;
                    }
                    return values.Any(v => v != null);
                }).Select(s => new FloatMenuOption(s.ToStringTranslated(), delegate
                {
                    settings.SortBy = s;
                    BetterArchitectSettings.Save();
                })).ToList();

                Find.WindowStack.Add(new FloatMenu(availableSortOptions));
            }
            if (Widgets.ButtonImage(toggleRect, settings.Ascending ? AscendingIcon : DescendingIcon))
            {
                settings.Ascending = !settings.Ascending;
                BetterArchitectSettings.Save();
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
        }

        private static void SortDesignators(List<Designator> designators, SortSettings settings)
        {
            if (settings.SortBy == SortBy.Default) return;

            List<Designator> sortedDesignators;
            
            if (settings.SortBy == SortBy.Label)
            {
                sortedDesignators = designators.OrderBy(a => a.GetLabel()).ToList();
            }
            else
            {
                sortedDesignators = designators.OrderBy(a => GetSortValueFor(a, settings.SortBy).GetValueOrDefault(float.MinValue))
                                               .ThenBy(a => GetUiOrderFor(a))
                                               .ThenBy(a => a.GetLabel())
                                               .ToList();
            }
            if (!settings.Ascending)
            {
                sortedDesignators.Reverse();
            }
            designators.Clear();
            designators.AddRange(sortedDesignators);
        }

        private static float? GetSortValueFor(Designator d, SortBy sortBy)
        {
            BuildableDef buildable = GetBuildableDefFrom(d);
            if (buildable == null) return null;
            return sortBy switch
            {
                SortBy.Beauty => GetStatValueIfDefined(buildable, StatDefOf.Beauty),
                SortBy.Comfort => GetStatValueIfDefined(buildable, StatDefOf.Comfort),
                SortBy.Value => GetStatValueIfDefined(buildable, StatDefOf.MarketValue),
                SortBy.WorkToBuild => GetStatValueIfDefined(buildable, StatDefOf.WorkToBuild),
                SortBy.Health => GetStatValueIfDefined(buildable, StatDefOf.MaxHitPoints),
                SortBy.Cleanliness => GetStatValueIfDefined(buildable, StatDefOf.Cleanliness),
                SortBy.Flammability => GetStatValueIfDefined(buildable, StatDefOf.Flammability),
                SortBy.SkillRequired => buildable.constructionSkillPrerequisite,
                SortBy.CoverEffectiveness => buildable is ThingDef thingDef ? thingDef.fillPercent : (float?)null,
                SortBy.MaxPowerOutput => GetMaxPowerOutput(buildable),
                SortBy.PowerConsumption => GetPowerConsumption(buildable),
                SortBy.RecreationPower => GetStatValueIfDefined(buildable, StatDefOf.JoyGainFactor),
                SortBy.MoveSpeed => GetMoveSpeed(buildable),
                SortBy.TotalStorageCapacity => GetTotalStorageCapacity(buildable),
                SortBy.DoorOpeningSpeed => GetStatValueIfDefined(buildable, StatDefOf.DoorOpenSpeed),
                SortBy.WorkSpeedFactor => GetStatValueIfDefined(buildable, StatDefOf.WorkTableWorkSpeedFactor),
                _ => null
            };
        }

        private static ThingDef GetStuffFrom(BuildableDef buildable)
        {
            if (buildable is not ThingDef thingDef || thingDef.MadeFromStuff is false) return null;
            
            if (Find.CurrentMap == null) return null;
            
            foreach (ThingDef item in from d in Find.CurrentMap.resourceCounter.AllCountedAmounts.Keys
                orderby d.stuffProps?.commonality ?? float.PositiveInfinity descending, d.BaseMarketValue
                select d)
            {
                if (item.IsStuff && item.stuffProps.CanMake(thingDef) && (DebugSettings.godMode || Find.CurrentMap.listerThings.ThingsOfDef(item).Count > 0))
                {
                    return item;
                }
            }
            
            return null;
        }

        private static Designator DrawOrdersPanel(Rect rect, List<Designator> designators)
        {
            var gizmoSize = 75f;
            var gizmoSpacing = 5f;
            var rowHeight = gizmoSize + gizmoSpacing + 5f;
            var outRect = rect.ContractedBy(2f);
            outRect.width += 2;

            var columns = 2;
            var columnWidth = (outRect.width - 16f - (columns - 1) * gizmoSpacing) / columns;

            var rowCount = Mathf.CeilToInt((float)designators.Count / columns);
            var viewRect = new Rect(0, 0, outRect.width - 16f, rowCount * rowHeight);

            Designator mouseoverGizmo = null;
            Widgets.BeginScrollView(outRect, ref ordersScrollPosition, viewRect);
            for (var i = 0; i < designators.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;
                var xPos = col * (gizmoSize + gizmoSpacing);
                var yPos = row * rowHeight;
                var result = designators[i].GizmoOnGUI(new Vector2(xPos, yPos), gizmoSize, default);
                if (result.State >= GizmoState.Mouseover) mouseoverGizmo = designators[i];
                if (result.State == GizmoState.Interacted) designators[i].ProcessInput(result.InteractEvent);
            }
            Widgets.EndScrollView();
            return mouseoverGizmo;
        }

        private static void DoInfoBox(Designator designator)
        {
            Find.WindowStack.ImmediateWindow(32520, ArchitectCategoryTab.InfoRect, WindowLayer.GameUI, delegate
            {
                if (designator == null) return;
                Rect rect = ArchitectCategoryTab.InfoRect.AtZero().ContractedBy(7f);
                Widgets.BeginGroup(rect);
                Rect titleRect = new Rect(0f, 0f, rect.width - designator.PanelReadoutTitleExtraRightMargin, 999f);
                Text.Font = GameFont.Small;
                Widgets.Label(titleRect, designator.LabelCap);
                float curY = Mathf.Max(24f, Text.CalcHeight(designator.LabelCap, titleRect.width));
                designator.DrawPanelReadout(ref curY, rect.width);
                Rect descRect = new Rect(0f, curY, rect.width, rect.height - curY);
                string desc = designator.Desc;
                GenText.SetTextSizeToFit(desc, descRect);
                desc = desc.TruncateHeight(descRect.width, descRect.height);
                Widgets.Label(descRect, desc);
                Widgets.EndGroup();
            });
        }

        private static float GetUiOrderFor(Designator d)
        {
            BuildableDef buildable = GetBuildableDefFrom(d);
            return buildable?.uiOrder ?? float.MaxValue;
        }

        private static BuildableDef GetBuildableDefFrom(Designator d)
        {
            if (d is Designator_Build db) return db.PlacingDef;
            if (d is Designator_Dropdown dd) return dd.Elements.OfType<Designator_Build>().FirstOrDefault()?.PlacingDef;
            return null;
        }

        private static TechLevel GetTechLevelFor(Designator d)
        {
            var b = GetBuildableDefFrom(d);
            if (b?.researchPrerequisites != null && b.researchPrerequisites.Any(x => x.techLevel != TechLevel.Undefined))
            {
                var highestTechLevel = b.researchPrerequisites
                    .Where(x => x.techLevel != TechLevel.Undefined)
                    .Max(x => x.techLevel);
                return highestTechLevel;
            }
            var techLevel = (b as ThingDef)?.techLevel;
            return (techLevel == TechLevel.Undefined || techLevel == null) ? TechLevel.Neolithic : techLevel.Value;
        }

        private static Color GetColorFor(TechLevel l)
        {
            return l switch
            {
                TechLevel.Spacer => new Color(0.6f, 0.9f, 1f),
                TechLevel.Ultra => new Color(0.9f, 0.7f, 1f),
                TechLevel.Industrial => new Color(0.7f, 1f, 0.7f),
                TechLevel.Medieval => new Color(1f, 0.9f, 0.6f),
                TechLevel.Neolithic => new Color(1f, 0.7f, 0.7f),
                TechLevel.Archotech => Color.yellow,
                _ => Color.gray
            };
        }

        private static void PopulateMaterials(Dictionary<MaterialInfo, List<Designator>> floorsByMaterial, MaterialInfo freeKey, Designator element)
        {
            var costs = GetFloorCosts(element);
            if (costs.NullOrEmpty())
            {
                AddFloor(element, freeKey, floorsByMaterial);
            }
            else
            {
                foreach (var cost in costs)
                {
                    if (cost.thingDef.stuffProps != null && (DebugSettings.godMode || Find.CurrentMap.listerThings.ThingsOfDef(cost.thingDef).Any()))
                    {
                        AddFloor(element, new MaterialInfo(cost.thingDef.LabelCap, cost.thingDef.uiIcon, cost.thingDef.uiIconColor, cost.thingDef), floorsByMaterial);
                    }
                }
            }
        }

        private static void AddFloor(Designator designator, MaterialInfo key, Dictionary<MaterialInfo, List<Designator>> floorsByMaterial)
        {
            if (!floorsByMaterial.ContainsKey(key)) floorsByMaterial[key] = new List<Designator>();
            if (!floorsByMaterial[key].Contains(designator)) floorsByMaterial[key].Add(designator);
        }
        private static void HandleScrollBar(Rect outRect, Rect viewRect, ref Vector2 scrollPosition)
        {
            if (Event.current.type == EventType.ScrollWheel && Mouse.IsOver(outRect))
            {
                scrollPosition.y += Event.current.delta.y * 20f;
                float num = 0f;
                float num2 = viewRect.height - outRect.height;
                if (scrollPosition.y < num)
                {
                    scrollPosition.y = num;
                }
                if (scrollPosition.y > num2)
                {
                    scrollPosition.y = num2;
                }
                Event.current.Use();
            }
        }

        private static void DrawOptionBackground(Rect rect, bool selected)
        {
            if (selected)
            {
                DrawOptionSelected(rect);
            }
            else
            {
                Widgets.DrawOptionUnselected(rect);
            }
            Widgets.DrawHighlightIfMouseover(rect);
        }

        public static void DrawOptionSelected(Rect rect)
        {
            GUI.color = Widgets.OptionSelectedBGFillColor;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Widgets.OptionSelectedBGBorderColor;
            Widgets.DrawBox(rect, 1);
            GUI.color = Color.white;
        }

        private static List<ThingDefCountClass> GetFloorCosts(Designator designator)
        {
            var costs = new List<ThingDefCountClass>();
            BuildableDef floor = GetBuildableDefFrom(designator);
            if (floor?.costList != null) costs.AddRange(floor.costList);
            if (floor?.costStuffCount > 0 && floor.stuffCategories != null)
            {
                var allStuff = DefDatabase<ThingDef>.AllDefs
                    .Where(y => y.stuffProps?.categories != null && y.stuffProps.categories.Any(cat => floor.stuffCategories.Contains(cat)));
                foreach (var item in allStuff)
                {
                    costs.Add(new ThingDefCountClass(item, floor.costStuffCount));
                }
            }
            return costs;
        }

        private static float? GetMaxPowerOutput(BuildableDef buildable)
        {
            if (buildable is ThingDef thingDef)
            {
                var powerComp = thingDef.GetCompProperties<CompProperties_Power>();
                if (powerComp != null)
                {
                    if (powerComp.basePowerConsumption < 0)
                    {
                        return -powerComp.basePowerConsumption;
                    }
                }
            }
            return null;
        }

        private static float? GetPowerConsumption(BuildableDef buildable)
        {
            if (buildable is ThingDef thingDef)
            {
                var powerComp = thingDef.GetCompProperties<CompProperties_Power>();
                if (powerComp != null)
                {
                    if (powerComp.basePowerConsumption > 0)
                    {
                        return powerComp.basePowerConsumption;
                    }
                }
            }
            return null;
        }
        private static System.Type vehicleBuildDefType;
        private static System.Reflection.FieldInfo thingToSpawnProp;
        private static System.Reflection.FieldInfo vehicleStatsField;
        private static System.Reflection.FieldInfo statDefProp;
        private static System.Reflection.PropertyInfo workerProp;
        private static System.Reflection.MethodInfo getValueMethod;

        private static void InitializeVehicleFrameworkReflection()
        {
            if (vehicleBuildDefType != null) return;

            try
            {
                vehicleBuildDefType = AccessTools.TypeByName("Vehicles.VehicleBuildDef");
                if (vehicleBuildDefType == null)
                {
                    Log.Error("InitializeVehicleFrameworkReflection: vehicleBuildDefType is null");
                    return;
                }
                thingToSpawnProp = AccessTools.Field(vehicleBuildDefType, "thingToSpawn");
                if (thingToSpawnProp == null)
                {
                    Log.Error("InitializeVehicleFrameworkReflection: thingToSpawnProp is null");
                    return;
                }
                var vehicleDefType = AccessTools.TypeByName("Vehicles.VehicleDef");
                if (vehicleDefType != null)
                {
                    vehicleStatsField = AccessTools.Field(vehicleDefType, "vehicleStats");
                    if (vehicleStatsField == null)
                    {
                        Log.Error("InitializeVehicleFrameworkReflection: vehicleStatsField is null");
                        return;
                    }
                }
                else
                {
                    Log.Error("InitializeVehicleFrameworkReflection: vehicleDefType is null");
                    return;
                }
                var vehicleStatModifierType = AccessTools.TypeByName("Vehicles.VehicleStatModifier");
                if (vehicleStatModifierType != null)
                {
                    statDefProp = AccessTools.Field(vehicleStatModifierType, "statDef");
                    if (statDefProp == null)
                    {
                        Log.Error("InitializeVehicleFrameworkReflection: statDefProp is null");
                        return;
                    }
                }
                else
                {
                    Log.Error("InitializeVehicleFrameworkReflection: vehicleStatModifierType is null");
                    return;
                }
                var vehicleStatDefType = AccessTools.TypeByName("Vehicles.VehicleStatDef");
                if (vehicleStatDefType != null)
                {
                    workerProp = AccessTools.Property(vehicleStatDefType, "Worker");
                    if (workerProp == null)
                    {
                        Log.Error("InitializeVehicleFrameworkReflection: workerProp is null");
                        return;
                    }
                }
                else
                {
                    Log.Error("InitializeVehicleFrameworkReflection: vehicleStatDefType is null");
                    return;
                }
                var vehicleStatWorkerType = AccessTools.TypeByName("Vehicles.VehicleStatWorker");
                if (vehicleStatWorkerType != null)
                {
                    getValueMethod = AccessTools.Method(vehicleStatWorkerType, "GetValueAbstract", new[] { vehicleDefType });
                    if (getValueMethod == null)
                    {
                        Log.Error("InitializeVehicleFrameworkReflection: getValueMethod is null");
                        return;
                    }
                }
                else
                {
                    Log.Error("InitializeVehicleFrameworkReflection: vehicleStatWorkerType is null");
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Error in InitializeVehicleFrameworkReflection: {ex}");
            }
        }

        private static bool? _vehicleFrameworkActive;
        public static bool VehicleFrameworkActive => _vehicleFrameworkActive ??= ModsConfig.IsActive("SmashPhil.VehicleFramework");
        private static float? GetMoveSpeed(BuildableDef buildable)
        {
            if (VehicleFrameworkActive)
            {
                InitializeVehicleFrameworkReflection();
                if (vehicleBuildDefType.IsAssignableFrom(buildable.GetType()) is false) return null;
                try
                {
                    var vehicleDef = thingToSpawnProp.GetValue(buildable);
                    if (vehicleDef == null)
                    {
                        Log.Error("GetMoveSpeed: vehicleDef is null");
                        return null;
                    }

                    var vehicleStats = vehicleStatsField.GetValue(vehicleDef) as System.Collections.IList;

                    foreach (var statModifier in vehicleStats)
                    {
                        var statDef = statDefProp.GetValue(statModifier) as Def;
                        if (statDef.defName == "MoveSpeed")
                        {
                            var worker = workerProp.GetValue(statDef);
                            if (worker == null)
                            {
                                Log.Error("GetMoveSpeed: worker is null");
                                return null;
                            }

                            var result = getValueMethod.Invoke(worker, new object[] { vehicleDef });
                            if (result is float f)
                            {
                                return f;
                            }
                            else
                            {
                                Log.Error("GetMoveSpeed: result is not a float");
                                return null;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Error in GetMoveSpeed for VehicleFramework: {ex}");
                }
            }

            return null;
        }

        private static float? GetStatValueIfDefined(BuildableDef buildable, StatDef statDef)
        {
            if (statDef.showIfUndefined is false && buildable.StatBaseDefined(statDef) is false)
            {
                return null;
            }
            var stuff = GetStuffFrom(buildable);
            var value = buildable.GetStatValueAbstract(statDef, stuff);
            return value;
        }

        private static float? GetTotalStorageCapacity(BuildableDef buildable)
        {
            if (buildable is ThingDef thingDef && typeof(Building_Storage).IsAssignableFrom(thingDef.thingClass) && thingDef.building != null && thingDef.building.maxItemsInCell > 0)
            {
                return thingDef.building.maxItemsInCell * thingDef.size.x * thingDef.size.z;
            }
            return null;
        }
        
        private static string GetLabel(this Designator designator)
        {
            if (designator is Designator_Build buildDesignator)
            {
                var oldWriteStuff = buildDesignator.writeStuff;
                buildDesignator.writeStuff = false;
                var label = buildDesignator.LabelCap;
                buildDesignator.writeStuff = oldWriteStuff;
                return label;
            }
            return designator.LabelCap;
        }
    }
}
