using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFirstPlugin
{
    class TransportInfo : IComparable<TransportInfo>
    {
        public int id;
        public int gid;
        public string name;
        public string[] items;
        public int entityId;
        public int planetId;
        public TransportInfo()
        {
            items = new string[] { "", "", "", "", "" };
        }

        public int CompareTo(TransportInfo other)
        {
            return other.planetId.CompareTo(planetId);
        }
    }

    internal class Cell
    {
        private string name;
        private List<TransportInfo> data;
        private bool isCollapse = true;
        private bool isInSpace = false;
        private Action<TransportInfo> callback;

        private static GUIStyle _pluginHeaderSkin = null;

        public Cell(string name, List<TransportInfo> data, Action<TransportInfo> callback, bool isInSpace)
        {
            this.name = name;
            this.data = data;
            this.callback = callback;
            this.isInSpace = isInSpace;
        }

        public void Draw(string search)
        {
            if (_pluginHeaderSkin == null)
            {
                _pluginHeaderSkin = new(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperCenter,
                    wordWrap = true,
                    stretchWidth = true
                };
            }
            GUILayout.BeginVertical(GUI.skin.box);
            if (string.IsNullOrEmpty(search))
            {
                if (isCollapse)
                {
                    //no search, isCollapse, clickable
                    if (GUILayout.Button(name + "\n...", _pluginHeaderSkin, GUILayout.ExpandWidth(true)))
                    {
                        isCollapse = !isCollapse;
                    }
                }
                else
                {
                    if (GUILayout.Button(name, _pluginHeaderSkin))
                    {
                        isCollapse = !isCollapse;
                    }
                    data.ForEach(item => DrawItem(item));
                }
            }
            else
            {
                //serching, ignore
                GUILayout.Label(name, _pluginHeaderSkin);
                data.ForEach((item) =>
                {
                    if (searched(item, search))
                    {
                        DrawItem(item);
                    }
                });
            }
            GUILayout.EndVertical();
        }

        private bool searched(TransportInfo item, string search)
        {
            if (item.name.IndexOf(search) >= 0) return true;
            foreach (var good in item.items)
            {
                if (good.IndexOf(search) >= 0) return true;
            }
            return false;
        }

        private void DrawItem(TransportInfo item)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(item.id.ToString(), GUILayout.Width(25));
            GUILayout.Label(item.name);
            foreach (var good in item.items)
            {
                GUILayout.Label(good, GUILayout.Width(100));
            }
            if (!isInSpace)
            {
                if (GUILayout.Button("info", GUILayout.ExpandWidth(false)))
                {
                    callback(item);
                }
            }
            GUILayout.EndHorizontal();
        }
    }
}