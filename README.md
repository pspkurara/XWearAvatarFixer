# XWearAvatarFixer

[![](https://img.shields.io/github/watchers/pspkurara/sceneries?style=social)](https://github.com/pspkurara/external-selecion-state/subscription)

## 概要

XWear Packager/VRoid Studioの着せ替え機能で書き出したVRM1.0の物理演算が失われる問題を簡易的に修復します。

## 修復される問題

- すべての揺れ物が以下の値にされる
</br>![image](https://github.com/user-attachments/assets/468d84be-0894-4b52-8671-9aa33bdfadd3)
- 揺れ物のオブジェクトで必須のエンドボーン（末端ボーンの長さを決めるためだけのボーンで、VRChatのPhysBoneでは値で管理するため不要）がない場合に作られない
- 一部のコライダーが作られない ※現状は衣装かつ完全に失われている場合にのみ対応

## 注意

- あくまで作者の環境で発生している症状を暫定的に修復するためのツールですので、その他の環境やデータで動作しない可能性があります。
- 書き出したXWear/XAvatar/XRoidによっては全く違う症状がでたり、でなかったりする可能性があります。
- VRChat SDKとVRM1.0の物理演算は作りが全く違うため、同一の挙動が再現されない場合があります。
- 仕組み上、オリジナルのデータを元に修復するため、人からもらった単体のXWear等は直りません。

## 確認環境

[XWear Packager](https://vroid.notion.site/XWear-Packager-8284c73c208e440ba8dd8033349d5978) v0.3.2
[VRoid Studio](https://vroid.pixiv.help/hc) v2.0.0
Unity 2022.3.2f1

その他XWear Package該当バージョンの推奨環境

## 必要なもの

- *.xwear化する直前のUnityプレハブ
- VRoidStudio着せ替え機能で書き出した*.vrm

## 使い方

以下fanbox記事参考 (全体公開です)
https://djkurara.fanbox.cc/posts/8970211

## ライセンス

* [MIT](https://github.com/pspkurara/XWearAvatarFixer/blob/main/Assets/XWearAvatarFixer/LICENSE.txt)

## Author

* [pspkurara](https://github.com/pspkurara) 
[![](https://img.shields.io/twitter/follow/pspkurara.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=pspkurara) 
