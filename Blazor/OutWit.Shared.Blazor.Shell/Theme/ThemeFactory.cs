using MudBlazor;
using MudBlazor.Utilities;

namespace OutWit.Shared.Blazor.Shell.Theme
{
    /// <summary>
    /// Factory for creating the standard OutWit MudBlazor theme.
    /// Provides consistent light and dark palettes across all OutWit applications.
    /// </summary>
    public static class ThemeFactory
    {
        /// <summary>
        /// Creates the default OutWit theme with light and dark palettes.
        /// </summary>
        public static MudTheme Create() => new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = ThemeDefaults.Navy,
                PrimaryContrastText = "#FFFFFF",

                Secondary = ThemeDefaults.Lime,
                SecondaryContrastText = "#000000",

                Tertiary = "#37DDE8",
                TertiaryContrastText = "#001417",

                Info = "#3A7AFE",
                Success = "#34C759",
                Warning = "#F59E0B",
                Error = "#B3261E",

                Background = ThemeDefaults.SurfaceLight,
                BackgroundGray = "#EEF2F7",
                Surface = ThemeDefaults.SurfaceLight,

                TextPrimary = "#0F1626",
                TextSecondary = new MudColor("#0F1626").SetAlpha(0.64).ToString(MudColorOutputFormats.RGBA),
                TextDisabled = new MudColor("#0F1626").SetAlpha(0.38).ToString(MudColorOutputFormats.RGBA),

                DrawerBackground = ThemeDefaults.SurfaceLight,
                DrawerText = "#202431",
                DrawerIcon = "#3B4150",

                AppbarBackground = ThemeDefaults.Navy,
                AppbarText = Colors.Shades.White,

                LinesDefault = new MudColor(ThemeDefaults.OutlineLight).SetAlpha(0.24).ToString(MudColorOutputFormats.RGBA),
                LinesInputs = "#C9D1E1",
                Divider = "#D9E1ED",
                DividerLight = new MudColor("#000000").SetAlpha(0.08).ToString(MudColorOutputFormats.RGBA),

                TableLines = new MudColor("#E4EAF2").ToString(MudColorOutputFormats.RGBA),
                TableStriped = new MudColor("#000000").SetAlpha(0.02).ToString(MudColorOutputFormats.RGBA),
                TableHover = new MudColor("#000000").SetAlpha(0.04).ToString(MudColorOutputFormats.RGBA),
            },

            PaletteDark = new PaletteDark
            {
                Primary = "#9FB4D6",
                PrimaryContrastText = "#0E192A",

                Secondary = ThemeDefaults.Lime,
                SecondaryContrastText = "#000000",

                Tertiary = "#4ADCE5",
                TertiaryContrastText = "#001316",

                Info = "#82A8FF",
                Success = "#5BE37E",
                Warning = "#F8B84E",
                Error = "#F2B8B5",

                Background = ThemeDefaults.SurfaceDark,
                BackgroundGray = "#0C0F14",
                Surface = "#101420",

                TextPrimary = ThemeDefaults.OnDark,
                TextSecondary = new MudColor(ThemeDefaults.OnDark).SetAlpha(0.72).ToString(MudColorOutputFormats.RGBA),
                TextDisabled = new MudColor(ThemeDefaults.OnDark).SetAlpha(0.38).ToString(MudColorOutputFormats.RGBA),

                DrawerBackground = "#111622",
                DrawerText = ThemeDefaults.OnDark,
                DrawerIcon = "#9CA6B7",

                AppbarBackground = "#172334",
                AppbarText = "#E8EEF9",

                LinesDefault = new MudColor(ThemeDefaults.OutlineDark).SetAlpha(0.30).ToString(MudColorOutputFormats.RGBA),
                LinesInputs = "#59657A",
                Divider = "#2C3342",
                DividerLight = new MudColor("#FFFFFF").SetAlpha(0.08).ToString(MudColorOutputFormats.RGBA),

                TableLines = new MudColor("#2A3140").ToString(MudColorOutputFormats.RGBA),
                TableStriped = new MudColor("#FFFFFF").SetAlpha(0.02).ToString(MudColorOutputFormats.RGBA),
                TableHover = new MudColor("#FFFFFF").SetAlpha(0.04).ToString(MudColorOutputFormats.RGBA),
            },

            LayoutProperties = new LayoutProperties { DefaultBorderRadius = "12px" },
        };
    }
}
