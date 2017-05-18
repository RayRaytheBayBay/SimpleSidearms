﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HugsLib.Settings;
using UnityEngine;
using RimWorld;
using SimpleSidearms.rimworld;
using SimpleSidearms.utilities;
using static SimpleSidearms.Globals;
using Verse.Sound;
using Verse;

namespace SimpleSidearms.hugsLibSettings
{
    internal static class SettingsUIs
    {
        internal enum WeaponListKind { Both, Melee, Ranged }

        private const float ContentPadding = 5f;
        private const float MinGizmoSize = 75f;
        private const float IconSize = 32f;
        private const float IconGap = 1f;
        private const float TextMargin = 20f;
        private const float BottomMargin = 2f;

        private static readonly Color iconBaseColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color iconMouseOverColor = new Color(0.6f, 0.6f, 0.4f, 1f);


        private static readonly Color SelectedOptionColor = new Color(0.6f, 1f, 0.6f, 1f);
        private static readonly Color constGrey = new Color(0.8f, 0.8f, 0.8f, 1f);

        private static float averageCarryCapacity = 35f;

        internal enum ExpansionMode { None, Vertical, Horizontal};

        public static bool CustomDrawer_FloatSlider(Rect rect, SettingHandle<float> setting, bool def_isPercentage, float def_min, float def_max)
        {
            /*if (setting.Value == null)
                setting.Value = new IntervalSliderHandler(def_isPercentage, def_value, def_min, def_max);
            if (setting.Value.setup == false)
                setting.Value = new IntervalSliderHandler(def_isPercentage, setting.Value, def_min, def_max);*/

            Rect sliderPortion = new Rect(rect);
            sliderPortion.width = sliderPortion.width - 50;
            Rect labelPortion = new Rect(rect);
            labelPortion.width = 50;
            labelPortion.position = new Vector2(sliderPortion.position.x + sliderPortion.width + 5f, sliderPortion.position.y + 4f);

            sliderPortion = sliderPortion.ContractedBy(2f);

            if (def_isPercentage)
                Widgets.Label(labelPortion, (Mathf.Round(setting.Value * 100f)).ToString("F0") + "%");
            else
                Widgets.Label(labelPortion, setting.Value.ToString("F2"));

            float val = Widgets.HorizontalSlider(sliderPortion, setting.Value, def_min, def_max, true);
            //Log.Message("getting val of "+val);
            bool change = false;

            if (setting.Value != val)
                change = true;

            setting.Value = val;

            return change;
        }

        public static bool CustomDrawer_FloatSlider(Rect rect, SettingHandle<float> setting, bool def_isPercentage)
        {
            return CustomDrawer_FloatSlider(rect, setting, def_isPercentage, 0, 1);
        }

        internal static bool CustomDrawer_Enumlist(SettingHandle handle, Rect controlRect, string[] enumNames, float[] forcedWidths, ExpansionMode expansionMode)
        {
            if (enumNames == null) return false;
            if (enumNames.Length != forcedWidths.Length) return false;

            if (expansionMode == ExpansionMode.Horizontal)
                throw new NotImplementedException("Horizontal scrolling not yet implemented.");

            float buttonWidth = controlRect.width;
            int forcedButtons = 0;
            for (int i = 0; i < forcedWidths.Length; i++)
            {
                if(forcedWidths[i] != 0f)
                {
                    forcedButtons++;
                    buttonWidth -= forcedWidths[i];
                }
            }
            if (forcedButtons != enumNames.Length)
                buttonWidth /= (float)(enumNames.Length - forcedButtons);

            float position = controlRect.position.x;

            bool changed = false;

            for(int i = 0; i < enumNames.Length; i++)
            {
                float width = (forcedWidths[i] == 0f) ? buttonWidth : forcedWidths[i];

                Rect buttonRect = new Rect(controlRect);
                buttonRect.position = new Vector2(position, buttonRect.position.y);
                buttonRect.width = width;
                //buttonRect = buttonRect.ContractedBy(2f);
                bool interacted = false;

                bool selected = handle.StringValue.Equals(enumNames[i]);

                string label = (handle.EnumStringPrefix + enumNames[i]).Translate();

                if(expansionMode == ExpansionMode.Vertical)
                {
                    float height = Text.CalcHeight(label, width);
                    if (handle.CustomDrawerHeight < height) handle.CustomDrawerHeight = height;
                }

                Color activeColor = GUI.color;
                if (selected)
                    GUI.color = SelectedOptionColor;

                interacted = Widgets.ButtonText(buttonRect, label);

                if (selected)
                    GUI.color = activeColor;

                if (interacted)
                {
                    handle.StringValue = enumNames[i];
                    changed = true;
                }

                position += width;
            }
            return changed;
        }
        
