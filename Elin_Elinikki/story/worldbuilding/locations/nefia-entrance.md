---
name: "ネフィア入口"
type: ruins
region: "異常ネフィア外縁"
notable-characters:
  - mina
  - sora
  - protagonist
tags:
  - nefia
  - entrance
  - return-point
status: abandoned
---

## Description

岩場の裂け目。入口は自然の洞窟に見えるが、奥に進むと空気が変わる。エーテル濃度の変化を肌で感じる。外はElinの通常の風景。中に入った瞬間、BGMが切り替わり、光の質が変わる。

## Notable Features

- **依頼看板**: 入口手前。異常ネフィア調査の依頼内容が書かれている
- **合流地点**: ミナとソラがここで待っている
- **帰還地点**: 帰りもここに出る。同じマップを再利用するが、帰還フェーズではユウが同行しており、会話内容が変わる

## Story Function

- 0章: 導入。仲間合流。ネフィアへ入る決意
- 5章: 帰還。ユウと歩きながら真相を聞く場所
- 再訪エンド: 一人でもう一度入る場所

## Implementation

- マップ: `nefia_entrance`
- 演出: 入口手前は通常BGM。入口を越えるとBGM切り替え
- 帰還時: 同じマップ、通常BGM。日常に戻る演出
