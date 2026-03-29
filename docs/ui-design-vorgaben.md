# SkyForge UI Design-Vorgaben

## Corporate Identity (aus xflight-v3 Website, localhost:3000)

### Farben (CSS Custom Properties)
| Variable | Hex | Verwendung |
|----------|-----|------------|
| `--bg-primary` | `#0A0A0F` | Haupthintergrund (Near-Black) |
| `--bg-secondary` | `#111118` | Sekundärer Hintergrund |
| `--bg-card` | `#16161F` | Panel/Card Hintergrund |
| `--text-primary` | `#FFFFFF` | Haupttext |
| `--text-secondary` | `#9CA3AF` | Sekundärtext |
| `--accent-cyan` | `#00D4FF` | Primary Accent (Electric Blue) |
| `--accent-blue` | `#3B82F6` | Sekundärer Accent |
| `--accent-violet` | `#8B5CF6` | Tertiärer Accent |
| `--accent-orange` | `#FF6B00` | XFLIGHT Orange (CTA, Highlights) |
| `--accent-orange-light` | `#FF8C3A` | Orange Light (Hover) |

### Schrift
- **Primär:** `Inter` (Website) — in Unity ggf. als importierte TrueType Font
- **Alternative:** DIN (Corporate Identity PDF) — wenn verfügbar
- **Fallback:** `-apple-system, BlinkMacSystemFont, sans-serif` (Apple HIG konform)

### Assets
- **Logo:** Transparentes XFLIGHT Logo (aus Website `/public/` oder Corporate Identity Assets)
- **Partner-Logos:** AI Cloud Partner-Logos im About/Credits-Bereich
- **Website Source:** `/Users/rudi/Projects/xflight-v3/` (Next.js, Port 3000)

## Design-Stil
- **Apple macOS Human Interface Guidelines** — runde Kanten, Glaseffekte, native Ästhetik
- **Referenz:** Apple HIG für macOS (developer.apple.com/design/human-interface-guidelines/macos)
- **Inspiration:** DroneMapPro UI (früher für Rudi erstellt — runde Ecken, sauber, professionell)
- **Look:** Dunkel (Dark Mode), minimalistisch, modern, XFLIGHT Corporate

## Technische Umsetzung in Unity
- OnGUI oder Unity UI Toolkit (USS Stylesheets für CSS-ähnliches Styling)
- Runde Ecken via Custom Shader oder 9-Slice Sprites
- Semi-transparente Panels mit Blur-Hintergrund (wo möglich)
- Konsistente Schriftgrößen und Padding

## Menüstruktur (aus Recherche abgeleitet)
1. **Hauptmenü** — Logo animiert, Start/Settings/About
2. **Controller-Setup** — Wizard: "Bewege Throttle-Stick" → Auto-Detect + Manual Override
3. **Drohnen-Config** — Rates (RC Rate, Super Rate, Expo), Kamera-Tilt
4. **Map-Auswahl** — Grid mit Thumbnails, Splat-Count, Performance-Info
5. **Flug** — HUD + Kamera-Modi + Controller Config (F4)
6. **About/Credits** — XFLIGHT Logo + Partner-Logos + Versionsinfo
