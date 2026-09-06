using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace RemoteAbandon.Services
{
    /// <summary>
    /// Tracks damage timestamps on grids to enforce combat abandonment delay cooldowns.
    /// </summary>
    public static class DamageTracker
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly ConcurrentDictionary<long, DateTime> LastDamageTimes = new ConcurrentDictionary<long, DateTime>();
        private static bool _isRegistered = false;

        /// <summary>
        /// Registers the damage event handler and entity removal listener.
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
                    MyEntities.OnEntityRemove += OnEntityRemove;
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
        /// Clears stored damage timestamps and unregisters handlers.
        /// </summary>
        public static void Cleanup()
        {
            if (_isRegistered)
            {
                MyEntities.OnEntityRemove -= OnEntityRemove;
                _isRegistered = false;
            }

            LastDamageTimes.Clear();
        }

        private static void OnEntityRemove(MyEntity entity)
        {
            if (entity is MyCubeGrid grid)
            {
                LastDamageTimes.TryRemove(grid.EntityId, out _);
            }
        }

        /// <summary>
        /// Records the damage timestamp for the target grid. Called on every damage event.
        /// </summary>
        private static void OnAfterDamageApplied(object target, MyDamageInformation info)
        {
            if (info.Amount <= 0f)
                return;

            var slim = target as MySlimBlock;
            if (slim?.CubeGrid == null)
                return;

            LastDamageTimes[slim.CubeGrid.EntityId] = DateTime.UtcNow;
        }

        /// <summary>
        /// Checks if any grid in the construct took damage within the cooldown window.
        /// </summary>
        public static bool IsConstructInDamageCooldown(IEnumerable<MyCubeGrid> grids, double cooldownSeconds, out int remainingSeconds)
        {
            remainingSeconds = 0;
            if (grids == null || cooldownSeconds <= 0)
                return false;

            // Cold path cleanup to prevent unpruned dict growth
            PurgeOldEntries(TimeSpan.FromSeconds(Math.Max(cooldownSeconds * 2, 300)));

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
        /// Purges damage timestamps older than maxAge.
        /// </summary>
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
