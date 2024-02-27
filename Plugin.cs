using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MyFirstPlugin
{
    [BepInPlugin(PluginName, "dsp", "1.1")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginName = "com.small.dsp.transferInfo";

        private XYModLib.UIWindow Window;
        private PropertyInfo planetSetter;

        private List<TransportInfo> arrayList = new List<TransportInfo>();
        internal static string search = "";
        internal static Dictionary<int, string> itemCache = new Dictionary<int, string>();
        internal static List<Cell> cells = new List<Cell>();
        internal static Vector2 scroll = new Vector2(0, 0);
        internal static double time = 0;
        internal static bool showCollector = false;

        public ConfigEntry<KeyCode> GUIHotkey;

        private void Awake()
        {
            // Plugin startup logic
            //Harmony.CreateAndPatchAll(typeof(DSPPatch));
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
            planetSetter = typeof(GameData).GetProperty("localPlanet");
        }

        protected void OnDestroy()
        {
            Harmony.UnpatchAll();
        }

        void Update()
        {
            if (Input.GetKeyDown(GUIHotkey.Value))
            {
                if (!GameMain.isRunning || GameMain.isPaused)
                {
                    Window.Show = false;
                }
                else
                {
                    bool ori = UICursor.locked;
                    //cannot set cursor status, just ignore it.
                    if (ori) return;
                    Window.Show = !Window.Show;
                }
            }
        }

        void getInfo()
        {
            StationComponent[] stations = GameMain.data.galacticTransport.stationPool;
            Dictionary<int, Dictionary<string, List<TransportInfo>>> map = new Dictionary<int, Dictionary<string, List<TransportInfo>>>();
            Dictionary<int, PlanetFactory> facCache = new Dictionary<int, PlanetFactory>();
            foreach (StationComponent station in stations)
            {
                if (station == null || station.isVeinCollector)
                {
                    continue;
                }
                TransportInfo info = new TransportInfo();
                info.id = station.id;
                info.gid = station.gid;
                info.entityId = station.entityId;
                info.planetId = station.planetId;
                info.isCollector = station.isCollector;

                //查物流站名称
                int galaxy = station.planetId / 100;
                if (!map.ContainsKey(galaxy))
                {
                    map.Add(galaxy, new Dictionary<string, List<TransportInfo>>());
                }
                var plantName = "";
                if (!facCache.ContainsKey(info.planetId))
                {
                    facCache[info.planetId] = GameMain.data.GetOrCreateFactory(GameMain.data.galaxy.PlanetById(info.planetId));
                    plantName = facCache[info.planetId].planet.name;
                    map[galaxy].Add(plantName, new List<TransportInfo>());
                }
                else
                {
                    plantName = facCache[info.planetId].planet.name;
                }
                info.name = facCache[info.planetId].ReadExtraInfoOnEntity(info.entityId);
                info.name = info.name == "" ? (station.isCollector ? "collector-" : "transfer-") + info.id : info.name;

                arrayList.Add(info);
                map[galaxy][plantName].Add(info);
            }
            var inSpace = GameMain.data.localPlanet == null;
            foreach (var kv in map)
            {
                string name = GameMain.galaxy.StarById(kv.Key).displayName;
                cells.Add(new Cell(name, kv.Value, OpenTransfer, inSpace));
            }
        }

        void UpdateItems()
        {
            StationComponent[] stations = GameMain.data.galacticTransport.stationPool;
            foreach (var info in arrayList)
            {
                var item = stations[info.gid];
                int i = 0;
                foreach (var storage in item.storage)
                {
                    if (storage.itemId <= 0) continue;
                    if (!itemCache.ContainsKey(storage.itemId))
                    {
                        itemCache.Add(storage.itemId, LDB.ItemName(storage.itemId));
                    }
                    info.items[i] = $"{itemCache[storage.itemId]}\n{storage.count}";
                    if (i >= 4) break;
                    i++;
                }
            }
        }

        void OpenTransfer(TransportInfo item)
        {
            var uiGame = UIRoot.instance.uiGame;
            uiGame.OpenPlayerInventory();
            var win = uiGame.stationWindow;
            var oriPlanet = GameMain.localPlanet;
            if (oriPlanet == null)
            {
                Window.Show = false;
                return;
            }
            else if (oriPlanet.id != item.planetId)
            {//需要处理星球信息
                var planet = GameMain.data.galaxy.PlanetById(item.planetId);
                planet.factory = GameMain.data.GetOrCreateFactory(planet);
                planetSetter.SetValue(GameMain.data, planet);
            }
            win.stationId = item.id;
            win._Open();
            planetSetter.SetValue(GameMain.data, oriPlanet);
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
            showCollector = GUILayout.Toggle(showCollector, "显示采集器/show collector");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.ExpandWidth(false));
            search = GUILayout.TextField(search);
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (Cell item in cells)
            {
                item.Draw(search, showCollector);
            }
            GUILayout.EndScrollView();
        }
    }
}