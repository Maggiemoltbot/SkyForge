# Betaflight SITL — malloc Crash Fix (2026-03-31)

## Symptom

```
[timer]Init...
betaflight_SITL.elf(XXXX,0xXXXXXXXX) malloc: *** error for object 0x10:
pointer being freed was not allocated
*** set a breakpoint in malloc_error_break to debug
```

Crash happened on startup, after `[timer]Init...`, intermittently.
Was temporarily "fixed" by deleting `eeprom.bin` (reduced startup time, smaller race window).

## Root Cause

**Thread-safety race condition in `dyad` (TCP library).**

### Timeline of events during startup:

1. `systemInit()` → `pthread_create(&tcpWorker, tcpThread)` — tcpThread starts
2. `tcpThread` calls `dyad_init()` then enters `dyad_update()` loop immediately
3. Main thread continues: `timerInit()` → `serialInit()` → `serTcpOpen()` → `dyad_newStream()` + `dyad_listenEx()`
4. **RACE**: `dyad_update()` (tcpThread) runs concurrently with `dyad_newStream/listenEx` (main thread)
5. `dyad` is **not thread-safe** — its internal stream linked list is corrupted
6. Corrupted pointer (value `0x10` = stream's `address` field at offset 16) gets freed
7. macOS malloc detects the invalid free → crash

### Why `0x10`?

`dyad_Stream` struct layout on arm64:
```c
struct dyad_Stream {
    int state, flags;    // offset 0, 8 bytes
    dyad_Socket sockfd;  // offset 8, 4 bytes (+4 pad)
    char *address;       // offset 16 = 0x10  ← corrupted pointer freed here
    ...
};
```

### Why intermittent?

- Race window is narrow (~microseconds)
- Deleting `eeprom.bin` skips `resetEEPROM()` path, making startup slightly faster
  → smaller race window → crash less likely (but not fixed!)
- `-O3 -ffast-math` compiler flags can reorder operations, widening the race

## Fix

**Files changed:**
- `src/main/target/SITL/sitl.c` — add `pthread_cond_wait` in `tcpThread`, add `sitlTcpSerialReady()`
- `src/main/main.c` — call `sitlTcpSerialReady()` at start of `run()` (after `init()`)

**Mechanism:**
- `tcpThread` calls `dyad_init()` but then waits on a condition variable before entering `dyad_update()` loop
- Main thread completes `init()` (which includes `serialInit()` → all `dyad_newStream/listenEx` calls)
- At the start of `run()`, `sitlTcpSerialReady()` signals the condition
- `tcpThread` is now safe to start `dyad_update()` — no more concurrent dyad modification

```c
// In tcpThread (sitl.c):
pthread_mutex_lock(&tcpInitMutex);
while (!tcpInitDone) {
    pthread_cond_wait(&tcpInitCond, &tcpInitMutex);
}
pthread_mutex_unlock(&tcpInitMutex);
// Now safe to call dyad_update()

// In run() (main.c):
#ifdef SIMULATOR_BUILD
sitlTcpSerialReady();  // unblocks tcpThread
#endif
```

## Secondary Finding: EEPROM Version Mismatch

The `eeprom.bin` created by the old `2026.6.0-alpha` binary had `eepromConfigVersion = 178`.
Betaflight 4.5.1 expects version `177` (`EEPROM_CONF_VERSION = 177`).

**This is handled gracefully** — Betaflight calls `resetEEPROM()` on version mismatch,
which rewrites the file with version 177. After the first run, the file is correct.

This is NOT the root cause of the crash, but explains why deleting `eeprom.bin` sometimes
appeared to help (different code path, slightly different timing).

## Verification

Stress-tested with `MallocScribble=1 MallocGuardEdges=1` — 5/5 runs successful.

## Build

```bash
cd /Users/rudi/Projects/SkyForge/beta
make TARGET=SITL
```