        internal static bool CustomDrawer_Button(Rect rect, SettingHandle<Preset> setting, string label)
        {
            Rect buttonRect = new Rect(rect);
            buttonRect.width = 120;
            Color keptColor = GUI.color;
            if (SimpleSidearms.ActivePreset.Value == setting.Value)
                GUI.color = SelectedOptionColor;
            bool clicked = Widgets.ButtonText(rect, label.Translate());
            if (clicked)
            {
                SimpleSidearms.ResetSettings();
                SimpleSidearms.ActivePreset.Value = setting.Value;
                Presets.presetChanged(setting.Value);
            }
            GUI.color = keptColor;
            return false; 
        }

        internal static bool CustomDrawer_RighthandSideLine(Rect wholeRect, SettingHandle setting)
        {
            return CustomDrawer_RighthandSideLine(wholeRect, setting, constGrey);
        }

        internal static bool CustomDrawer_RighthandSideLine(Rect wholeRect, SettingHandle setting, Color color)
        {
            wholeRect.position = new Vector2(wholeRect.position.x, wholeRect.position.y + (wholeRect.height - 1f) / 2f);
            wholeRect.height = 1f;
            GUI.color = color;
            GUI.DrawTexture(wholeRect, TexUI.FastFillTex);
            GUI.color = Color.white;
            return false;
        }

        internal static bool CustomDrawer_RighthandSideLabel(Rect wholeRect, string label)
        {
            DrawLabel(label, wholeRect, TextMargin);
            return false;
        }

        internal static bool CustomDrawer_MatchingWeapons_passiveRelative(Rect wholeRect, SettingHandle<WeaponListKind> setting)
        {
            float weightLimit = averageCarryCapacity;
            switch (setting.Value)
            {
                case WeaponListKind.Melee:
                    weightLimit *= SimpleSidearms.LimitModeSingleMelee_Relative.Value;
                    break;
                case WeaponListKind.Ranged:
                    weightLimit *= SimpleSidearms.LimitModeSingleRanged_Relative.Value;
                    break;
                case WeaponListKind.Both:
                default:
                    weightLimit *= SimpleSidearms.LimitModeSingle_Relative.Value;
                    break;
            }
            return CustomDrawer_MatchingWeapons_passive(wholeRect, setting, weightLimit, "Sidearms below avg. max. weight ("+weightLimit.ToString("F1")+"kg)");
        }

        internal static bool CustomDrawer_MatchingWeapons_passiveAbsolute(Rect wholeRect, SettingHandle<WeaponListKind> setting)
        {
            float weightLimit = 0;
            switch (setting.Value)
            {
                case WeaponListKind.Melee:
                    weightLimit = SimpleSidearms.LimitModeSingleMelee_Absolute.Value;
                    break;
                case WeaponListKind.Ranged:
                    weightLimit = SimpleSidearms.LimitModeSingleRanged_Absolute.Value;
                    break;
                case WeaponListKind.Both:
                default:
                    weightLimit = SimpleSidearms.LimitModeSingle_Absolute.Value;
                    break;
            }
            return CustomDrawer_MatchingWeapons_passive(wholeRect, setting, weightLimit, "Sidearms below maximum weight");

        }

