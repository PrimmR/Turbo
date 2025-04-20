using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace Turbo
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        /*********
        ** Attributes
        *********/

        /*** Public ***/
        public const long SPF = 166667;

        public static double speed = 1;

        /*** Private ***/
        internal static ModConfig Config;

        internal static long elapsedTicks = 0;

        internal static long nextFrame = 0;

        internal static int change = 0; // 0 Up, 1 Down, 2 Reset

        internal static long clockTicks = 0; // To fix very small issue with linear clock mode


        /// <summary>The mod entry point method.</summary>
        public override void Entry(IModHelper helper)
        {
            LoadConfig();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;

            // Harmony patches
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(AccessTools.Method(typeof(Game1), nameof(Game1.Instance_Update)),
                new HarmonyMethod(typeof(UpdatePatcher), nameof(UpdatePatcher.UpdateGame)));
            harmony.Patch(original: AccessTools.Method(typeof(Game1), nameof(Game1.UpdateGameClock)),
                new HarmonyMethod(typeof(UpdatePatcher), nameof(UpdatePatcher.UpdateClock)));
            harmony.Patch(original: AccessTools.Method(typeof(HUDMessage), nameof(HUDMessage.draw)),
                finalizer: new HarmonyMethod(typeof(HUDPatcher), nameof(HUDPatcher.HUDDraw_Final)));

            // Inputs
            helper.Events.Input.ButtonPressed += IO.OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += IO.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += IO.OnDayStarted;

            helper.ConsoleCommands.Add("set_speed", "Sets the game speed.\n\nUsage: set_speed <value>\n- value: the speed multiplier.", IO.SetSpeedCmd);
            helper.ConsoleCommands.Add("set_clock_mode", "Sets the behaviour of the in-game clock.\n\nUsage: set_clock_mode <value>\n- value: the clock mode (0, 1, or 2):\n   0: the clock increments proportionally with game speed\n   1: the clock increments at a constant rate, regardless of game speed \n   2: the clock is frozen", IO.SetClockModeCmd);

            IO.Initialise(this.Monitor, this.Helper);
            UpdatePatcher.Initialise(this.Monitor);
            HUDPatcher.Initialise(this.Monitor);
        }

        /// <summary>Called on game launch.</summary>
        /// Used for GenericModConfigMenu
        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(Config)
            );

            // add some config options
            string[] clock_modes = {
                Helper.Translation.Get("ClockMode.Regular"),
                Helper.Translation.Get("ClockMode.Constant"),
                Helper.Translation.Get("ClockMode.Frozen")
            };

            configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => "Options"
                );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("ClockMode.ConfigName"),
                tooltip: () => Helper.Translation.Get("ClockMode.ConfigTooltip"),
                getValue: () => clock_modes[Config.clockMode],
                setValue: value => Config.clockMode = Array.IndexOf(clock_modes, value),
                allowedValues: clock_modes
            );


            configMenu.AddSectionTitle(
                    mod: this.ModManifest,
                    text: () => "Keybinds"
                );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("DecrementSpeed.ConfigName"),
                tooltip: () => Helper.Translation.Get("DecrementSpeed.ConfigTooltip"),
                getValue: () => Config.decrementSpeedKeybind,
                setValue: value => Config.decrementSpeedKeybind = value
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("IncrementSpeed.ConfigName"),
                tooltip: () => Helper.Translation.Get("IncrementSpeed.ConfigTooltip"),
                getValue: () => Config.incrementSpeedKeybind,
                setValue: value => Config.incrementSpeedKeybind = value
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("ResetSpeed.ConfigName"),
                tooltip: () => Helper.Translation.Get("ResetSpeed.ConfigTooltip"),
                getValue: () => Config.resetSpeedKeybind,
                setValue: value => Config.resetSpeedKeybind = value
            );

            configMenu.AddKeybindList(
                 mod: this.ModManifest,
                 name: () => Helper.Translation.Get("CycleClockMode.ConfigName"),
                 tooltip: () => Helper.Translation.Get("CycleClockMode.ConfigTooltip"),
                 getValue: () => Config.cycleClockModeKeybind,
                 setValue: value => Config.cycleClockModeKeybind = value
             );
        }

        /// <summary>Resets Turbo's internal timer to 0.</summary>
        public static void ResetClock()
        {
            elapsedTicks = 0;
            nextFrame = 0;
        }

        /// <summary>Loads the mod's configuration data.</summary>
        private void LoadConfig()
        {
            Config = this.Helper.ReadConfig<ModConfig>();
            if (Config.clockMode < 0 || Config.clockMode > 2)
            {
                Config.clockMode = 0;
                Helper.WriteConfig(Config);
                Monitor.Log("Clock mode set to invalid state - Initialised to 0 (regular)", LogLevel.Error);
            }
        }
    }
}