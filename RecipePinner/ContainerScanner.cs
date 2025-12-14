using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimRecipePinner
{
    public class ContainerScanner
    {
        public static List<Container> AllContainers = new List<Container>();
        internal static readonly object ContainerLock = new object();

        public Dictionary<string, int> ContainerCache = new Dictionary<string, int>();

        private static HashSet<int> _processedIDs = new HashSet<int>();
        private Vector3 _lastScanPos;
        private int _lastItemCount = 0;
        private float _scanTimer = 0f;

        private const float MovementThresholdSqr = 4.0f;

        public void InitializeContainers()
        {
            DebugLogger.Log("Initializing container tracking...");

            lock (ContainerLock)
            {
                if (AllContainers.Count == 0)
                {
                    Container[] existingContainers = Object.FindObjectsByType<Container>(FindObjectsSortMode.None);

                    foreach (var container in existingContainers)
                    {
                        if (container != null && !AllContainers.Contains(container))
                        {
                            AllContainers.Add(container);
                            if (container.GetComponent<ContainerTracker>() == null)
                            {
                                ContainerTracker tracker = container.gameObject.AddComponent<ContainerTracker>();
                                tracker.MyContainer = container;
                            }
                        }
                    }

                    DebugLogger.Log($"Initialized tracking for {AllContainers.Count} existing containers");
                }
            }
        }

        public void UpdateScanning()
        {
            if (Player.m_localPlayer == null) return;

            _scanTimer += Time.deltaTime;
            float distSqr = Vector3.SqrMagnitude(Player.m_localPlayer.transform.position - _lastScanPos);
            bool playerMoved = distSqr > MovementThresholdSqr;

            int currentCount = 0;
            foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
            {
                currentCount += item.m_stack;
            }

            bool inventoryChanged = currentCount != _lastItemCount;

            bool isContainerOpen = false;
            if (InventoryGui.instance != null)
            {
                isContainerOpen = ReflectionHelper.GetCurrentContainer(InventoryGui.instance) != null;
            }

            float dynamicInterval = isContainerOpen ? 0.5f : RecipePinnerPlugin.ChestScanInterval.Value;

            if (playerMoved || inventoryChanged || _scanTimer >= dynamicInterval)
            {
                _scanTimer = 0f;
                _lastItemCount = currentCount;

                DebugLogger.Verbose($"Scanning containers - Moved: {playerMoved}, InvChanged: {inventoryChanged}, Interval: {_scanTimer >= dynamicInterval}");
                UpdateContainerCache();
            }
        }

        private void UpdateContainerCache()
        {
            ContainerCache.Clear();

            if (Player.m_localPlayer == null)
            {
                DebugLogger.Verbose("Cannot scan - player is null");
                return;
            }

            Vector3 center = Player.m_localPlayer.transform.position;
            float range = RecipePinnerPlugin.ChestScanRange.Value;
            float rangeSqr = range * range;

            List<Container> snapshot;
            lock (ContainerLock)
            {
                snapshot = new List<Container>(AllContainers);
            }

            _processedIDs.Clear();

            int scannedCount = 0;
            int skippedCount = 0;
            int accessDeniedCount = 0;

            foreach (var container in snapshot)
            {
                if (container == null || container.transform == null)
                {
                    skippedCount++;
                    continue;
                }

                int id = container.GetInstanceID();
                if (_processedIDs.Contains(id))
                {
                    skippedCount++;
                    continue;
                }
                _processedIDs.Add(id);

                float distSqr = Vector3.SqrMagnitude(container.transform.position - center);
                if (distSqr > rangeSqr)
                {
                    skippedCount++;
                    continue;
                }

                bool hasAccess = true;
                if (ReflectionHelper.CheckContainerAccess != null)
                {
                    hasAccess = ReflectionHelper.CheckContainerAccess(container, Player.m_localPlayer.GetPlayerID());
                }

                if (!hasAccess)
                {
                    accessDeniedCount++;
                    continue;
                }

                Inventory inv = container.GetInventory();
                if (inv != null)
                {
                    foreach (ItemDrop.ItemData item in inv.GetAllItems())
                    {
                        string name = item.m_shared.m_name;

                        if (ContainerCache.TryGetValue(name, out int currentVal))
                        {
                            ContainerCache[name] = currentVal + item.m_stack;
                        }
                        else
                        {
                            ContainerCache[name] = item.m_stack;
                        }
                    }
                    scannedCount++;
                }
            }

            _lastScanPos = center;

            DebugLogger.Verbose($"Container scan complete - Scanned: {scannedCount}, Skipped: {skippedCount}, AccessDenied: {accessDeniedCount}, UniqueItems: {ContainerCache.Count}");
        }

        [HarmonyPatch(typeof(Container), "Awake")]
        [HarmonyPostfix]
        public static void TrackContainerAwake(Container __instance)
        {
            if (__instance != null)
            {
                lock (ContainerLock)
                {
                    if (!AllContainers.Contains(__instance))
                    {
                        AllContainers.Add(__instance);

                        ContainerTracker tracker = __instance.gameObject.GetComponent<ContainerTracker>();
                        if (tracker == null)
                        {
                            tracker = __instance.gameObject.AddComponent<ContainerTracker>();
                        }
                        tracker.MyContainer = __instance;

                        DebugLogger.Verbose($"New container tracked: {__instance.name} (Total: {AllContainers.Count})");
                    }
                }
            }
        }
    }

    public class ContainerTracker : MonoBehaviour
    {
        public Container MyContainer;

        private void OnDestroy()
        {
            if (ContainerScanner.AllContainers != null && MyContainer != null)
            {
                lock (ContainerScanner.ContainerLock)
                {
                    ContainerScanner.AllContainers.Remove(MyContainer);
                    DebugLogger.Verbose($"Container removed: {MyContainer.name} (Remaining: {ContainerScanner.AllContainers.Count})");
                }
            }
        }
    }
}