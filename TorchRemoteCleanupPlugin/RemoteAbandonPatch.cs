using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RemoteAbandon.Config;
using RemoteAbandon.Services;
using RemoteAbandon.Utils;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Network;
using VRageMath;

namespace RemoteAbandon
{
    /// <summary>
    /// Intercepts remote grid removal requests to convert them into derelict abandonments rather than immediate deletion.
    /// </summary>
    public static class RemoteAbandonPatch
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Registers the Torch prefix patch on Keen's <see cref="MyBlockLimits.RemoveBlocksBuiltByID"/> method.
        /// </summary>
        /// <param name="ctx">Torch patch context to register the prefix on.</param>
        public static void Patch(PatchContext ctx)
        {
            try
            {
                var targetMethod = typeof(MyBlockLimits).GetMethod(nameof(MyBlockLimits.RemoveBlocksBuiltByID), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (targetMethod != null)
                {
                    var prefixMethod = typeof(RemoteAbandonPatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    ctx.GetPattern(targetMethod).Prefixes.Add(prefixMethod);
                    Log.Info("Registered MyBlockLimits.RemoveBlocksBuiltByID prefix patch.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to patch MyBlockLimits.RemoveBlocksBuiltByID!");
            }
        }

        /// <summary>
        /// Intercepts the player's 'X' (Remove) click in the Info Tab menu on the server.
        /// </summary>
        /// <param name="gridEntityId">Entity ID of the root grid to abandon or remove.</param>
        /// <returns>False to suppress Keen's default deletion routine and handle abandonment; true to fall back to vanilla deletion.</returns>
        public static bool Prefix(long gridEntityId)
        {
            try
            {
                var config = Plugin.Instance?.Config;

                // If plugin config is null (failed to init) or disabled, fallback to vanilla Keen deletion routine
                if (config == null || !config.Enabled)
                {
                    Log.Info($"Remote Abandon: Plugin disabled in config. Allowing vanilla full deletion for grid {gridEntityId}.");
                    return true;
                }

                // 1. Identify the player sending the RPC
                ulong senderSteamId = MyEventContext.Current.Sender.Value;
                long senderIdentityId = MySession.Static.Players.TryGetIdentityId(senderSteamId);

                if (senderIdentityId == 0L)
                {
                    Log.Warn($"Remote abandon request from unknown Steam ID: {senderSteamId}");
                    return false;
                }

                // 2. Fetch the target grid
                if (!MyEntities.TryGetEntityById(gridEntityId, out MyCubeGrid rootGrid) || rootGrid == null || rootGrid.Closed || rootGrid.MarkedForClose)
                {
                    Log.Warn($"Remote abandon requested for non-existent or closing grid ID: {gridEntityId}");
                    return false;
                }

                // 3. Max PCU Limit check
                if (config != null && config.MaxGridPCU > 0 && rootGrid.BlocksPCU > config.MaxGridPCU)
                {
                    Log.Warn($"Player {senderSteamId} attempted to abandon grid '{rootGrid.DisplayName}' exceeding max PCU limit ({rootGrid.BlocksPCU} > {config.MaxGridPCU}).");
                    if (config.SendNotificationToPlayer)
                    {
                        ChatUtils.SendNotificationToPlayer(senderSteamId, $"Cannot abandon grid: PCU exceeds limit ({rootGrid.BlocksPCU} / {config.MaxGridPCU}).", font: MyFontEnum.Red);
                    }
                    return false;
                }

                // 4. Collect the root grid and any attached subgrids (rotors, pistons, hinges)
                var logicalGroup = MyCubeGridGroups.Static.Logical.GetGroup(rootGrid);
                List<MyCubeGrid> allGrids = logicalGroup != null
                    ? logicalGroup.Nodes.Select(n => n.NodeData).ToList()
                    : new List<MyCubeGrid> { rootGrid };

                // 5. Combat & Anti-Exploit Restrictions
                if (config != null)
                {
                    // 5a. Proximity Check (hostile players nearby)
                    if (config.PreventAbandonInCombat)
                    {
                        Vector3D gridPos = rootGrid.PositionComp.GetPosition();
                        double checkRadiusSq = config.CombatCheckRadius * config.CombatCheckRadius;
                        bool enemyNearby = false;

                        foreach (var player in MySession.Static.Players.GetOnlinePlayers())
                        {
                            if (player == null || player.Identity == null || player.Identity.IdentityId == senderIdentityId)
                                continue;

                            if (Vector3D.DistanceSquared(player.GetPosition(), gridPos) <= checkRadiusSq)
                            {
                                var relation = MyIDModule.GetRelationPlayerPlayer(senderIdentityId, player.Identity.IdentityId);
                                if (relation.ToString().Equals("Enemies", StringComparison.OrdinalIgnoreCase))
                                {
                                    enemyNearby = true;
                                    break;
                                }
                            }
                        }

                        if (enemyNearby)
                        {
                            Plugin.Instance?.Statistics?.RecordCombatBlocked();
                            Log.Warn($"Player {senderSteamId} attempted to abandon grid '{rootGrid.DisplayName}' while enemy player is within {config.CombatCheckRadius}m combat radius.");
                            if (config.SendNotificationToPlayer)
                            {
                                ChatUtils.SendNotificationToPlayer(senderSteamId, "Cannot abandon grid: Hostile players detected nearby!", font: MyFontEnum.Red);
                            }
                            return false;
                        }
                    }

                    // 5b. Damage Cooldown Check (grid took damage recently)
                    if (config.PreventAbandonOnDamage && config.DamageCooldownSeconds > 0)
                    {
                        if (DamageTracker.IsConstructInDamageCooldown(allGrids, config.DamageCooldownSeconds, out int remainingSeconds))
                        {
                            Plugin.Instance?.Statistics?.RecordCombatBlocked();
                            Log.Warn($"Player {senderSteamId} attempted to abandon grid '{rootGrid.DisplayName}' which took damage recently ({remainingSeconds}s cooldown remaining).");
                            if (config.SendNotificationToPlayer)
                            {
                                ChatUtils.SendNotificationToPlayer(senderSteamId, $"Cannot abandon grid: Grid took damage recently! Please wait {remainingSeconds}s.", font: MyFontEnum.Red);
                            }
                            return false;
                        }
                    }
                }

                int totalBeaconsDestroyed = 0;
                int totalPcuRefunded = 0;

                bool destroyBeacons = config?.DestroyPlayerBeacons ?? true;
                bool preserveOtherBeacons = config?.PreserveOtherPlayerBeacons ?? true;
                bool depowerGrid = config?.DepowerGridOnAbandon ?? false;
                bool resetOwnership = config?.ResetTerminalOwnershipToNobody ?? true;
                bool transferAuthorship = config?.TransferAuthorshipToNobody ?? true;
                long targetOwnerId = config?.CustomOwnerIdentityId ?? 0L;

                foreach (MyCubeGrid grid in allGrids)
                {
                    if (grid == null || grid.Closed || grid.MarkedForClose)
                        continue;

                    // A. Collect Beacons to remove
                    if (destroyBeacons)
                    {
                        var beaconsToRemove = new List<MySlimBlock>();
                        foreach (MySlimBlock slim in grid.CubeBlocks)
                        {
                            if (slim.FatBlock is MyBeacon beacon)
                            {
                                bool ownedByOther = beacon.OwnerId != 0L && beacon.OwnerId != senderIdentityId;
                                if (preserveOtherBeacons && ownedByOther)
                                {
                                    continue; // Keep scrap/claim beacons owned by other identities
                                }

                                if (beacon.OwnerId == senderIdentityId || slim.BuiltBy == senderIdentityId)
                                {
                                    beaconsToRemove.Add(slim);
                                }
                            }
                        }

                        if (config.EnableDebugLogging)
                            Log.Info($"[DEBUG] Step A: Found {beaconsToRemove.Count} beacons to destroy on grid '{grid.DisplayName}'");

                        foreach (MySlimBlock beacon in beaconsToRemove)
                        {
                            grid.RemoveBlock(beacon, updatePhysics: true);
                            totalBeaconsDestroyed++;
                        }
                    }

                    // B. Depower functional power blocks (Reactors, Batteries, Solar Panels, Wind Turbines, Hydrogen Engines, modded power blocks)
                    if (depowerGrid)
                    {
                        int depoweredCount = 0;
                        foreach (MySlimBlock slim in grid.CubeBlocks)
                        {
                            if (slim.FatBlock is Sandbox.ModAPI.IMyPowerProducer powerProducer)
                            {
                                powerProducer.Enabled = false;
                                depoweredCount++;
                            }
                            else if (slim.FatBlock is MyFunctionalBlock functional)
                            {
                                string typeName = functional.GetType().Name;
                                if (typeName.Contains("Generator") || typeName.Contains("Solar") || typeName.Contains("Wind") || 
                                    typeName.Contains("Reactor") || typeName.Contains("Engine") || typeName.Contains("Battery"))
                                {
                                    functional.Enabled = false;
                                    depoweredCount++;
                                }
                            }
                        }

                        if (config.EnableDebugLogging)
                            Log.Info($"[DEBUG] Step B: Depowered {depoweredCount} power blocks on grid '{grid.DisplayName}'");
                    }

                    // C. Reset terminal block ownership
                    if (resetOwnership)
                    {
                        int ownershipResetCount = 0;
                        foreach (MySlimBlock slim in grid.CubeBlocks)
                        {
                            if (slim.FatBlock is MyTerminalBlock terminalBlock && !(slim.FatBlock is MyBeacon))
                            {
                                if (terminalBlock.OwnerId != 0L)
                                {
                                    terminalBlock.ChangeBlockOwnerRequest(targetOwnerId, MyOwnershipShareModeEnum.None);
                                    ownershipResetCount++;
                                }
                            }
                        }

                        if (config.EnableDebugLogging)
                            Log.Info($"[DEBUG] Step C: Reset ownership on {ownershipResetCount} terminal blocks on grid '{grid.DisplayName}'");
                    }

                    // D. Transfer authorship to refund PCU on server and client
                    if (transferAuthorship)
                    {
                        totalPcuRefunded += grid.BlocksPCU;
                        grid.TransferBlocksBuiltByID(senderIdentityId, targetOwnerId);

                        MyMultiplayer.RaiseStaticEvent(
                            (IMyEventOwner x) => MyBlockLimits.TransferBlocksBuiltByIDClient,
                            grid.EntityId,
                            senderIdentityId,
                            targetOwnerId
                        );

                        if (config.EnableDebugLogging)
                            Log.Info($"[DEBUG] Step D: Transferred authorship for {grid.BlocksPCU} PCU on grid '{grid.DisplayName}'");
                    }
                }

                // 6. Record live statistics
                Plugin.Instance?.Statistics?.RecordGridAbandoned(
                    allGrids.Count,
                    totalBeaconsDestroyed,
                    totalPcuRefunded,
                    rootGrid.DisplayName,
                    senderSteamId
                );

                string logMsg = $"Player {senderSteamId} (Identity {senderIdentityId}) abandoned grid '{rootGrid.DisplayName}' ({allGrids.Count} subgrids, {totalBeaconsDestroyed} beacons destroyed, {totalPcuRefunded} PCU refunded).";

                if (config.EnableDebugLogging)
                {
                    Log.Info($"[DEBUG] {logMsg}");
                }
                else
                {
                    Log.Info(logMsg);
                }

                // 7. Write to dedicated plugin log file
                if (config?.WriteDedicatedLogFile == true && Plugin.Instance != null)
                {
                    try
                    {
                        string logPath = Path.Combine(Plugin.Instance.StoragePath, "RemoteAbandon.log");
                        string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logMsg}{Environment.NewLine}";
                        File.AppendAllText(logPath, logLine);
                    }
                    catch (Exception logEx)
                    {
                        Log.Warn(logEx, "Failed to write to dedicated RemoteAbandon.log file.");
                    }
                }

                // 8. Send notification to player
                if (config?.SendNotificationToPlayer == true)
                {
                    string msg = string.Format(config.NotificationMessage ?? "Grid '{0}' was abandoned as a derelict. PCU refunded.", rootGrid.DisplayName);
                    ChatUtils.SendNotificationToPlayer(senderSteamId, msg);
                }

                // Return false: Cancels Keen's vanilla routine so the rest of the ship is NOT deleted!
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception in RemoteAbandonPatch.Prefix for grid {gridEntityId}");
                return true; // Let Keen's vanilla deletion run as a safe fallback
            }
        }
    }
}

