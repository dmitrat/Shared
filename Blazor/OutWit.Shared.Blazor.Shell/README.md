# OutWit.Shared.Blazor.Shell

Generic Material 3 admin shell for Blazor WebAssembly apps, built on
MudBlazor. Self-contained: no product-specific dependencies — drop into
any WASM app, pass branding via parameters, fill slots for product bits.

## What's inside

- **AppShellLayout** — top app bar (with menu button + theme toggle), mini
  navigation drawer, breakpoint-driven mobile collapse, Material 3 light /
  dark palettes via `ThemeFactory`.
- **ThemeFactory / ThemeDefaults** — the canonical OutWit theme (navy
  primary, lime accent).
- **CSS** at `_content/OutWit.Shared.Blazor.Shell/css/` —
  `m3-tokens.css`, `m3-appbar.css`, `m3-nav.css`, `shell.css`.
- **AppNotFound** page — wired to `/notfound`.
- **DisplayUtils** — name → initials, OIDC-claims → display name helpers.
- **LoginMenuItem** — record type used by product-side login UI components
  to define dropdown menu entries.

## Usage

`index.html`:

```html
<link rel="stylesheet" href="_content/OutWit.Shared.Blazor.Shell/css/m3-tokens.css" />
<link rel="stylesheet" href="_content/OutWit.Shared.Blazor.Shell/css/m3-appbar.css" />
<link rel="stylesheet" href="_content/OutWit.Shared.Blazor.Shell/css/m3-nav.css" />
<link rel="stylesheet" href="_content/OutWit.Shared.Blazor.Shell/css/shell.css" />
<!-- Material Symbols font + helper classes for the icons used by AppShellLayout -->
<link rel="stylesheet" href="_content/MudBlazor.FontIcons.MaterialSymbols/css/font.min.css" />
```

`MainLayout.razor`:

```razor
@inherits LayoutComponentBase

<AppShellLayout Title="My App"
                Header="My App"
                Description="..."
                LogoDarkUrl="/logo-light.svg"
                LogoLightUrl="/logo.svg">
    <NavigationItems>
        <MudNavLink Href="/" Icon="@Icons.Material.Outlined.Dashboard">Home</MudNavLink>
    </NavigationItems>
    <HeaderEnd>
        <!-- Product-specific: e.g. a login button -->
        <MudIconButton Icon="@Icons.Material.Outlined.AccountCircle" Color="Color.Inherit" />
    </HeaderEnd>
    <ChildContent>@Body</ChildContent>
</AppShellLayout>
```

## Parameters

| Parameter         | Type             | Purpose                                                                       |
| ----------------- | ---------------- | ----------------------------------------------------------------------------- |
| `Title`           | `string`         | `<title>` element via `<HeadContent>`.                                        |
| `Header`          | `string`         | App name shown in the app bar.                                                |
| `Description`     | `string`         | `<meta name="description">`.                                                  |
| `LogoDarkUrl`     | `string`         | Logo rendered on the dark navy app bar (usually a light / inverted logo).     |
| `LogoLightUrl`    | `string`         | URL used for the page favicon.                                                |
| `NavigationItems` | `RenderFragment` | Slot rendered inside the drawer's `MudNavMenu`.                               |
| `HeaderEnd`       | `RenderFragment` | Slot at the right end of the app bar (after spacer + theme toggle).           |
| `Footer`          | `RenderFragment` | Slot rendered below `MudLayout` for product-specific badges (e.g. version).   |
| `ChildContent`    | `RenderFragment` | Optional. When nested in another layout, pass `@Body`. When used directly, `Body` is used. |

## Versioning

Apache-2.0, MinVer-based. Targets `net10.0`; requires MudBlazor 9.4+.
