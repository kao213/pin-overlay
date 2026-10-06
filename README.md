# pin-overlay

[日本語](#日本語) | [English](#english)

---

## 日本語

好きな画像やテキストを、画面の最前面にピン留めできるオフラインのオーバーレイツールです。
もともとは Escape from Tarkov の Collector タスクで、必要なアイテムをレイド中に表示しておくために作りました。フォルダに画像を入れれば、ほかの用途にも使えます。

### 機能

- **画像の表示** — `images` フォルダのサブフォルダがタブになり、チェックを付けた画像を最前面に表示します
- **タブごとのかたまり** — タブ（フォルダ）ごとに 1 つのかたまりとして横 1 列に並びます。かたまりはドラッグで好きな位置に置けます
- **大きさの調整** — タブごとに「拡大率」「最大サイズ」「折り返し幅」を設定できます。並べた画像が折り返し幅を超えると次の行に送られます
- **テキストの表示** — 「テキスト」タブに書いた内容を、縁取り付きで表示します。フォント・サイズ・色・太字・斜体・揃え方を自由に設定できます
- **透過表示** — 表示中のかたまりはクリックを下のウィンドウに通すので、ゲームや作業の邪魔になりません
- **不透明度** — すべてのかたまりの不透明度をまとめて変えられます
- **設定の保存** — チェック・位置・大きさ・タブの並び順・テキストは自動で保存され、次に起動したときも同じ状態で表示されます
- **オフライン動作** — インターネット接続は不要です

### 動作環境

- Windows 10 / 11（64 ビット）

### 使い方

1. [Releases](../../releases) から `pin-overlay.exe` をダウンロードし、好きなフォルダに置きます
   - `C:\Program Files` などの書き込みに管理者権限が必要な場所は避けてください（設定を保存できません）
2. `pin-overlay.exe` をダブルクリックして起動します。初回起動時に、exe と同じ場所に `images` フォルダが作られます
3. `images` フォルダの中にサブフォルダを作り、画像を入れます。サブフォルダ 1 つがタブ 1 つになります

   ```
   pin-overlay.exe
   settings.json      ← 設定（自動で作られます）
   images/
   ├─ Xxx/
   │  ├─ Aaa.png
   │  └─ Bbb.png
   └─ Yyy/
      ├─ Ccc.png
      └─ Ddd.png
   ```

   - 対応形式：PNG／JPEG／BMP／GIF（静止画）／WebP
   - `images` の直下に置いた画像と、孫フォルダの中身は表示されません
4. 設定画面の「⟳ 再読み込み」を押すと、タブが表示されます
5. 表示したい画像にチェックを付けます。画面上にかたまりが表示されるので、ドラッグで好きな位置に動かします
6. 「透過して表示 ▶」を押すと設定画面が閉じ、かたまりはクリックを下に通す状態になります
7. 設定画面に戻るには、かたまりの右上の ⚙ を押すか、タスクトレイのアイコンを右クリックして「設定画面を開く」を選びます

アプリを終了するには、設定画面を閉じるか、タスクトレイのアイコンを右クリックして「終了」を選びます。

### 注意事項

- **ゲームの画面モードは「ボーダーレス（ウィンドウ）」にしてください。** フルスクリーンの上には表示できません
- **チート対策ソフト（BattlEye など）について：** このツールは最前面にウィンドウを表示するだけで、ゲームのメモリを読んだり、入力を横取りしたりはしません。ただし、問題にならないことは保証できません。**自己責任でお使いください**
- **このツールは Battlestate Games の公認・提携ツールではありません。** 使用を許可されたものでもありません
- **ゲームの画像はこのリポジトリに含めていません。** 画像は各自で用意して `images` フォルダに入れてください。ゲーム内の画像を他の人に配布することは、ゲームの利用規約で禁止されています
- **WebP 画像**を表示するには、Windows に「WebP 画像拡張機能」が入っている必要があります（Microsoft Store から入手できます）。読み込めない画像は、設定画面に「読み込めません」と表示されます

### ビルド

[.NET 10 SDK](https://dotnet.microsoft.com/) が必要です。

```
dotnet test tests/PinOverlay.Core.Tests
dotnet publish src/PinOverlay -c Release -o publish
```

`publish/pin-overlay.exe` が、実行環境を同梱した単一の exe です。設計は [docs/design.md](docs/design.md) を参照してください。

---

## English

An offline overlay tool that pins any image or text you like on top of all other windows.
It was originally built to keep the items needed for the Collector task in Escape from Tarkov visible during raids, but it works for anything you put in its image folder.

### Features

- **Images** — Each subfolder of the `images` folder becomes a tab. Checked images are shown on top of all windows
- **One group per tab** — Images from each tab are shown in a single row. Drag each group wherever you like
- **Sizing** — Set a scale, a maximum size, and a wrap width per tab. Images that would exceed the wrap width move to the next row
- **Text** — Whatever you write in the "テキスト" (Text) tab is shown with an outline. Font, size, color, bold, italic, and alignment are all customizable
- **Click-through** — While overlays are shown, clicks pass through to the window underneath
- **Opacity** — Change the opacity of all groups at once
- **Saved settings** — Checks, positions, sizes, tab order, and text are saved automatically and restored on the next launch
- **Offline** — No internet connection required

### Requirements

- Windows 10 / 11 (64-bit)

### Usage

1. Download `pin-overlay.exe` from [Releases](../../releases) and put it in any folder
   - Avoid folders that need administrator rights to write to, such as `C:\Program Files` (settings cannot be saved there)
2. Double-click `pin-overlay.exe`. On first launch, an `images` folder is created next to the exe
3. Create subfolders inside `images` and put images in them. Each subfolder becomes one tab
   - Supported formats: PNG / JPEG / BMP / GIF (first frame) / WebP
   - Images directly under `images` and anything in nested subfolders are ignored
4. Press "⟳ 再読み込み" (Reload) in the settings window to show the tabs
5. Check the images you want to show. A group appears on screen; drag it where you want it
6. Press "透過して表示 ▶" (Show overlay). The settings window closes and clicks pass through the groups
7. To return to the settings window, click the ⚙ at the top-right of a group, or right-click the tray icon and choose "設定画面を開く" (Open settings)

To quit, close the settings window or right-click the tray icon and choose "終了" (Exit).

### Notes

- **Set the game to borderless (windowed) mode.** Overlays cannot be shown over exclusive fullscreen
- **About anti-cheat software (BattlEye, etc.):** This tool only shows topmost windows; it does not read game memory or intercept input. However, there is no guarantee it will never be flagged. **Use at your own risk**
- **This tool is not affiliated with or endorsed by Battlestate Games.** Its use has not been approved by them
- **No game images are included in this repository.** Provide your own images in the `images` folder. Distributing in-game images to others is prohibited by the game's license agreement
- **WebP images** require the "WebP Image Extensions" to be installed on Windows (available from the Microsoft Store). Images that cannot be loaded are marked "読み込めません" (Cannot load) in the settings window

### Build

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/).

```
dotnet test tests/PinOverlay.Core.Tests
dotnet publish src/PinOverlay -c Release -o publish
```

`publish/pin-overlay.exe` is a single self-contained exe. See [docs/design.md](docs/design.md) for the design.