        private static bool CustomDrawer_MatchingWeapons_passive(Rect wholeRect, SettingHandle<WeaponListKind> setting, float weightLimit , string label)
        { 

            Rect offsetRect = new Rect(wholeRect);
            offsetRect.height = wholeRect.height - TextMargin + BottomMargin;
            offsetRect.position = new Vector2(offsetRect.position.x, offsetRect.position.y);

            DrawLabel(label, offsetRect, TextMargin);

            offsetRect.position = new Vector2(offsetRect.position.x, offsetRect.position.y + TextMargin);

            List<ThingStuffPair> matchingSidearms; 
            switch (setting.Value)
            {
                case WeaponListKind.Melee:
                    matchingSidearms = GettersFilters.filterForType(PawnSidearmsGenerator.getWeaponsList(), WeaponSearchType.Melee, false);
                    break;
                case WeaponListKind.Ranged:
                    matchingSidearms = GettersFilters.filterForType(PawnSidearmsGenerator.getWeaponsList(), WeaponSearchType.Ranged, false);
                    break;
                case WeaponListKind.Both:
                default:
                    matchingSidearms = GettersFilters.filterForType(PawnSidearmsGenerator.getWeaponsList(), WeaponSearchType.Both, false);
                    break;
            }
            matchingSidearms.Sort(new MassComparer());
            List<ThingStuffPair> matchingSidearmsUnderLimit =  new List<ThingStuffPair>();
            for (int i = 0; i < matchingSidearms.Count; i++)
            {
                float mass = matchingSidearms[i].thing.GetStatValueAbstract(StatDefOf.Mass);
                if (mass > weightLimit)
                    break; 
                else
                    matchingSidearmsUnderLimit.Add(matchingSidearms[i]);
            }

            int iconsPerRow = (int)(offsetRect.width / (IconGap + IconSize));

            int biggerRows = ((matchingSidearmsUnderLimit.Count - 1) / iconsPerRow) + 1;
            setting.CustomDrawerHeight = (biggerRows * IconSize) + ((biggerRows) * IconGap) + TextMargin;
            //Log.Message("matching sidearms: " + matchingSidearms.Count);
            //Log.Message("matching sidearms under limit (" + weightLimit + "): " + matchingSidearmsUnderLimit.Count);

            for (int i = 0; i < matchingSidearmsUnderLimit.Count; i++)
            {
                int collum = (i % iconsPerRow);
                int row = (i / iconsPerRow);
                bool interacted = DrawIconForWeapon(matchingSidearmsUnderLimit[i].thing, offsetRect, new Vector2(IconSize* collum + collum * IconGap, IconSize * row + row * IconGap),i);
                if(interacted)
                {
                    //nothing, since this is the passive list
                }
            }
            return false;
        }

        private static void DrawLabel(string labelText, Rect textRect, float offset)
        {
            var labelHeight = Text.CalcHeight(labelText, textRect.width);
            labelHeight -= 2f;
            var labelRect = new Rect(textRect.x, textRect.yMin - labelHeight + offset, textRect.width, labelHeight);
            GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, labelText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }

        internal static bool CustomDrawer_MatchingWeapons_active(Rect wholeRect, SettingHandle<StringListHandler> setting, WeaponListKind kind, string yesText = "Sidearms", string noText = "Not sidearms", bool excludeNeolithic = false)
        {
            if (setting.Value == null)
                setting.Value = new StringListHandler();

            GUI.color = Color.white;

            Rect leftRect = new Rect(wholeRect);
            leftRect.width = leftRect.width / 2;
            leftRect.height = wholeRect.height - TextMargin + BottomMargin;
            leftRect.position = new Vector2(leftRect.position.x, leftRect.position.y);
            Rect rightRect = new Rect(wholeRect);
            rightRect.width = rightRect.width / 2;
            leftRect.height = wholeRect.height - TextMargin + BottomMargin;
            rightRect.position = new Vector2(rightRect.position.x + leftRect.width, rightRect.position.y);

            DrawLabel(yesText, leftRect, TextMargin);
            DrawLabel(noText, rightRect, TextMargin);

            leftRect.position = new Vector2(leftRect.position.x, leftRect.position.y + TextMargin);
            rightRect.position = new Vector2(rightRect.position.x, rightRect.position.y + TextMargin);

            int iconsPerRow = (int)(leftRect.width / (IconGap + IconSize));

            List<string> selection = setting.Value.InnerList;

            List<ThingStuffPair> matchingSidearms;
            List<ThingStuffPair> allSidearms = GettersFilters.filterForType(PawnSidearmsGenerator.getWeaponsList(), WeaponSearchType.Both, false);

            switch (kind)
            {
                case WeaponListKind.Melee:
                    matchingSidearms = GettersFilters.filterForType(allSidearms, WeaponSearchType.Melee, false);
                    break;
                case WeaponListKind.Ranged:
                    matchingSidearms = GettersFilters.filterForType(allSidearms, WeaponSearchType.Ranged, false);
                    break;
                case WeaponListKind.Both:
                default:
                    matchingSidearms = allSidearms;
                    break;
            }

            if (excludeNeolithic)
                GettersFilters.excludeNeolithic(matchingSidearms);

            matchingSidearms.Sort(new MassComparer());

            ThingDef[] selectionThingDefs = new ThingDef[selection.Count];
            for (int i = 0; i < allSidearms.Count; i++)
            {
                for (int j = 0; j < selectionThingDefs.Length; j++)
                {
                    if (selection[j].Equals(allSidearms[i].thing.defName))
                        selectionThingDefs[j] = allSidearms[i].thing;
                }
            }

            List<ThingStuffPair> unselectedSidearms = new List<ThingStuffPair>();
            for (int i = 0; i < matchingSidearms.Count; i++)
            {
                if (!selection.Contains(matchingSidearms[i].thing.defName))
                    unselectedSidearms.Add(matchingSidearms[i]);
            }
            //Log.Message("matching sidearms: " + matchingSidearms.Count);
            //Log.Message("matching sidearms under limit (" + weightLimit + "): " + matchingSidearmsUnderLimit.Count);

            bool change = false;

            int biggerRows = Math.Max((selection.Count - 1) / iconsPerRow, (unselectedSidearms.Count - 1) / iconsPerRow) + 1;
            setting.CustomDrawerHeight = (biggerRows * IconSize) + ((biggerRows) * IconGap) + TextMargin;

            for (int i = 0; i < selection.Count; i++)
            {
                if (selectionThingDefs[i] == null)
                {
                    Log.Message("Missing matching thingDef");
                    continue;
                }
                int collum = (i % iconsPerRow);
                int row = (i / iconsPerRow);
                bool interacted = DrawIconForWeapon(selectionThingDefs[i], leftRect, new Vector2(IconSize * collum + collum * IconGap, IconSize * row + row * IconGap), i);
                if (interacted)
                {
                    change = true;
                    selection.Remove(selection[i]);
                    i--; //to prevent skipping
                }
            }

            for (int i = 0; i < unselectedSidearms.Count; i++)
            {
                int collum = (i % iconsPerRow);
                int row = (i / iconsPerRow);
                bool interacted = DrawIconForWeapon(unselectedSidearms[i].thing, rightRect, new Vector2(IconSize * collum + collum * IconGap, IconSize * row + row * IconGap), i);
                if (interacted)
                {
                    change = true;
                    selection.Add(unselectedSidearms[i].thing.defName);
                }
            }
            if (change)
            {
                setting.Value.InnerList = selection;
                //Log.Message("selected list change");
            }
            return change;
        }

