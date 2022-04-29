using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using BepInEx.Configuration;

namespace MyFirstPlugin
{
    [BepInPlugin(PluginName, "dsp", "1.1")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginName = "com.small.dsp.transferInfo";

        private XYModLib.UIWindow Window;

        private List<TransportInfo> arrayList = new List<TransportInfo>();
        internal static string search = "";
        internal static Dictionary<int, string> itemCache = new Dictionary<int, string>();
        internal static List<Cell> cells = new List<Cell>();
        internal static Vector2 scroll = new Vector2(0, 0);
        internal static double time = 0;

        public ConfigEntry<KeyCode> GUIHotkey;

        private void Awake()
        {
            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(DSPPatch));
            Logger.LogInfo($"Plugin {PluginName} is loaded!");
            Window = new XYModLib.UIWindow("Logistic Station Info");
            Window.Show = false;
            Window.OnWinodwGUI = guiInfo;
            Window.OnWindowOpen = () =>
            {
                arrayList.Clear();
                itemCache.Clear();
                cells.Clear();
                getInfo();
            };
            GUIHotkey = Config.Bind("Common", "GUIHotkey", KeyCode.F4, "Hotkey to show Logistic Station Info");
        }

        protected void OnDestroy()
        {
            Harmony.UnpatchAll();
        }

        void Update()
        {
            if (Input.GetKeyDown(GUIHotkey.Value))
            {
                if (!GameMain.isRunning && !GameMain.isPaused)
                {
                    Window.Show = false;
                    return;
                }
                Window.Show = !Window.Show;
            }
        }

        void getInfo()
        {
            StationComponent[] stations = GameMain.data.galacticTransport.stationPool;
            Dictionary<int, List<TransportInfo>> map = new Dictionary<int, List<TransportInfo>>();
            foreach (StationComponent station in stations)
            {
                if (station == null || station.isCollector || station.isVeinCollector)
                {
                    continue;
                }
                TransportInfo info = new TransportInfo();
                info.id = station.id;
                info.gid = station.gid;
                info.name = string.IsNullOrEmpty(station.name) ? "transfer-" + info.id : station.name;
                info.entityId = station.entityId;
                info.planetId = station.planetId;
                arrayList.Add(info);
                int galaxy = station.planetId / 100;
                if (!map.ContainsKey(galaxy))
                {
                    map.Add(galaxy, new List<TransportInfo>());
                }
                map[galaxy].Add(info);
            }
            foreach (var kv in map)
            {
                string name = GameMain.galaxy.StarById(kv.Key).displayName;
                kv.Value.Sort();
                cells.Add(new Cell(name, kv.Value, OpenTransfer));
            }
        }

        void UpdateItems()
        {
            StationComponent[] stations = GameMain.data.galacticTransport.stationPool;
            foreach (var info in arrayList)
            {
                var item = stations[info.gid];
                int i = -1;
                foreach (var storage in item.storage)
                {
                    i++;
                    if (storage.itemId <= 0) continue;
                    if (!itemCache.ContainsKey(storage.itemId))
                    {
                        itemCache.Add(storage.itemId, LDB.ItemName(storage.itemId));
                    }
                    info.items[i] = $"{itemCache[storage.itemId]}\n{storage.count}";
                    if (i >= 4) break;
                }
            }
        }

        void OpenTransfer(TransportInfo item)
        {
            var uigame = UIRoot.instance.uiGame;
            uigame.OpenPlayerInventory();
            var win = uigame.stationWindow;
            win.player = GameMain.mainPlayer;
            win.factory = GameMain.data.GetOrCreateFactory(GameMain.data.galaxy.PlanetById(item.planetId));
            win.transport = win.factory.transport;
            win.powerSystem = win.factory.powerSystem;
            win.factorySystem = win.factory.factorySystem;

            win.nameInput.onValueChanged.AddListener(new UnityAction<string>(win.OnNameInputSubmit));
            win.nameInput.onEndEdit.AddListener(new UnityAction<string>(win.OnNameInputSubmit));
            win._Open();
            win.stationId = item.id;
            Window.Show = false;
        }

        void OnGUI()
        {
            Window.OnGUI();
        }

        void guiInfo()
        {
            if (GameMain.gameTime - time > 1)
            {
                UpdateItems();
                time = GameMain.gameTime;
            }
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.ExpandWidth(false));
            search = GUILayout.TextField(search);
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (Cell item in cells)
            {
                item.Draw(search);
            }
            GUILayout.EndScrollView();
            Input.ResetInputAxes();
        }
    }
}