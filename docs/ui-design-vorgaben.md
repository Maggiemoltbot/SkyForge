# SkyForge UI Design-Vorgaben

## Corporate Identity
- **Farben:** XFLIGHT Corporate Identity — Orange, Blau, Schwarz, Weiß
- **Referenz-Website:** board3000.de (xflight.de) — Schriftart und Farben ableiten
- **Logo:** Transparentes XFLIGHT Logo einbinden
- **Partner-Logos:** AI Cloud Partner-Logos im About/Credits-Bereich (nicht im Haupt-UI)

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
