# AMJ Dining — 献立と配膳

## 所有と依存（2026-10-30作者決定）

Culture系カテゴリの独立Modであり、Culture本体を必須にしない。AgricultureはDiningを必須とする。DiningはAgriculture・MOに依存しない。食料Modが料理と役割を定義し、Diningが膳の構成・取り分け・配膳・喫食を担当する。

今回の移転は既存機能の所有変更。Daily MenuとHarmonyを引き続き必須とし、第三者コードは取り込まない。依存削減や新しい献立UIは未実装。Agriculture PR #281由来の承認済み実装を移すもので、新たな配膳システムを並行開発するものではない。

## 現行実装

- 主食がある場合、主食・汁・副菜を役割ごとに最大1種選ぶ。欠けた役割を必須としない。
- 空腹時に備蓄した完成料理を取り分け、1つの膳として食べる。食前の調理待ちや時刻同期は不要。
- 栄養配分3:1:1は移転前の暫定値を維持。最終バランスの承認ではない。
- 材料・栄養・腐敗・食品効果、取餐中断・残り物・Scribe・代理給餌を保持する。
- Daily Menu Supplyの起動互換とCheat Menuの受け入れ判定互換、餐台のMass補完を所有する。
- 料理・MO料理の役割指定、汎用簡単な食事の新規Bill停止はAgricultureに残す。Dining単独では役割付き料理を追加せず、適合料理がなければ通常の食事経路を維持する。

## 次段階の設計（今回未実装）

料理の主食・主菜・副菜・汁物・添え物の役割と組合せを再現する。主菜と副菜の分離、添え物の独立枠は現行3役からの拡張になる。全枠を埋めることを食事の条件にはしない。

膳をグラフィカルに表示し、主食は左、汁物は右、主菜は中央へ料理画像を配置する。朝昼晩の3献立を必須にしない。画像の選択方法・副菜と添え物の具体配置は後続設計。

盛る量と栄養密度を分け、低栄養の野菜を栄養ノルマのため大量に盛らない。不足分を主食で補う。漬菜・梅干し等は少量で複数でも品数加算は最大1品。調味料は料理への心情上の有利で、独立した品数ではない。Daily Menuの栄養閾値式の品数判定を要改修。量・効果・重複値は未決定。

## 識別子と導入

packageIdは `sucro.ancientmedievaljapan.dining`。Agricultureと同時更新し、Harmony → Daily Menu → Dining → Agricultureの順で有効にする。Cultureを追加する必要はない。有効Mod設定は本移転で自動変更しない。

既存参照のため `AMJAgriculture.Dining.dll`、`AMJAgriculture.Dining`型名、Harmony ID、`AMJA_MealPlate` / `AMJA_TakeMeal`、保存キーをそのまま保持。名前にAgricultureが残ることは依存を意味しない。旧DLLをAgriculture側に残すと二重ロードになるため、対応版ペアを使用する。識別子維持は実際の既存セーブ互換の合格証拠ではない。

膳画像はAgricultureの既存承認済み穀飯3状態をバイト同一で独自パス `Things/Item/AMJD/MealPlate` に複製。元料理の画像はAgricultureに維持する。最終的な膳UI・画像受入とは区別する。

## 検証と保守

`Scripts/Build.ps1` に実ゲームManaged・Harmony・DailyMenu DLLを渡すと、移転後のソースを独立ビルドできる。バイナリ参照は同梱しない。今回の配布DLLは移転元とバイト同一。ソース比較はGitのLF改行へ正規化する。

`python Tests/validate_migration.py --agriculture ../AncientMedievalJapanGrains` は二重所有・依存方向・XML・移転元ハッシュと画像を検証する。ゲームは起動しない。[過去の実行証拠](Dining.md)は移転前の特定DLLに限定。分離後のMO併用一膳化は[限定実ゲーム確認](ExtractionValidation.md)で合格。実際の旧セーブ・単独構成・1.7・新UIは未検証。

移管後のMO併用実ゲーム確認：[結果と前提](ExtractionValidation.md)。単独構成・実ユーザー旧セーブ・1.7は未検証。
