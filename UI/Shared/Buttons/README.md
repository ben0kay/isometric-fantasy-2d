# Shared Buttons

`UIButtonFactory.Create()` builds a styled button; `UIButtonFactory.Apply()` styles an editor-authored button. Both use `DefaultButtonStyle.tres` / `UIButtonStyle.cs` with Primary, Secondary, Danger and Small variants. Styles apply once, using native hover/pressed theme states, without per-button `_Process`. This factory is for ALL game UI, not just menus.
