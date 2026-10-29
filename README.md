# Ancient & Medieval Japan - Dining

主食・主菜・副菜・汁物・漬物系の料理ロールを組み合わせ、一つの膳で食べる仕組みを目指す独立Modです。Culture系カテゴリですがCulture本体は不要です。

## 現在使える機能

Agricultureから移した既存機能として、主食・汁・副菜の取り分け、一膳での喫食、残り物、中断、保存、介助給餌を提供します。料理と役割指定はAgricultureなどの食料Modが提供します。Dining単独では料理は追加しません。

RimWorld 1.6 / Harmony / Daily Menuが必要です。Agriculture・MO・CultureはDiningの必須依存ではありません。Agricultureと併用するときは両方を対応版へ更新し、Daily Menu → Dining → Agricultureの順で有効にしてください。旧Agricultureの配膳DLLを重複させないでください。

膳に料理画像を配置するUI、主菜と副菜・添え物の独立枠、新しい品数・盛り付け判定は未実装です。Daily Menuからの独立もまだ行っていません。

## Development

Dining owns meal assembly/serving/ingestion; food mods own recipes and role mappings. Culture is a category, not a dependency. The legacy assembly/type/Def identities are intentionally retained for references. No Daily Menu code or DLL is redistributed.

See [design and build/validation](Docs/Design.md) and [source-bounded historical evidence](Docs/Dining.md). This extraction is not a claim of new in-game or old-save acceptance. No Steam publication has occurred.

移管後のMO併用実ゲーム確認：[結果と前提](Docs/ExtractionValidation.md)。単独構成・実ユーザー旧セーブ・1.7は未検証。
