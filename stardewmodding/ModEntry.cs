using System;
using System.Text.Json;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace stardewmodding
{
    public class ModEntry : Mod
    {
        private const string ManualSaveDataKey = "mariasek.stardewmodding/manual-save";
        private bool shouldRestoreManualSave;
        private bool isManualSaveInProgress;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.Saving += OnSaving;
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (e.Button == SButton.K)
            {
                
                if (Game1.eventUp || Game1.activeClickableMenu != null)
                {
                    this.Monitor.Log("Cannot save right now: Player is in a menu or cutscene.", LogLevel.Warn);
                    return;
                }

                this.Monitor.Log("Manually Saving...", LogLevel.Info);
                try
                {
                    SaveManualReturnPoint();
                    this.isManualSaveInProgress = true;

                    var saveEnumerator = SaveGame.getSaveEnumerator();
                    while (saveEnumerator.MoveNext())
                    {
                    }

                    Game1.addHUDMessage(new HUDMessage("Game Saved Successfully!", HUDMessage.error_type));
                }
                catch (Exception ex)
                {
                    this.Monitor.Log($"Save failed: {ex.Message}", LogLevel.Error);
                }
                finally
                {
                    this.isManualSaveInProgress = false;
                }
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.shouldRestoreManualSave = ReadManualReturnPoint() != null;
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!this.shouldRestoreManualSave)
                return;

            this.shouldRestoreManualSave = false;

            ManualSaveData? data = ReadManualReturnPoint();
            if (data == null)
                return;

            if (Game1.getLocationFromName(data.LocationName) == null)
            {
                this.Monitor.Log($"Couldn't restore manual save location '{data.LocationName}' because it no longer exists.", LogLevel.Warn);
                return;
            }

            Game1.warpFarmer(data.LocationName, data.TileX, data.TileY, data.FacingDirection);
            Game1.timeOfDay = data.TimeOfDay;
            Game1.player.health = Math.Min(data.Health, Game1.player.maxHealth);
            Game1.player.Stamina = Math.Min(data.Stamina, Game1.player.MaxStamina);

            this.Monitor.Log($"Restored manual save at {data.LocationName} ({data.TileX}, {data.TileY}) at {data.TimeOfDay}.", LogLevel.Info);
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            if (!this.isManualSaveInProgress)
                ClearManualReturnPoint();
        }

        private void SaveManualReturnPoint()
        {
            Vector2 tile = Game1.player.Tile;
            ManualSaveData data = new()
            {
                LocationName = Game1.currentLocation.Name,
                TileX = (int)tile.X,
                TileY = (int)tile.Y,
                FacingDirection = Game1.player.FacingDirection,
                TimeOfDay = Game1.timeOfDay,
                Health = Game1.player.health,
                Stamina = Game1.player.Stamina
            };

            Game1.player.modData[ManualSaveDataKey] = JsonSerializer.Serialize(data);
        }

        private ManualSaveData? ReadManualReturnPoint()
        {
            if (!Game1.player.modData.TryGetValue(ManualSaveDataKey, out string? rawData) || string.IsNullOrWhiteSpace(rawData))
                return null;

            try
            {
                return JsonSerializer.Deserialize<ManualSaveData>(rawData);
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"Couldn't read manual save data: {ex.Message}", LogLevel.Warn);
                return null;
            }
        }

        private static void ClearManualReturnPoint()
        {
            Game1.player.modData.Remove(ManualSaveDataKey);
        }

        private sealed class ManualSaveData
        {
            public string LocationName { get; set; } = "";
            public int TileX { get; set; }
            public int TileY { get; set; } 
            public int FacingDirection { get; set; }
            public int TimeOfDay { get; set; }
            public int Health { get; set; }
            public float Stamina { get; set; }
        }
    }
}
