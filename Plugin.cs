using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace MyFirstPlugin
{
    [BepInPlugin(PluginName, "dsp", "1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginName = "com.small.dsp.transferInfo";

        private XYModLib.UIWindow Window;

        private List<TransportInfo> arrayList = new List<TransportInfo>();
        internal static string search = "";
        internal static Dictionary<int,string> itemCache = new Dictionary<int, string>();
        internal static List<Cell> cells = new List<Cell>();
        internal static Vector2 scroll = new Vector2(0,0);

        private void Awake()
        {
            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(DSPPatch));
            Logger.LogInfo($"Plugin {PluginName} is loaded!");
            Window = new XYModLib.UIWindow("Transfers Info");
            Window.Show = false;
            Window.OnWinodwGUI = guiInfo;
            Window.OnWindowOpen = () =>
            {
                getInfo();
            };
            Window.OnWindowClose = () =>
            {
                arrayList.Clear();
            };
        }

        protected void OnDestroy()
        {
            Harmony.UnpatchAll();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (!GameMain.isRunning)
                {
                    Window.Show=false;
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
                if (station==null || station.isCollector || station.isVeinCollector)
                {
                    continue;
                }
                TransportInfo info=new TransportInfo();
                info.id = station.id;
                info.gid = station.gid;
                info.name = string.IsNullOrEmpty(station.name)?"transfer-"+ info.id : station.name;
                info.entityId=station.entityId;
                info.planetId=station.planetId;
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
                string name=GameMain.galaxy.StarById(kv.Key).displayName;
                kv.Value.Sort();
                cells.Add(new Cell(name,kv.Value, OpenTransfer));
            }
        }

        void UpdateItems()
        {
            StationComponent[] stations = GameMain.data.galacticTransport.stationPool;
            foreach (var info in arrayList)
            {
                var item=stations[info.gid];
                int i = -1;
                foreach(var storage in item.storage)
                {
                    i++;
                    if (storage.itemId <= 0) continue;
                    if (!itemCache.ContainsKey(storage.itemId))
                    {
                        itemCache.Add(storage.itemId, LDB.ItemName(storage.itemId));
                    }
                    info.items[i] = $"{itemCache[storage.itemId]}\n{storage.count}";
                }
            }
        }

        void OpenTransfer(TransportInfo item)
        {
            var uigame=UIRoot.instance.uiGame;
            uigame.OpenPlayerInventory();
            var win = uigame.stationWindow;
            win.player= GameMain.mainPlayer;
            win.factory = GameMain.data.GetOrCreateFactory(GameMain.data.galaxy.PlanetById(item.planetId));
            win.transport = win.factory.transport;
            win.powerSystem = win.factory.powerSystem;
            win.factorySystem = win.factory.factorySystem;
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
            UpdateItems();
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