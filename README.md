# NeeView

アプリの使用についてはWikiを参照してください
  
  * [NeeViewプロジェクト Wiki](https://bitbucket.org/neelabo/neeview/wiki/)

## 開発環境

* Windows 10 Pro 64bit
* VisualStudio 2017 Community  
    - ユニバーサル Window プラットフォーム開発
    - .Net デスクトップ開発
        - (追加) .Net Framework 4.6.2 開発ツール
    - C++によるデスクトップ開発
    - 個別のコンポーネント
        - Blend for Visual Studio SDK for .NET

## Gitリポジトリからのプロジェクト取得

% git clone --recursive https://bitbucket.org/neelabo/neeview.git NeeView

## 配布パッケージ作成

* PowerShell
* [pandoc 1.19.2.1](http://pandoc.org/)
* [WiX Toolset 3.11](http://wixtoolset.org/)

配布用のZip,Msiを作成します。  
`Deploy.ps1` (PowerShell Script) でビルドからパッケージ化までを行っています。  
markdown から ドキュメント用html を作成するために `pandoc` を使用しています。
msiパッケージ作成に `WiX Toolset` を使用しています。