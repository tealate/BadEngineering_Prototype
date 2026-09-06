# 次回Prototypeテスト

## 起動

- Unity 6000.3.23f1で `Assets/Scenes/PrototypeLan.unity` を開いてPlay。
- Host側は `Host`、もう一方はHost PCのLAN IPv4を入力して `Connect`。
- UDPポートは7777。Windowsのファイアウォール確認が出た場合は、使用するプライベートネットワークでアプリを許可する。
- `Esc`でマウスを解放／再取得し、接続メニューを開く。
- Windowsビルドは `Builds/LanPlaytest/BadEngineering.exe`。

## 操作

| 操作 | キー |
|---|---|
| 移動・操縦 | WASD |
| ジャンプ／運転時ブレーキ | Space |
| 拾う・乗る・降りる | E |
| 設置・自分の搭載武器を回収・タイヤ交換 | F |
| 共通スロット選択 | 1 / 2 / 3 |
| 選択ItemのDrop | Q |
| 射撃 | 左クリック |
| Weapon Aim | 右クリック長押し |
| 設置モード切替 | 中クリック：位置→X→Y→Z |
| 設置回転 | マウス左右。回転中は設置点を車体に固定 |
| 重砲のゼロ点距離 | Aim中のホイール |

固定砲・可動砲・重砲が車両手前に配置される。初期武器は固定砲。
タイヤは1輪のItemとして拾い、車体へFで全輪の種類を交換する。
旧タイヤが交換に使ったItem枠へ戻る。同種交換は何もせず拒否する。

## Editor調整

- `Assets/PrototypePlaytest/Settings`：武器の反動・弾速・Cooldown・Aim制限・重砲追従速度・距離範囲、Preview Material。
- `Player.prefab`：カメラの距離・高さ・追従速度・障害物回避、Preview回転感度・Snap。
- `TireDefinition`：通常Grip（Tread）、側面摩擦、接触角の閾値。元PrototypeTireのGrip=3を維持。
- `LAN Session`：接続IP・ポート・タイヤ定義一覧。Gameplayの要求検証はNetworkPlayer／PlayerInteractor、状態同期はNetworkActor。
- `Bad Engineering/Prepare LAN Playtest` は元PrototypeTestからLAN Sceneと生成Prefabを再構築する。LAN Scene／生成Prefabを手調整した後は再実行前に差分を保存する。武器設定Assetは既存値を保持する。

## 方針と範囲

NGO 2.7.0 / Unity Transport。Listen ServerがPlayer・Vehicle・Weapon・Projectileの物理と状態を確定する。
GameplayのWeapon OwnerはNetwork Authorityとは独立。操作要求はPlayerのNetwork所有権で検証する。
確定状態は20Hzを初期値として同期し、Clientは補間表示する。Camera／Ghostはローカル専用。
重砲の距離は同じ高さの目標への水平距離・低弾道解。高低差の自動計測や弾道予測UIは含めない。
現在は4輪共通TireDefinition。1個のItemで全輪交換し、個別装着・混在設定は持たない。
敵・HP・弾薬・リロード・破壊・Relay・Steam・Prediction・複数Pivotは対象外。

## 検証コマンド

- Editor executeMethod `BadEngineering.Editor.PlaytestValidation.ValidateAssets`
- Editor executeMethod `BadEngineering.Editor.PrototypePlayModeSmokeTest.RunFromCommandLine`（quitは付けない）
- Editor executeMethod `BadEngineering.Editor.PlaytestSceneBuilder.BuildWindows`
- ビルド2プロセス：`-batchmode -nographics -prototypeHost -prototypeProbe` と `-batchmode -nographics -prototypeClient 127.0.0.1 -prototypeProbe`。
- Probeは明示的な起動引数がある場合のみ動く。結果は `PROBE SERVER/CLIENT PASSED`、失敗は非0終了。
- Graphicsありのビルド：`-batchmode -force-d3d11 -prototypeHost -prototypeCapture <絶対PNGパス>`。Ghost描画、Visual専用構造、XYZ回転、設置要求による実物とのTransform一致を確認する。画像はCamera描画のみでOverlay UIを含まない。

## 2026-09-07 再開後の検証結果

前回の差分を引き継ぎ、今回のNotion主仕様と再照合した。対象機能の実装は揃っている。未着手の対象内機能・仕様判断による保留はなく、実機2台での体感・長時間物理検証が残る。

| 検証 | 結果 | Logs内の記録 |
|---|---|---|
| Scene／Prefab／設定参照・Collision Layer・弾道式・摩擦分類 | 成功 | final-build.log |
| Unityコンパイル・Windows Development Build | 成功 | resume-final-build.log |
| 既存Unity Play Mode smoke test | 成功 | resume-smoke.log |
| 2プロセスListen Server接続・席交代・Client操縦 | 成功 | resume-host.log / resume-client.log |
| Owner制約・Attach／回収／Drop／Pickup・Projectile同期 | 成功 | 同上 |
| 共通スロット・全輪タイヤ交換・旧タイヤ返却 | 成功 | 同上 |
| Client重砲Aim・ゼロ点操作と同期 | 成功 | 同上 |
| Ghost描画・物理なし・XYZ回転・設置Pose一致 | 成功 | resume-visual.log / preview.png |
| dotnet Runtime／Editorビルド | エラー0件。既存PackageのDLL競合警告あり | dotnet-runtime.log / dotnet-editor.log |

通信テストは同一PC上の別実行プロセスによる127.0.0.1接続。別PC間LAN、実際のマウス操作の感触、長時間の横転・衝突安定性、途中参加・切断競合は次回2人テストで確認する。自動テスト成功を人間によるテストプレイ完了とは扱わない。

既存のユーザー変更であるPrototypeTest Scene、PrototypeTire Asset、AGENTS.mdは作業開始時の内容とハッシュ一致を確認済み。生成・ビルドに伴うURP設定のUnity再シリアライズが差分に含まれる。

外部ビルド前に古いcsprojの参照漏れを解消するため、既存Riderパッケージの `Packages.Rider.Editor.RiderScriptEditor.SyncSolution` で再生成した。`com.unity.pipeline`のCodeAnalysis DLLに由来するMSB3277警告が外部ビルドに残る。今回のコードではなく既存Package間の依存であり、Unityのコンパイル・Playerビルドでは再現していないためPackage差し替えは行っていない。`git diff --check`は既存ユーザーScene内の空のm_Name末尾空白を検出するが、その行は保持している。

## 2人で確認する項目

- HostとClientでDriver／Crewを交代し、同じ車両を操縦する。
- Owner以外の回収禁止、同時Pickup／同時着席の競合、Drop後の別Owner取得。
- Attachedのまま直接再設置できず、F回収→再配置で位置と向きが一致する。
- Ghostと実物、4回転モード、移動する車体上の設置点が一致する。
- 3種のAim、右クリック終了・スロット切替・Drop・乗降で視点や連射が残らない。
- 重砲の距離目盛りと弾道、旋回・俯仰の遅れ、角度限界。
- タイヤItemが地面とPlayerへ衝突し、装着後もPlayerへ衝突する。交換で全輪が変わる。
- 横転時に側面の接地・摩擦が安定し、タイヤが車体を不自然に弾き飛ばさない。
- 反動・衝突・横転の見え方、入力遅延、途中参加、Client切断後の空席・Drop。
