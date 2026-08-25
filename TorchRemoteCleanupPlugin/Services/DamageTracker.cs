using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace RemoteAbandon.Services
{
    /// <summary>
    /// Monitors Space Engineers game damage events and tracks the most recent damage timestamp
    /// for grids to enforce anti-combat abandonment delay cooldowns.
    /// </summary>
    public static class DamageTracker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly ConcurrentDictionary<long, DateTime> LastDamageTimes = new ConcurrentDictionary<long, DateTime>();
        private static bool _isRegistered = false;

        /// <summary>
        /// Registers the damage event handler with the Space Engineers DamageSystem.
        /// </summary>
        public static void Init()
        {
            try
            {
                if (_isRegistered)
                    return;

                if (MyAPIGateway.Session?.DamageSystem != null)
                {
                    MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(0, OnAfterDamageApplied);
                    _isRegistered = true;
                    Log.Info("DamageTracker registered with Space Engineers DamageSystem.");
                }
                else
                {
                    Log.Warn("DamageTracker: MyAPIGateway.Session.DamageSystem is null! Could not register damage handler.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize DamageTracker!");
            }
        }

        /// <summary>
        /// Clears all stored damage timestamps and unregisters handlers upon session unload.
        /// </summary>
        public static void Cleanup()
        {
            LastDamageTimes.Clear();
            _isRegistered = false;
        }

        /// <summary>
        /// Handles the damage applied event and records the timestamp for the target grid.
        /// </summary>
        /// <param name="target">The damaged object (block or grid).</param>
        /// <param name="info">Information regarding the damage amount and type.</param>
        private static void OnAfterDamageApplied(object target, MyDamageInformation info)
        {
            try
            {
                if (info.Amount <= 0f)
                    return;

                long gridEntityId = 0;
                if (target is MySlimBlock slimBlock && slimBlock.CubeGrid != null)
                {
                    gridEntityId = slimBlock.CubeGrid.EntityId;
                }
                else if (target is MyCubeBlock fatBlock && fatBlock.CubeGrid != null)
                {
                    gridEntityId = fatBlock.CubeGrid.EntityId;
                }
                else if (target is MyCubeGrid grid)
                {
                    gridEntityId = grid.EntityId;
                }
                else if (target is IMySlimBlock iSlim && iSlim.CubeGrid != null)
                {
                    gridEntityId = iSlim.CubeGrid.EntityId;
                }
                else if (target is IMyCubeGrid iGrid)
                {
                    gridEntityId = iGrid.EntityId;
                }

                if (gridEntityId != 0)
                {
                    LastDamageTimes[gridEntityId] = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing damage event in DamageTracker.");
            }
        }

        /// <summary>
        /// Checks if any grid in the provided construct (root grid and connected subgrids) has taken damage
        /// within the specified cooldown duration.
        /// </summary>
        /// <param name="grids">Collection of cube grids in the construct.</param>
        /// <param name="cooldownSeconds">Cooldown duration in seconds.</param>
        /// <param name="remainingSeconds">Outputs the remaining cooldown time in seconds.</param>
        /// <returns>True if the construct took damage within the cooldown window; otherwise, false.</returns>
        public static bool IsConstructInDamageCooldown(IEnumerable<MyCubeGrid> grids, double cooldownSeconds, out int remainingSeconds)
        {
            remainingSeconds = 0;
            if (grids == null || cooldownSeconds <= 0)
                return false;

            DateTime now = DateTime.UtcNow;
            DateTime mostRecentDamage = DateTime.MinValue;

            foreach (var grid in grids)
            {
                if (grid == null)
                    continue;

                if (LastDamageTimes.TryGetValue(grid.EntityId, out DateTime lastTime))
                {
                    if (lastTime > mostRecentDamage)
                        mostRecentDamage = lastTime;
                }
            }

            if (mostRecentDamage > DateTime.MinValue)
            {
                double elapsed = (now - mostRecentDamage).TotalSeconds;
                if (elapsed < cooldownSeconds)
                {
                    remainingSeconds = (int)Math.Ceiling(cooldownSeconds - elapsed);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Purges damage timestamps older than the specified max age to keep memory footprint minimal.
        /// </summary>
        /// <param name="maxAge">Maximum age threshold to retain.</param>
        public static void PurgeOldEntries(TimeSpan maxAge)
        {
            DateTime cutoff = DateTime.UtcNow - maxAge;
            foreach (var kvp in LastDamageTimes)
            {
                if (kvp.Value < cutoff)
                {
                    LastDamageTimes.TryRemove(kvp.Key, out _);
                }
            }
        }
    }
}
