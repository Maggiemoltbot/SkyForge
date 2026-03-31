# SkyForge Final Bugfix Sweep — 2026-03-31

## Summary
- Unity Start Screen no longer throws null references when elements are missing or delayed during load.
- Flight Dynamics Bridge now reuses UDP sockets safely, guards against partial initialisation, and logs cleanly on macOS.
- Betaflight SITL builds reliably with Apple clang; macOS fallbacks for `clock_gettime`/`nanosleep` keep the firmware portable.
- `start_sitl.sh` skips Intel HEX generation (not supported for 64-bit binaries) and rebuilds with `OBJCOPY=true` automatically.
- Documentation updated to capture root causes, fixes, and follow-up actions.

## 1. Unity Console Errors
- **Issue:** Start screen initialisation accessed UI Toolkit elements before the UIDocument resolved, producing `NullReferenceException` spam.
- **Fix:** Added defensive guards to `StartScreenController` to verify queried elements, lazily register event handlers, and fail gracefully when the `UIManager` reference is missing.
- **Validation:** Cleared `Editor.log` tail after reload — no new `NullReferenceException` entries during scene boot, only expected info logs.

## 2. FlightDynamicsBridge
- **Issue:** UDP receivers reported `Address already in use` when Unity hot-reloaded scripts or SITL restarted, leaving sockets bound.
- **Fixes:**
  - Wrap socket setup in `InitializeBridge()` with `ReuseAddress` set on all clients and ensure `CleanupSockets()` runs on both success and failure paths.
  - Guard RC sending against uninitialised endpoints and surface invalid config via log.
- **Validation:** Unity reloaded scripts twice; FDM/PWM sockets re-bound without throwing. `Editor.log` shows clean reboot (`Shutting down Flight Dynamics Bridge...` followed by successful init messages).

## 3. UI Functionality Checks
- **Start Screen:** Buttons instantiate even if the UIDocument resolves late; `Fly` stays disabled until a map is picked.
- **Controller Setup:** Now protected against missing `UIManager`, avoiding crashes when the panel isn't wired yet.
- **HUD:** No code changes required; behaviour verified indirectly via absence of runtime errors while HUD was active (Unity logs).

## 4. SITL Integration
- **Root Cause:** Betaflight upstream assumes GNU toolchain flags (`-Ofast`, `-fuse-linker-plugin`, `-Werror`) and Linux-specific timing APIs. Apple clang treats them as errors and lacks `clock_gettime`/`nanosleep` in strict POSIX mode.
- **Changes:**
  - `beta/mk/mcu/SITL.mk`: swap `-Ofast` → `-O3 -ffast-math`, drop GCC-only linker flags, add macOS-safe extra flags, disable HEX generation, and ensure include paths resolve.
  - `beta/src/main/target/SITL/sitl.c`: add Apple-specific fallbacks using `gettimeofday`/`usleep` to emulate monotonic timers.
  - `tools/start_sitl.sh`: build with `OBJCOPY=true` so 64-bit Mach-O builds succeed without failing on Intel HEX output.
- **Validation:**
  - `make TARGET=SITL OBJCOPY=true` (with patched Makefile) succeeds on macOS 15.3 arm64.
  - `./tools/start_sitl.sh` launches SITL, binds TCP 5761, and `./tools/stop_sitl.sh` shuts it down cleanly.

## 5. Gamepad / DJI C5 Controller
- **Code state:** Input pipeline already maps arm/disarm to Button B via `ControllerInputProfile`.
- **Hardware check:** DJI C5 not connected during this pass; no runtime errors when the input system initialises without it. Next tethered session should confirm detection and OSD mode display.

## Follow-up Actions
- When DJI C5 is available, plug in via USB, verify detection in the Controller Setup screen, confirm `B` arms via SITL, and ensure HUD flight-mode indicator toggles.
- After the next Unity session, consider suppressing repeated Unity Cloud curl warnings if offline (optional).
