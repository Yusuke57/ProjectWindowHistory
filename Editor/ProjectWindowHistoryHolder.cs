using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_6000_3_OR_NEWER
using WindowId = UnityEngine.EntityId;
#else
// EntityId は 6000.3 で追加された型なので、それ以前は従来どおり int を使う
using WindowId = System.Int32;
#endif

namespace ProjectWindowHistory
{
    /// <summary>
    /// ProjectWindowとHistoryのペアを保持しておくScriptableSingleton
    /// アセットとして保存はしてないので、Unityエディタ再起動時には履歴情報は消える
    /// </summary>
    public class ProjectWindowHistoryHolder : ScriptableSingleton<ProjectWindowHistoryHolder>
    {
        [SerializeField] private List<ProjectWindowHistorySaveData> _saveDataList = new();

        public ProjectWindowHistory GetHistory(EditorWindow targetWindow)
        {
            return _saveDataList.FirstOrDefault(data => data.WindowInstanceId == GetWindowId(targetWindow))?.History;
        }

        /// <summary>
        /// EditorWindow の識別子を取得する
        /// </summary>
        internal static WindowId GetWindowId(EditorWindow window)
        {
#if UNITY_6000_3_OR_NEWER
            return window.GetEntityId();
#else
            return window.GetInstanceID();
#endif
        }

        public void Add(EditorWindow targetWindow, ProjectWindowHistory history)
        {
            var saveData = new ProjectWindowHistorySaveData(targetWindow, history);
            _saveDataList.Add(saveData);
        }
    }

    [Serializable]
    public class ProjectWindowHistorySaveData
    {
        [SerializeField] private WindowId _windowInstanceId;
        [SerializeField] private ProjectWindowHistory _history;

        public WindowId WindowInstanceId => _windowInstanceId;
        public ProjectWindowHistory History => _history;

        public ProjectWindowHistorySaveData(EditorWindow window, ProjectWindowHistory history)
        {
            _windowInstanceId = ProjectWindowHistoryHolder.GetWindowId(window);
            _history = history;
        }
    }
}