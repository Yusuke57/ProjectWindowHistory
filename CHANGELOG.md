# ChangeLog

## [1.2.0] - 2026-09-28
### Changed
- Unity 6000.6 対応
  - インスタンスIDを保持する型を `UNITY_6000_3_OR_NEWER` で `EntityId` / `int` に切り替え、`GetInstanceID()`、`InstanceIDToObject(int)`、`GetAssetPath(int)` の呼び出しを撤去（6000.6 でこれらが CS0618 から CS0619 に格上げされ、コンパイルが通らなくなっていた）
  - 1.1.0 で追加した `#pragma warning disable CS0618` とバージョン条件分岐を削除
  - `GetLastFolderInstanceIds` を `GetFolderInstanceIDs` 経由の実装に戻した
  - `ProjectWindowHistoryRecord.IsValid()` のフォルダ判定を `AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(id))` に変更

### Fixed
- Unity 6000.6 で `SetFolderSelection` のオーバーロードを見つけられず、Undo/Redo 時に `NullReferenceException` になる問題を修正（引数が最も少ないオーバーロードを選ぶようにした）

### Breaking changes
- 以下の公開メンバーの型が Unity 6000.3 以降で `EntityId` ベースになります（メンバー名は変更なし）
  - `ProjectWindowHistoryRecord.SelectedFolderInstanceIDs`: `int[]` → `EntityId[]`
  - `ProjectWindowHistorySaveData.WindowInstanceId`: `int` → `EntityId`
- 型変更にともない、パッケージ更新後は更新前の履歴が復元されません

## [1.1.0] - 2026-03-28
### Changed
- Unity 6000.3 互換性対応
  - `InstanceIDToObject(int)`、`GetAssetPath(int)` の CS0618 deprecation 警告を `#pragma warning disable` で抑制
  - `GetFolderInstanceIDs` を `m_LastFolders` パスベースの実装に書き換え（6000.3 で `EntityId[]` を返すため）
  - `SetSearch` 内の `GetAssetPath` を `InstanceIDToObject` 経由に変更
  - `SetFolderSelection` に `UNITY_6000_3_OR_NEWER` 条件分岐を追加（`EntityId` 経由での選択）

## [1.0.0] - 2023-12-02
### first release