        private static Color getColor(ThingDef weapon)
        {
            if (weapon.graphicData != null)
            {
                return weapon.graphicData.color;
            }
            return Color.white;
        }

        private static Color getColorTwo(ThingDef weapon)
        {
            if (weapon.graphicData != null)
            {
                return weapon.graphicData.colorTwo;
            }
            return Color.white;
        }

        private static bool DrawIconForWeapon(ThingDef weapon, Rect contentRect, Vector2 iconOffset, int buttonID)
        {
            var iconTex = weapon.uiIcon;
            Graphic g = weapon.graphicData.Graphic;
            Color color = getColor(weapon);
            Color colorTwo = getColor(weapon);
            Graphic g2 = weapon.graphicData.Graphic.GetColoredVersion(g.Shader, color, colorTwo);

            var iconRect = new Rect(contentRect.x + iconOffset.x, contentRect.y + iconOffset.y, IconSize, IconSize);

            if (!contentRect.Contains(iconRect))
                return false;

            string label = weapon.label;

            TooltipHandler.TipRegion(iconRect, label);
            MouseoverSounds.DoRegion(iconRect, SoundDefOf.MouseoverCommand);
            if (Mouse.IsOver(iconRect))
            {
                GUI.color = iconMouseOverColor;
                GUI.DrawTexture(iconRect, TextureResources.drawPocket);
                //Graphics.DrawTexture(iconRect, TextureResources.drawPocket, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconMouseOverColor);
            }
            else
            {
                GUI.color = iconBaseColor;
                GUI.DrawTexture(iconRect, TextureResources.drawPocket);
                //Graphics.DrawTexture(iconRect, TextureResources.drawPocket, new Rect(0, 0, 1f, 1f), 0, 0, 0, 0, iconBaseColor);
            }

            Texture resolvedIcon;
            if (!weapon.uiIconPath.NullOrEmpty())
            {
                resolvedIcon = weapon.uiIcon;
            }
            else
            {
                resolvedIcon = g2.MatSingle.mainTexture;
            }
            GUI.color = color;
            GUI.DrawTexture(iconRect, resolvedIcon);
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(iconRect, true))
            {
                Event.current.button = buttonID;
                return true;
            }
            else
                return false;
        }
    }
}
