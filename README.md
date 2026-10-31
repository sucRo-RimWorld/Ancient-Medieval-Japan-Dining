# Ancient & Medieval Japan - Dining

主食・汁・副菜を備蓄から取り分け、一つの膳で食べる独立Modです。Culture系カテゴリですがCulture本体は不要です。

RimWorld 1.6とHarmonyが必要です。**Daily Menuは不要で、同時使用は非対応です。** Agriculture・MOもDining自体の必須依存ではありません。料理と役割指定はAgricultureなどの食料Modが提供します。

## 使い方

1. 配膳台を建て、「料理を選ぶ」から供する料理を指定する。
2. 通常の作業指示で料理を作り置きする。運搬担当者が配膳台へ補充する。
3. 空腹時に主食と、用意できる汁・副菜を一膳にまとめて食べる。病人等への給餌にも接続する。

食べ残し、中断、材料・腐敗の引継ぎに対応します。朝昼晩の献立や自動調理計画は不要です。対応する主食がない場合は通常の食事経路を維持します。

膳に料理画像を置く編集UI、主菜・添え物の独立枠、盛り付けと品数の新しい評価は後続作業です。既存の暫定数値は最終バランスではありません。

## 更新と開発

Harmony → Dining → Agricultureの順で有効化してください。従来のDaily Menu併用版から更新する際はDaily Menuを外します。元のMod・プレイヤーの設定・セーブは自動変更しません。既存の膳識別子とAMJで使った餐台の読み取り口を残していますが、Daily Menuの全機能・実ユーザー旧セーブ全体の移行は未保証です。

Dining implements its own storage-backed serving, portions, meal records and vanilla assisted-feeding integration. Only Harmony is required. No Daily Menu code, DLL or artwork is distributed. Food mods own recipes and role assignments; Culture is categorization only.

See [design/build/validation](Docs/Design.md). Historical evidence in [ExtractionValidation](Docs/ExtractionValidation.md) predates dependency removal. No Steam publication is implied.

Current dependency-removal runtime evidence: [2026-10-31 acceptance](Docs/IndependentValidation.md).
