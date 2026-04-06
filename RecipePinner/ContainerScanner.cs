using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimRecipePinner
{
    public class ContainerScanner
    {
        public static List<Container> AllContainers = new List<Container>();
        private static readonly HashSet<Container> _containerSet = new HashSet<Container>();
        internal static readonly object ContainerLock = new object();

        public Dictionary<string, int> ContainerCache = new Dictionary<string, int>();

        private static readonly HashSet<int> _processedIDs = new HashSet<int>();
        private readonly List<Container> _snapshotBuffer = new List<Container>();
        private Vector3 _lastScanPos;
        private int _lastItemCount = 0;
        private float _scanTimer = 0f;
        private float _moveScanCooldown = 0f;

        private const float MovementThresholdSqr = 4.0f;
        private const float MinMoveScanCooldown = 1.0f;

        public void InitializeContainers()
        {
            if (!RecipePinnerPlugin.EnableChestScanning.Value) return;

            DebugLogger.Log("Init containers");

            lock (ContainerLock)
            {
                if (AllContainers.Count == 0)
                {
                    Container[] existingContainers = Object.FindObjectsByType<Container>(FindObjectsSortMode.None);

                    foreach (var container in existingContainers)
                    {
                        if (container != null && _containerSet.Add(container))
                        {
                            AllContainers.Add(container);
                            if (container.GetComponent<ContainerTracker>() == null)
                            {
                                ContainerTracker tracker = container.gameObject.AddComponent<ContainerTracker>();
                                tracker.MyContainer = container;
                            }
                        }
                    }

                    DebugLogger.Log($"Tracking {AllContainers.Count} containers");
                }
            }
        }

        public void UpdateScanning()
        {
            if (Player.m_localPlayer == null) return;

            _scanTimer += Time.deltaTime;
            _moveScanCooldown += Time.deltaTime;
            float distSqr = Vector3.SqrMagnitude(Player.m_localPlayer.transform.position - _lastScanPos);
            bool playerMoved = distSqr > MovementThresholdSqr && _moveScanCooldown >= MinMoveScanCooldown;

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

            bool timerExpired = _scanTimer >= dynamicInterval;
            if (playerMoved || inventoryChanged || timerExpired)
            {
                DebugLogger.Verbose($"Scanning containers - Moved: {playerMoved}, InvChanged: {inventoryChanged}, Interval: {timerExpired}");
                _scanTimer = 0f;
                if (playerMoved) _moveScanCooldown = 0f;
                _lastItemCount = currentCount;
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

            _snapshotBuffer.Clear();
            lock (ContainerLock)
            {
                _snapshotBuffer.AddRange(AllContainers);
            }

            _processedIDs.Clear();

            int scannedCount = 0;
            int skippedCount = 0;
            int accessDeniedCount = 0;

            foreach (var container in _snapshotBuffer)
            {
                if (container == null || container.transform == null)
                {
                    skippedCount++;
                    continue;
                }

                int id = container.GetInstanceID();
                if (!_processedIDs.Add(id))
                {
                    skippedCount++;
                    continue;
                }

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
            if (__instance == null) return;
            if (RecipePinnerPlugin.EnableChestScanning == null || !RecipePinnerPlugin.EnableChestScanning.Value) return;

            {
                lock (ContainerLock)
                {
                    if (_containerSet.Add(__instance))
                    {
                        AllContainers.Add(__instance);

                        ContainerTracker tracker = __instance.gameObject.GetComponent<ContainerTracker>()
                            ?? __instance.gameObject.AddComponent<ContainerTracker>();
                        tracker.MyContainer = __instance;

                        DebugLogger.Verbose($"New container tracked: {__instance.name} (Total: {AllContainers.Count})");
                    }
                }
            }
        }

        public static void RemoveFromSet(Container c)
        {
            _containerSet.Remove(c);
        }
    }

    public class ContainerTracker : MonoBehaviour
    {
        public Container MyContainer;

        private void OnDestroy()
        {
            if (ContainerScanner.AllContainers != null && MyContainer != null &&
                RecipePinnerPlugin.EnableChestScanning != null && RecipePinnerPlugin.EnableChestScanning.Value)
            {
                lock (ContainerScanner.ContainerLock)
                {
                    ContainerScanner.AllContainers.Remove(MyContainer);
                    ContainerScanner.RemoveFromSet(MyContainer);
                    DebugLogger.Verbose($"Container removed: {MyContainer.name} (Remaining: {ContainerScanner.AllContainers.Count})");
                }
            }
        }
    }
}