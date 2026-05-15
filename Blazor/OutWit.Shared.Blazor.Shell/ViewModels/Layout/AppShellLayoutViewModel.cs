using Microsoft.AspNetCore.Components;
using MudBlazor;
using OutWit.Shared.Blazor.Shell.Theme;

namespace OutWit.Shared.Blazor.Shell.ViewModels.Layout
{
    /// <summary>
    /// ViewModel for the <c>AppShellLayout</c> component. Provides the standard
    /// MudBlazor + Material 3 application shell: AppBar with menu toggle and
    /// theme toggle, mini-drawer navigation, breakpoint-driven mobile collapse.
    ///
    /// All product-specific bits (branding text, logos, login UI) are supplied
    /// by the consumer via parameters / slots — this VM has no product-specific
    /// DI dependencies, which is what makes the shell reusable across OutWit
    /// applications.
    /// </summary>
    public class AppShellLayoutViewModel : LayoutComponentBase
    {
        #region Constructors

        public AppShellLayoutViewModel()
        {
            InitDefaults();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Theme = ThemeFactory.Create();
            IsDark = false;
            IsDrawerOpened = true;
        }

        #endregion

        #region Functions

        /// <summary>Toggles between dark and light themes.</summary>
        public void ToggleTheme()
        {
            IsDark = !IsDark;
            StateHasChanged();
        }

        /// <summary>Toggles the navigation drawer open/closed state.</summary>
        public void ToggleDrawer()
        {
            IsDrawerOpened = !IsDrawerOpened;
            StateHasChanged();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles responsive breakpoint changes. Collapses the drawer on mobile.
        /// </summary>
        public void OnBreakpointChanged(Breakpoint breakpoint)
        {
            IsMobile = breakpoint == Breakpoint.Md;
            if (IsMobile)
                IsDrawerOpened = false;
            StateHasChanged();
        }

        #endregion

        #region Parameters

        /// <summary>Window title (rendered into &lt;HeadContent&gt;&lt;title&gt;).</summary>
        [Parameter]
        public string Title { get; set; } = string.Empty;

        /// <summary>App name shown next to the logo in the AppBar.</summary>
        [Parameter]
        public string Header { get; set; } = string.Empty;

        /// <summary>Meta-description for the &lt;HeadContent&gt; block.</summary>
        [Parameter]
        public string Description { get; set; } = string.Empty;

        /// <summary>Logo used in the AppBar (on the dark navy background — usually a light/inverted logo).</summary>
        [Parameter]
        public string LogoDarkUrl { get; set; } = string.Empty;

        /// <summary>Logo URL used for the page favicon.</summary>
        [Parameter]
        public string LogoLightUrl { get; set; } = string.Empty;

        /// <summary>Navigation items rendered inside the drawer. Each application provides its own.</summary>
        [Parameter]
        public RenderFragment? NavigationItems { get; set; }

        /// <summary>
        /// Slot rendered at the right end of the AppBar, after the spacer and
        /// theme toggle. Typical contents: product-specific login/profile UI
        /// (e.g. WitIdentity's <c>&lt;LoginDisplay /&gt;</c>) and extra action buttons.
        /// </summary>
        [Parameter]
        public RenderFragment? HeaderEnd { get; set; }

        /// <summary>
        /// Slot rendered below the layout (outside MudLayout). Typical contents:
        /// a small version / environment badge.
        /// </summary>
        [Parameter]
        public RenderFragment? Footer { get; set; }

        /// <summary>
        /// Child content. When the shell is used as a nested component (inside
        /// another layout) pass <c>@Body</c> here. When used as a direct layout,
        /// the framework-provided <see cref="LayoutComponentBase.Body"/> is used
        /// automatically.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        #endregion

        #region Properties

        public MudTheme Theme { get; private set; } = null!;

        public bool IsDrawerOpened { get; set; }

        public bool IsDark { get; private set; }

        public bool IsMobile { get; private set; }

        #endregion
    }
}
