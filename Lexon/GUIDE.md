# Lexon — basic guide

Lexon runs in the background and helps while you type in other apps.

## Tray icon

Right-click the Lexon icon next to the clock:

- **Settings** — options (or press **Ctrl+Shift+S**)
- **Keyboard Shortcuts** — list of keys
- **Toggle** — turn assistance off/on (or **double-press Ctrl**)
- **Exit** — quit Lexon

## Word suggestions

1. Type in almost any text box.
2. A small popup lists completions.
3. **Up / Down** moves the highlight.
4. **Tab** inserts the highlighted word (adds a space).
5. **Esc** hides the list.
6. **Space** does **not** accept a suggestion (it just types a space).

If the list covers your text, press Esc, then keep typing.

## Spelling

Common typos (like `teh` → `the`) show in the **word** popup. **Tab** accepts.

## Grammar

Grammar fixes (like `he are` → `he is`) appear in a **separate** popup labeled **Grammar**, on the other side of the line from word suggestions.

- **Tab** accepts the grammar fix when that popup is showing (it takes priority over the word list).
- **Up / Down** still move the word list when both are visible.
- After you pause (~1.5 seconds), the grammar popup may appear again. **Tab** still accepts.
- **Ctrl+Alt+G** (if enabled in Settings) opens a full issues list.

Grammar can be turned off in Settings: **Automatically suggest grammar fixes**.

## Rewrite (optional)

Select a sentence, then:

- click the small **Aa** control near the selection, or
- **Ctrl+Alt+R** / **Ctrl+Shift+R** if those shortcuts are enabled.

Cloud rewrite needs an AI provider in Settings. Without a key, skip this.

**Ctrl+Shift+Z** undoes the last Lexon insert/edit (not the app’s own Undo in every program).

## Text expansions

In Settings you can add shortcuts, for example `addr` → your address. Type the shortcut, then a space.

## Privacy while testing

Lexon should **not** suggest in password boxes. It also skips some remote/admin tools.

If you use a cloud AI key, only features that call that provider send text off the machine (for example rewrite). Everyday word lists stay local.

Please **do not** type real passwords, secrets, or private documents into test windows if you have cloud AI turned on.

## Updates

Lexon checks GitHub for new versions on startup; **no typing data is included in that check**. It only asks GitHub "what is the newest version?"

If a new version exists, Lexon downloads it in the background and then asks whether to restart. Nothing is installed until you answer **Yes**. You can also check any time from the tray icon → **Check for updates…**, and turn the startup check off in Settings → General.

This check is separate from the cloud AI setting. Local-only mode stops text being sent to AI providers; it does not disable update checks.

## What to try (for friends)

Please try these and note what happens (app name, what you typed, what you expected):

1. Notepad: `helo` + Tab  
2. Notepad: `he are` + Tab  
3. Google Docs in Chrome: same two tests (click in the page first, then type)  
4. Word or another desktop app: same two tests  
5. A password field: confirm **no** popup  
6. Double-press Ctrl: assistance pauses, then again to resume  
7. Exit from the tray, start `Run\Lexon.exe` again  

Google Docs is a web editor, not a normal text box. Word suggestions and Tab-to-fix grammar should work after you click in the document. Selecting text and using rewrite / the Aa chip is still unreliable there.  

## Troubleshooting

**No popup at all**

- Is the tray icon present? If not, start `Run\Lexon.exe` again.
- Double-press Ctrl (it may be toggled off).
- Try Notepad first; some apps block injected keys.
- Run as a normal user. If the target app is **Run as administrator** and Lexon is not, Windows can block typing into that app.

**Popup appears but Tab does nothing / text looks missing**

- Press Esc, click in the text box, try again.
- Tell Liam the app you were in (Notepad vs Word vs browser).

**Windows SmartScreen**

- More info → Run anyway. The test build is not a store-signed installer.

**Want it gone**

- Tray → Exit, then delete the unzipped folder.
