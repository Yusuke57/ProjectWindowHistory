# ChangeLog

## [1.1.0] - 2026-03-28
### Changed
- Unity 6000.3 互換性対応
  - `InstanceIDToObject(int)`、`GetAssetPath(int)` の CS0618 deprecation 警告を `#pragma warning disable` で抑制
  - `GetFolderInstanceIDs` を `m_LastFolders` パスベースの実装に書き換え（6000.3 で `EntityId[]` を返すため）
  - `SetSearch` 内の `GetAssetPath` を `InstanceIDToObject` 経由に変更
  - `SetFolderSelection` に `UNITY_6000_3_OR_NEWER` 条件分岐を追加（`EntityId` 経由での選択）

## [1.0.0] - 2023-12-02
### first release
