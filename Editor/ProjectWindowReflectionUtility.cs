using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
#if UNITY_6000_3_OR_NEWER
using FolderId = UnityEngine.EntityId;
#else
// EntityId は 6000.3 で追加された型なので、それ以前は従来どおり int を使う
using FolderId = System.Int32;
#endif

namespace ProjectWindowHistory
{
    /// <summary>
    /// ProjectWindowのリフレクションUtilityクラス
    /// </summary>
    public static class ProjectWindowReflectionUtility
    {
        // ====== ProjectBrowserの型情報 ======
        private static Type _projectBrowserType;
        private static Type ProjectBrowserType => _projectBrowserType ??= Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");

        private static Type _projectBrowserListType;
        private static Type ProjectBrowserListType => _projectBrowserListType ??= typeof(List<>).MakeGenericType(ProjectBrowserType);

        // ====== ProjectBrowserのフィールド ======
        private static FieldInfo _lastInteractedProjectBrowserField;
        private static FieldInfo LastInteractedProjectBrowserField => _lastInteractedProjectBrowserField
            ??= ProjectBrowserType.GetField("s_LastInteractedProjectBrowser", BindingFlags.Public | BindingFlags.Static);

        private static FieldInfo _viewModeField;
        private static FieldInfo ViewModeField => _viewModeField
            ??= ProjectBrowserType.GetField("m_ViewMode", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo _lastFoldersField;
        private static FieldInfo LastFoldersField => _lastFoldersField
            ??= ProjectBrowserType.GetField("m_LastFolders", BindingFlags.NonPublic | BindingFlags.Instance);

        private static FieldInfo _searchFieldTextField;
        private static FieldInfo SearchFieldTextField => _searchFieldTextField
            ??= ProjectBrowserType.GetField("m_SearchFieldText", BindingFlags.NonPublic | BindingFlags.Instance);

        // ====== ProjectBrowserのメソッド ======
        private static MethodInfo _getAllProjectBrowsersMethod;
        private static MethodInfo GetAllProjectBrowsersMethod => _getAllProjectBrowsersMethod
            ??= ProjectBrowserType.GetMethod("GetAllProjectBrowsers", BindingFlags.Public | BindingFlags.Static);

        private static MethodInfo _getFolderInstanceIDsMethod;
        private static MethodInfo GetFolderInstanceIDsMethod => _getFolderInstanceIDsMethod
            ??= ProjectBrowserType.GetMethod("GetFolderInstanceIDs", BindingFlags.NonPublic | BindingFlags.Static);

        private static MethodInfo _setFolderSelectionMethod;
        // オーバーロードがあるので引数が最も少ないメソッドを探して呼ぶ
        // (6000.3 で引数2つのオーバーロードが無くなり、省略可能引数が付いた3つ以上のものだけになった)
        private static MethodInfo SetFolderSelectionMethod => _setFolderSelectionMethod
            ??= ProjectBrowserType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(method => method.Name == "SetFolderSelection")
                .OrderBy(method => method.GetParameters().Length)
                .FirstOrDefault();

        private static MethodInfo _setSearchMethod;
        // オーバーロードがあるのでSearchFilter型引数のメソッドの方を探して呼ぶ
        private static MethodInfo SetSearchMethod => _setSearchMethod
            ??= ProjectBrowserType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(method => method.Name == "SetSearch" && method.GetParameters().First().ParameterType == SearchFilterType);

        private static MethodInfo _getSearchViewStateMethod;
        private static MethodInfo GetSearchViewStateMethod => _getSearchViewStateMethod
            ??= ProjectBrowserType.GetMethod("GetSearchViewState", BindingFlags.NonPublic | BindingFlags.Instance);

        private static MethodInfo _setSearchViewStateMethod;
        private static MethodInfo SetSearchViewStateMethod => _setSearchViewStateMethod
            ??= ProjectBrowserType.GetMethod("SetSearchViewState", BindingFlags.NonPublic | BindingFlags.Instance);

        // ====== SearchFilterの型情報 ======
        private const string SearchFilterTypeName = "UnityEditor.SearchFilter,UnityEditor";
        private static Type _searchFilterType;
        private static Type SearchFilterType => _searchFilterType ??= Type.GetType(SearchFilterTypeName);

        // ====== SearchFilterのフィールド ======
        private static FieldInfo _searchFilterFoldersField;
        private static FieldInfo SearchFilterFoldersField => _searchFilterFoldersField
            ??= SearchFilterType.GetField("m_Folders", BindingFlags.NonPublic | BindingFlags.Instance);

        // ====== SearchFilterのメソッド ======
        private static MethodInfo _createSearchFilterFromStringMethod;
        private static MethodInfo CreateSearchFilterFromStringMethod => _createSearchFilterFromStringMethod
            ??= SearchFilterType.GetMethod("CreateSearchFilterFromString", BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// 現在エディタ上に存在する全てのProjectビューを取得する
        /// </summary>
        /// <returns></returns>
        public static List<EditorWindow> GetAllProjectWindows()
        {
            var projectWindowsObject = GetAllProjectBrowsersMethod.Invoke(null, null);

            // 以下object型をList<EditorWindow>型に変換する処理
            var countProperty = ProjectBrowserListType.GetProperty("Count");
            var indexer = ProjectBrowserListType.GetProperty("Item");

            if (countProperty == null || indexer == null)
            {
                return new List<EditorWindow>();
            }

            var projectWindowCount = (int) countProperty.GetValue(projectWindowsObject, null);
            var projectWindows = new List<EditorWindow>();
            for (var i = 0; i < projectWindowCount; i++)
            {
                var projectWindow = (EditorWindow) indexer.GetValue(projectWindowsObject, new object[] { i });
                projectWindows.Add(projectWindow);
            }

            return projectWindows;
        }

        /// <summary>
        /// 開いているProjectビューを取得する
        /// </summary>
        /// <returns></returns>
        public static EditorWindow GetLastProjectWindow()
        {
            return (EditorWindow) LastInteractedProjectBrowserField.GetValue(null);
        }

        /// <summary>
        /// ProjectWindowが1カラムビューかどうか
        /// </summary>
        /// <returns></returns>
        public static bool IsOneColumnViewMode(EditorWindow targetProjectWindow)
        {
            // OneColumn=0, TwoColumns=1
            return ((int) ViewModeField.GetValue(targetProjectWindow)) == 0;
        }

        /// <summary>
        /// 左側のツリーで選択したフォルダのインスタンスIDを取得する
        /// </summary>
        /// <returns></returns>
        public static FolderId[] GetLastFolderInstanceIds(EditorWindow targetProjectWindow)
        {
            // 選択中のフォルダのパス配列を取得し、GetFolderInstanceIDs でインスタンスIDに変換する
            // (戻り値の要素型は 6000.3 以降 EntityId、それ以前は int で、FolderId と一致する)
            var lastFolderPaths = LastFoldersField.GetValue(targetProjectWindow) as string[] ?? Array.Empty<string>();
            return (FolderId[]) GetFolderInstanceIDsMethod.Invoke(null, new object[] { lastFolderPaths });
        }

        /// <summary>
        /// 指定したインスタンスIDのフォルダを選択状態にする
        /// </summary>
        /// <param name="targetProjectWindow"></param>
        /// <param name="selectedFolderInstanceIds"></param>
        public static void SetFolderSelection(EditorWindow targetProjectWindow, FolderId[] selectedFolderInstanceIds)
        {
            // 第2引数以降 (revealSelectionAndFrameLastSelected など) は全て false
            var args = new object[SetFolderSelectionMethod.GetParameters().Length];
            args[0] = selectedFolderInstanceIds;
            for (var i = 1; i < args.Length; i++)
            {
                args[i] = false;
            }

            SetFolderSelectionMethod.Invoke(targetProjectWindow, args);
        }

        /// <summary>
        /// 検索フィールドの文字列を取得する
        /// </summary>
        /// <param name="targetProjectWindow"></param>
        /// <returns></returns>
        public static string GetSearchedText(EditorWindow targetProjectWindow)
        {
            return (string) SearchFieldTextField.GetValue(targetProjectWindow);
        }

        /// <summary>
        /// 検索を設定する
        /// </summary>
        /// <param name="targetProjectWindow"></param>
        /// <param name="searchedText"></param>
        /// <param name="selectedFolderInstanceIds"></param>
        public static void SetSearch(EditorWindow targetProjectWindow, string searchedText, FolderId[] selectedFolderInstanceIds)
        {
            // 検索文字列からsearchFilterを生成する
            var searchFilter = CreateSearchFilterFromStringMethod.Invoke(null, new object[] { searchedText });

            // searchFilterに選択中のフォルダを設定する
            var selectedFolderPathList = selectedFolderInstanceIds
                .Select(id => AssetDatabase.GetAssetPath(id))
                .ToArray();
            SearchFilterFoldersField.SetValue(searchFilter, selectedFolderPathList);

            // targetProjectWindowにsearchFilterを設定する
            SetSearchMethod.Invoke(targetProjectWindow, new[] { searchFilter });
        }

        /// <summary>
        /// 検索範囲(SearchViewState)を取得する
        /// </summary>
        /// <param name="targetProjectWindow"></param>
        /// <returns></returns>
        public static SearchViewState GetSearchViewState(EditorWindow targetProjectWindow)
        {
            return (SearchViewState) GetSearchViewStateMethod.Invoke(targetProjectWindow, null);
        }

        /// <summary>
        /// 検索範囲(SearchViewState)を設定する
        /// </summary>
        /// <param name="targetProjectWindow"></param>
        /// <param name="searchViewState"></param>
        public static void SetSearchViewState(EditorWindow targetProjectWindow, SearchViewState searchViewState)
        {
            SetSearchViewStateMethod.Invoke(targetProjectWindow, new object[] { (int) searchViewState });
        }

        /// <summary>
        /// internalクラスのProjectBrowser内で定義されたSearchViewStateを複製したもの
        /// </summary>
        public enum SearchViewState
        {
            NotSearching,
            AllAssets,
            InAssetsOnly,
            InPackagesOnly,
            SubFolders,
        }
    }
}