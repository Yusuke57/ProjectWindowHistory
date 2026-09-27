using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_6000_3_OR_NEWER
using FolderId = UnityEngine.EntityId;
#else
// EntityId は 6000.3 で追加された型なので、それ以前は従来どおり int を使う
using FolderId = System.Int32;
#endif
using SearchViewState = ProjectWindowHistory.ProjectWindowReflectionUtility.SearchViewState;

namespace ProjectWindowHistory
{
    /// <summary>
    /// ProjectWindowの履歴レコード
    /// </summary>
    [Serializable]
    public class ProjectWindowHistoryRecord
    {
        [SerializeField] private FolderId[] _selectedFolderInstanceIds;
        [SerializeField] private string _searchedText;
        [SerializeField] private SearchViewState _searchViewState;

        public FolderId[] SelectedFolderInstanceIDs => _selectedFolderInstanceIds;
        public string SearchedText => _searchedText;
        public SearchViewState SearchViewState => _searchViewState;

        public ProjectWindowHistoryRecord(FolderId[] selectedFolderInstanceIds, string searchedText, SearchViewState searchViewState)
        {
            _selectedFolderInstanceIds = selectedFolderInstanceIds;
            _searchedText = searchedText;
            _searchViewState = searchViewState;
        }

        public void ChangeSearchViewState(SearchViewState searchViewState)
        {
            _searchViewState = searchViewState;
        }

        public bool IsValid()
        {
            // フォルダが何かしら削除されていた場合は無効にしておく
            return (_selectedFolderInstanceIds?.Any() ?? false)
                   && _selectedFolderInstanceIds.All(instanceId => AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(instanceId)));
        }

        /// <summary>
        /// 表示用テキストを生成して返す
        /// </summary>
        /// <returns></returns>
        public string ToLabelText()
        {
            // 検索文字列がない場合は選択フォルダ名
            if (string.IsNullOrEmpty(_searchedText))
            {
                return SelectedFolderToLabelText();
            }

            // 検索文字列がある場合は検索文字列と検索範囲
            var labelText = $"\"{_searchedText}\" [{_searchViewState}]";

            // 検索範囲が選択フォルダ内の場合は選択フォルダ名も追記
            if (_searchViewState == SearchViewState.SubFolders)
            {
                labelText = $"{SelectedFolderToLabelText()} : {labelText}";
            }

            return labelText;

            string SelectedFolderToLabelText()
            {
                const int displayFolderCountMax = 3; // 表示は最大3件
                var targetFolderNames = _selectedFolderInstanceIds
                    .Take(displayFolderCountMax)
                    .Select(id =>
                    {
                        var path = AssetDatabase.GetAssetPath(id);
                        return Path.GetFileName(path);
                    });

                var suffix = _selectedFolderInstanceIds.Length > displayFolderCountMax ? "+" : string.Empty;
                return string.Join(",", targetFolderNames) + suffix;
            }
        }
    }
}