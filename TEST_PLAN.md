# 🚀 Remote Grid Abandon: Master Test Plan

Comprehensive validation and test matrix for the **Remote Grid Abandon** Torch plugin on Space Engineers dedicated servers.

---

## 📋 Test Environment Prerequisites

* **Torch Server**: Running with `RemoteAbandon.zip` loaded in `Torch\Plugins`.
* **Testing Roles**:
  * **Tester A (Admin / Main Player)**: Primary tester for commands, grid abandonment, and UI telemetry.
  * **Tester B (Secondary Account / Faction Mate / Enemy)**: Auxiliary tester for cross-faction beacon preservation and combat lockouts.
* **Test Grid Blueprint Arsenal**:
  1. **Standard Rover / Ship**: Cockpit, battery, small reactor, solar panel, beacon, cargo container, armor (~500 PCU).
  2. **Multi-Subgrid Heavy Construct**: Main chassis with a rotor turret, piston lift, hinge ramp, and a subgrid beacon attached to each subpart.
  3. **Multi-Owner Hybrid Grid**: Grid built by Tester A, with an attached beacon built/owned by Tester B (simulating a salvage claim or scrap marker).
  4. **Oversized Heavy Cruiser**: High-PCU grid (> 5,000 PCU) to test `MaxGridPCU` cutoff thresholds.

---

## Phase 1: Torch GUI & Configuration Persistence

| # | Test Case | Action / Procedure | Expected Result | Status |
|---|---|---|---|:---:|
| **1.1** | **Plugin Initialization** | Launch Torch Server and observe server startup log. | Log confirms: `Remote Abandon plugin initialized and Torch patches applied successfully.` | [ ] |
| **1.2** | **WPF Control Tab** | Open Torch GUI → click the **Remote Grid Abandon** tab. | GUI renders dark-theme layout with two tabs (**Configuration** and **Live Telemetry**) and all group boxes populated. | [ ] |
| **1.3** | **Config Save & Disk Sync** | 1. In GUI, toggle `Enable Debug Logging` to **ON**.<br>2. Set `Combat Check Radius` to `4500`.<br>3. Click **Save Configuration** button. | • Pop-up modal confirms: `"Remote Grid Abandon configuration saved successfully!"`<br>• Inspect `Torch\Plugins\Storage\RemoteAbandon\RemoteAbandon.cfg` to verify XML values updated. | [ ] |
| **1.4** | **Config Reload Command** | In Torch Console or chat, run: `!abandon reload`. | Console outputs: `Remote Grid Abandon configuration reloaded from disk.` | [ ] |

---

## Phase 2: Core Abandonment & PCU Reclamation (Basic Grid)

