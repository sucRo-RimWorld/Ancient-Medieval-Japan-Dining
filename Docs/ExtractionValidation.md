# 分離後の限定実ゲーム確認 — 2026-10-30 JST

対象はDiningの配布DLL `7e22363f53fa8e596a794a153807e0937866ffd0bf8baefd0abeaefcd677525c` と対応するAgriculture。移転元Agriculture `7a30b5d5e036534d9f08aa94efa96426343393c3` のDLLと同一。ソースは別途Windows 1.6参照でビルド成功。試験内で製品DLLを再ビルドせず、配布する同じバイナリをロードした。

隔離WSL2 AMJRuntime / Xvfb / 実MO・Daily Menu・Dining・Agriculture。ログのゲーム版は1.6.4871 rev600（ホストVersion.txtはrev598）。ホストログ日付はUTCの10月29日、作業日はJSTの10月30日。通常の有効Mod設定とプレイヤーセーブは変更していない。

| 前提と確認項目 | 結果 |
|---|---|
| Agricultureが穀飯を提供し、Diningが配膳を担当 | 実DoBillで4食調理、通常空腹AIで主食・MO汁・MO副菜を一膳化して喫食 |
| 膳の内容を食べ、膳自体は追加の一品にしない | 空腹回復、元料理3種を各1回記録、MOスープ効果維持 |
| 中断や少量喫食で残りを消さない | 中断時の膳と残存栄養を確認 |
| 保存対象は膳と内包食品 | Scribe保存・再読込後の栄養維持と通常喫食を確認（実ユーザーセーブ全体の移行ではない） |
| 代理給餌は既存の患者適格条件を使用 | 看護役が一膳を集めて麻酔患者へ給餌 |
| 全役割を必須にしない | 主食だけでも取餐Job成立 |
| Agricultureの料理方針は移転しない | 汎用簡単な食事の新規Bill停止、旧Def維持 |
| 上流Modを改変しない | Daily Menu配布物全ファイルのハッシュ一致 |

exit0、1シナリオPassed、failed/skipped/flakyは0。完全ログに既知のMO/SYR FMOD Unsupported fileエラー2件が重複して4行あり、全ERROR0ではない。作者既定の外部音声例外のみを適用し、新しい例外・XML/配膳エラーは0。

[結果](ValidationEvidence/Extraction-20261030/result.json)、[シナリオ集計](ValidationEvidence/Extraction-20261030/report-summary.json)、[完全ログ](ValidationEvidence/Extraction-20261030/unity.log.gz)、[検証用ロード順](ValidationEvidence/Extraction-20261030/ModsConfig.xml)。

未検証：AgricultureなしのDiningの実ゲーム、実際の旧セーブ全体、長期食料経済、1.7、全Mod構成、新UI。Agriculture/MO/Cultureへの依存がないことはXML・参照の静的確認であり、単独起動PASSとは呼ばない。膳グラフィックは既存画像の同一複製で、最終UIの見た目承認ではない。
