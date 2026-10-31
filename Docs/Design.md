# AMJ Dining — 献立と配膳

## 所有と依存（2026-10-31）

Culture系カテゴリの独立Mod。Culture本体・Agriculture・MOには依存しない。必須ModはHarmonyのみ。AgricultureはDiningを必須とし、料理・調理・料理の役割指定を所有する。

作者の「まずDaily Menu依存をなくす」に従い、餐台・小分け・介助配膳接続・品数記録をDining自身の実装へ置き換える。Daily Menuのソース・DLL・画像は再配布しない。以前の比較・採用経緯は[過去調査](Research/DailyMenuNativeAssessment-20261028.md)。既存Modを全置換する一般献立管理は目的とせず、AMJで採用した一膳化の必要部分だけを所有する。朝昼晩の献立計画、飲料管理、自動調理計画は導入しない。

## 現行実装

- `AMJD_ServingTable`（配膳台）で料理を1種類指定する。見た目はバニラの小さな棚を借用。バニラの運搬で通常ストレージへ補充し、取り分けの残りも通常の食料探索から見える場所へ戻す。
- 主食・汁・副菜から各1種類を選び、空腹時に一膳へ取り分ける。主食があれば汁・副菜の欠品を許容。役割指定は食料Modが提供する。Diningだけでは料理や役割指定を追加せず、対応する主食がなければバニラの食事経路を維持する。
- 複数の小分け食品を順に取り、同じ料理の古い端数が小さいだけで取餐を打ち切らない。材料・品質・腐敗・栄養を引き継ぎ、料理固有の喫食効果を実際の食品から適用する。
- 食事制限・食用可否・禁止・予約を確認。医者・看守が通常の給餌Jobを選んだ場合に膳へ置き換え、対象の適格性をバニラのWorkGiverに委ねる。
- 取餐中断時の膳、食べ残し、膳・内包食品のScribeを維持。品数は内包食品だけから記録し、膳自体を一品として追加しない。
- 食前調理待ちや全員の食事時刻同期は不要。

## 数値とUIの境界

今回は依存除去であり、既存の暫定栄養配分3:1:1を維持する。品数判定の栄養閾値 `max(0.02, 食事目標×0.15)` と既存の2品/+2・3品以上/+4、0.5日の効果も引継ぎ、最終数値の承認とは扱わない。

**後続作業:** 主食・主菜・副菜・汁物・添え物へ役割を拡張。膳画像の左に主食、右に汁、中央に主菜を配置するグラフィカルな献立UI。盛る量と栄養密度を分け、不足を主食で補う方式。漬菜・梅干し等は少量で、複数でも品数加算最大1品。調味料は料理への心情有利で独立の品数に数えない。これらは今回実装済みとはしない。

## 導入・既存データ

Harmony → Dining → Agriculture（利用する場合）の順。Daily Menuは外して使用する。同時使用は非対応としてAboutに宣言する。通常プレイのMod設定・セーブは自動変更しない。

packageId `sucro.ancientmedievaljapan.dining`、旧来の `AMJAgriculture.Dining.dll` と型名、`AMJA_MealPlate` / `AMJA_TakeMeal`、膳の保存キーは維持。料理の小分けにはDining自身のCompを使用し、以前の小分け保存フィールドも読める形式とする。

既存のAMJ利用餐台向けに `DailyMenu_Buffet` Defと `DailyMenu.Buffet` の小さな保存名アダプターを残す。これは独自実装を継承するだけでDaily Menuへの参照ではない。旧 `buffetContents` を読み込んだ場合は通常の地上ストレージへ中身を戻す。新規建築メニューには旧餐台を表示しない。旧Daily Menu全体の飲料台・計画・進行中Job・MapComponentの移行は対象外であり、実ユーザー旧セーブ全体の互換を保証しない。

## ビルドと確認

`Scripts/Build.ps1 -RimWorldManagedDir <Managed> -HarmonyAssembly <0Harmony.dll> -OutputDirectory <Assemblies>`。必要な参照はゲームとHarmonyのみ。`Source/AMJAgriculture.Dining/*.cs`を一つのDLLへビルドする。

`python Tests/validate_migration.py --agriculture ../AncientMedievalJapanGrains` はXML・単一所有・依存方向・画像の保存を検証する。`Docs/Migration.json` のソース/DLLハッシュは**最初の移転時の履歴**であり、今回の独立実装に同一性を要求しない。

現在の試験入口は `Tools/Acceptance/RunIndependent.py`。入力にはAgricultureとDiningだけを置く。配布DLLをそのままロードし、Daily MenuのMod/DLLがないこと、バニラ運搬、材料・栄養保存、通常喫食、中断・Scribe、介助給餌を確認する。旧Tools/Research以下と[移転時の検証](ExtractionValidation.md)は過去のソースに対する証拠であり今回の合格とは別。

Current dependency-removal runtime evidence: [2026-10-31 acceptance](IndependentValidation.md).