| # | Test Case | Action / Procedure | Expected Result | Status |
|---|---|---|---|:---:|
| **2.1** | **Derelict Preservation (Anti-Despawn)** | 1. Tester A spawns Test Grid 1.<br>2. Open **Terminal (K) → Info Tab**.<br>3. Locate grid in the list and click **"X"** (Remove Grid). | • **Grid remains physically in the world** (Keen's destructive despawn is intercepted and prevented).<br>• Grid stays stationary without physics jitter or Havok solver runaway. | [ ] |
| **2.2** | **Instant PCU Refund & Info Tab Removal** | Note Tester A's total PCU before clicking "X". Click "X". | • Tester A's PCU instantly drops by the grid's PCU value.<br>• The grid vanishes from Tester A's Info Tab list immediately. | [ ] |
| **2.3** | **HUD Alert Notification** | Observe Tester A's HUD after clicking "X". | Green on-screen notification displays: `"Grid '[GridName]' was abandoned as a derelict. PCU refunded."` | [ ] |
| **2.4** | **Live Telemetry Increment** | Switch to Torch GUI → **Live Telemetry** tab. | • **Grids Abandoned** increments by `+1`.<br>• **Total PCU Refunded** increases by grid's PCU.<br>• **Last Abandoned Grid** shows grid name and player Steam ID. | [ ] |

---

## Phase 3: Smart Beacon Stripping & Multi-Subgrid Traversal

| # | Test Case | Action / Procedure | Expected Result | Status |
|---|---|---|---|:---:|
| **3.1** | **Player Beacon Destruction** | 1. Ensure `DestroyPlayerBeacons = true`.<br>2. Spawn a grid with an active broadcasting beacon.<br>3. Click "X" in Info Tab. | • The beacon block is removed from the grid.<br>• Broadcast HUD signal disappears immediately.<br>• In GUI, **Beacons Destroyed** increments by `+1`. | [ ] |
| **3.2** | **Multi-Subgrid Traversal (Rotors / Hinges / Pistons)** | 1. Spawn Test Grid 2 (Chassis + 3 subgrids, each with a beacon).<br>2. Click "X" in Info Tab on the root grid. | • All 4 beacons across main hull and all subgrids are stripped.<br>• Authorship across all subparts transferred.<br>• Telemetry reports: `Subgrids Handled: +4`, `Beacons Destroyed: +4`. | [ ] |
| **3.3** | **Cross-Faction / Claim Beacon Preservation** | 1. Ensure `PreserveOtherPlayerBeacons = true`.<br>2. Spawn Test Grid 3.<br>3. Tester A owns the grid & Beacon 1.<br>4. Tester B (different faction) places Beacon 2 (`"CLAIMED / SALVAGE"`).<br>5. Tester A clicks "X" in Info Tab. | • Tester A's Beacon 1 is **destroyed**.<br>• Tester B's Beacon 2 **remains intact and broadcasting**.<br>• Salvage markers/claim beacons stay visible across the desert. | [ ] |
| **3.4** | **Keep Beacons Intact Toggle** | 1. Set `DestroyPlayerBeacons = false`.<br>2. Abandon a grid with a beacon. | • The beacon remains physically on the grid and is not removed. | [ ] |

---

## Phase 4: Power State & Ownership Reset

| # | Test Case | Action / Procedure | Expected Result | Status |
|---|---|---|---|:---:|
| **4.1** | **Depower Grid on Abandon (Active)** | 1. Set `DepowerGridOnAbandon = true`.<br>2. Spawn grid with running reactor, hydrogen engine, and charged battery.<br>3. Click "X" in Info Tab. | • All batteries, reactors, hydrogen engines, solar panels, and wind turbines toggle to **Off** (`Enabled = false`).<br>• Grid lights/thrusters go dark. | [ ] |
| **4.2** | **Depower Grid on Abandon (Disabled)** | 1. Set `DepowerGridOnAbandon = false`.<br>2. Abandon an active grid. | • Power sources remain in their pre-abandon state (e.g. reactors stay on). | [ ] |
| **4.3** | **Ownership Reset to Nobody (`0L`)** | 1. Set `ResetTerminalOwnershipToNobody = true` and `CustomOwnerIdentityId = 0`.<br>2. Abandon grid with functional turrets, cockpits, and doors.<br>3. Inspect blocks with admin tools or character terminal. | • All terminal blocks show **Owner: Nobody**.<br>• Turrets cease targeting neutral/friendly players. | [ ] |
| **4.4** | **Custom NPC Identity Assignment** | 1. Set `CustomOwnerIdentityId` to a valid NPC Identity ID (e.g., `GAALSIEN` or `SPRT`).<br>2. Abandon grid. | • Terminal blocks and authorship transfer to the specified NPC Identity ID instead of `0L`. | [ ] |

---

## Phase 5: Combat Lockout & Anti-Exploit Restrictions

| # | Test Case | Action / Procedure | Expected Result | Status |
|---|---|---|---|:---:|
| **5.1** | **Combat Lockout (Hostile in Radius)** | 1. In GUI or chat, set: `PreventAbandonInCombat = true`, `CombatCheckRadius = 3000`.<br>2. Tester B (Enemy faction) stands 1,500m away from Tester A's ship.<br>3. Tester A clicks "X" in Info Tab. | • **Abandonment DENIED**.<br>• Red HUD alert: `"Cannot abandon grid: Hostile players detected nearby!"`<br>• Grid is NOT modified, PCU is NOT refunded.<br>• **Combat Lockouts** counter in GUI increments by `+1`. | [ ] |
| **5.2** | **Combat Lockout (Hostile Outside Radius)** | 1. Tester B moves 3,500m away (> 3,000m radius).<br>2. Tester A clicks "X" in Info Tab. | • **Abandonment ALLOWED**.<br>• Ship transitions to derelict and PCU is refunded normally. | [ ] |
| **5.3** | **Combat Lockout (Friendly/Neutral in Radius)** | 1. Tester B switches to Ally/Same Faction and stands 500m away.<br>2. Tester A clicks "X" in Info Tab. | • **Abandonment ALLOWED** (allies/neutrals do not trigger combat lockouts). | [ ] |
| **5.4** | **Max Grid PCU Restriction** | 1. Set `MaxGridPCU = 2000`.<br>2. Tester A tries to abandon Test Grid 4 (5,000 PCU). | • **Abandonment DENIED**.<br>• Red HUD alert: `"Cannot abandon grid: PCU exceeds limit (5000 / 2000)."`<br>• Grid remains untouched. | [ ] |
| **5.5** | **Damage Taken Cooldown (Active)** | 1. Set `PreventAbandonOnDamage = true`, `DamageCooldownSeconds = 60`.<br>2. Shoot or grind a block on the grid (or connected subgrid).<br>3. Immediately click "X" in Info Tab. | • **Abandonment DENIED**.<br>• Red HUD alert: `"Cannot abandon grid: Grid took damage recently! Please wait Xs."`<br>• Grid is NOT modified, PCU is NOT refunded.<br>• **Combat Lockouts** counter in GUI increments by `+1`. | [ ] |
| **5.6** | **Damage Taken Cooldown (Expired)** | 1. Wait until > 60 seconds have elapsed since damage was taken.<br>2. Click "X" in Info Tab. | • **Abandonment ALLOWED**.<br>• Grid transitions to derelict normally. | [ ] |

---

## Phase 6: Audit Logging & Debug Telemetry

| # | Test Case | Action / Procedure | Expected Result | Status |
|---|---|---|---|:---:|
| **6.1** | **Dedicated Log File (`RemoteAbandon.log`)** | 1. Ensure `WriteDedicatedLogFile = true`.<br>2. Abandon any test grid.<br>3. Open `Torch\Plugins\Storage\RemoteAbandon\RemoteAbandon.log`. | Log contains a new line formatted like:<br>`[2026-08-24 19:45:00] Player 7656119... (Identity 144...) abandoned grid 'Desert Scout' (2 subgrids, 1 beacons destroyed, 450 PCU refunded).` | [ ] |
| **6.2** | **Torch Console Debug Output** | 1. Enable `EnableDebugLogging = true`.<br>2. Abandon a multi-grid construct. | Torch console displays step-by-step trace:<br>• `[DEBUG] Step A: Found X beacons...`<br>• `[DEBUG] Step B: Depowered Y power blocks...`<br>• `[DEBUG] Step C: Reset ownership on Z blocks...`<br>• `[DEBUG] Step D: Transferred authorship for N PCU...` | [ ] |
| **6.3** | **Telemetry Reset (`ResetStats`)** | 1. In GUI, click **Reset Telemetry Counters** (or run `!abandon resetstats`). | • All 4 metric counters reset to `0`.<br>• Last abandoned grid resets to `"None"`, timestamp to `"Never"`. | [ ] |

---

## Phase 7: Chat & Console Commands (`!abandon`)

| # | Command | Required Permission | Expected Output / Behavior | Status |
|---|---|---|---|:---:|
| **7.1** | `!abandon info` *(with `EnablePlayerCommands = false`)* | Regular Player (`None`) | `"Player commands for Remote Grid Abandon are currently disabled."` | [ ] |
| **7.2** | `!abandon info` *(with `EnablePlayerCommands = true`)* | Regular Player (`None`) | Outputs full breakdown of server abandonment rules, beacon stripping, and combat limits. | [ ] |
| **7.3** | `!abandon status` | Admin | Displays complete current configuration parameters formatted in chat. | [ ] |
| **7.4** | `!abandon stats` | Admin | Displays real-time summary of all telemetry counters and last abandoned grid info. | [ ] |
| **7.5** | `!abandon toggle` | Admin | Switches plugin state. Chat reports: `Remote Grid Abandon Override is now DISABLED (Vanilla full deletion active)` / `ENABLED`. | [ ] |
| **7.6** | `!abandon set combatcheck true` | Admin | Config value updates dynamically. Chat confirms: `Set 'combatcheck' to 'true'. Configuration saved.` | [ ] |
| **7.7** | `!abandon set combatradius 5000` | Admin | Config updates `CombatCheckRadius` to 5000m. | [ ] |
| **7.8** | `!abandon set maxpcu 3500` | Admin | Config updates `MaxGridPCU` to 3500. | [ ] |
| **7.9** | `!abandon set message Abandoned: {0}` | Admin | Custom notification message template is updated and saved. | [ ] |

---

## Phase 8: Master Plugin Toggle & Vanilla Fallback Verification

| # | Test Case | Action / Procedure | Expected Result | Status |
|---|---|---|---|:---:|
| **8.1** | **Vanilla Deletion Passthrough** | 1. Set `Enabled = false` (via GUI or `!abandon toggle`).<br>2. Spawn a test ship.<br>3. Click "X" in Info Tab. | • Plugin allows Keen's vanilla routine to run.<br>• **Grid is completely deleted from the world**.<br>• Torch log records: `Plugin disabled in config. Allowing vanilla full deletion for grid [ID].` | [ ] |
| **8.2** | **Re-enable & Verify Derelict Behavior** | 1. Set `Enabled = true`.<br>2. Click "X" on a second ship. | • Derelict mode is active again; ship stays in world with beacon stripped and PCU refunded. | [ ] |

---

## 🎯 Recommended Test Execution Workflow

1. **Step 1**: Start with **Phase 1** (Torch GUI and config file sync).
2. **Step 2**: Execute **Phase 2 & 3** synchronously in-game with a standard rover and a multi-subgrid construct.
3. **Step 3**: Introduce a second player/test character for **Phase 3.3** (cross-faction salvage beacon preservation) and **Phase 5** (combat lockouts).
4. **Step 4**: Run all CLI commands in **Phase 7** to verify chat parsing, permissions, and live updates.
5. **Step 5**: Finish with **Phase 8** to ensure disabling the plugin cleanly falls back to Keen's stock deletion behavior without requiring a server reboot.

