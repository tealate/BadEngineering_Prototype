\# BadEngineering



Unity 6で開発するゲームプロジェクト。



\## Project Goal



PvE協力型アクションゲーム。



プレイヤーは車両の任意位置に武器を取り付け、

重量・重心・反動による物理挙動を楽しむ。



現在はPrototype段階。



\## Development Policy



\- Unity 6を使用

\- C#を使用

\- 可能な限りシンプルな実装を優先

\- Prototypeでは拡張性より検証速度を優先

\- 過剰な抽象化を避ける

\- Rigidbodyを利用した物理挙動を優先

\- 既存コードを変更する場合は影響範囲を確認する

\- 調査・ツール呼び出し・説明は作業に必要な範囲に絞る

\- 応答は結論中心で簡潔にする



\## Current Prototype Scope



1\. Player movement

2\. Vehicle movement

3\. Enter / exit vehicle

4\. Weapon pickup

5\. Attach weapon to vehicle

6\. Fire weapon

7\. Apply recoil to vehicle


\## Project Documentation


このプロジェクトの企画書は、以下のNotionページを一次資料として参照する。


\- [BadEngineering](https://app.notion.com/p/dfe1fd1a615183c08d2c0193458ec5ef?pvs=204)


通常の作業ではNotionの確認・更新を行わない。

Notionの確認は、ユーザーから依頼された場合、または作業に不可欠でチャットと既存実装だけでは判断できない場合に限る。

Notionへの書き込みは、ユーザーから明示的に依頼された場合のみ行う。

仕様が不明でも安全に判断できる範囲は合理的に進める。結果が大きく変わる判断のみ、ユーザーへ短く確認する。

