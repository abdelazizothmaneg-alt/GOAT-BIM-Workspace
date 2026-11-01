---
name: arabic-rtl-best-practices
description: >-
  Guides software, desktop (WPF/WinForms/XAML), and web user interface development for Right-to-Left (RTL) Arabic localization,
  covering layout mirroring, icon orientation, logical padding, form layouts, and bidirectional data inputs.
---

# Arabic RTL UI & UX Best Practices

Engineering manual for designing and building software interfaces, dialogs, and plugins with native Right-to-Left (RTL) ergonomics.

## 1. Desktop UI & WPF / XAML Rules (e.g., Revit Plugins)
* **Root Window Flow Direction:**
  ```xml
  <!-- Set FlowDirection on the root Window or UserControl -->
  <Window x:Class="ORTools.UI.MyToolWindow"
          xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
          xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
          FlowDirection="RightToLeft"
          FontFamily="Cairo, Segoe UI, Tahoma"
          Title="أداة التصدير والتشطيبات">
  ```
* **Grid & Column Reversal:**
  * Setting `FlowDirection="RightToLeft"` automatically reverses `Grid.Column="0"` to the right side and `Grid.Column="1"` to the left side.
* **Text Alignment vs. FlowDirection:**
  * Avoid hardcoding `HorizontalContentAlignment="Right"`. Let `FlowDirection` naturally determine reading alignment.
* **Input Fields (TextBox) with Numerical Data:**
  * For phone numbers, file paths, and numerical dimensions (e.g., `C:\Program Files\...`, `192.168.1.1`):
  * Set `FlowDirection="LeftToRight"` locally on the specific TextBox to prevent cursor jumping and scrambled paths.

## 2. Icon Mirroring Decision Matrix
* **MUST Mirror (اتجاه الحركة والأفق):**
  * Back / Forward arrows (`←` becomes `→`).
  * Breadcrumb navigators (`>` becomes `<`).
  * Undo / Redo circular arrows.
  * Bulleted list indicators and chevrons.
* **DO NOT Mirror (عناصر عالمية وغير متجهة):**
  * Media playback controls (Play `▶`, Pause `❚❚`, Stop `■`).
  * Search magnifying glass (handle angled right).
  * Video camera, camera, printer, settings cog icon.
  * Clocks and gauges (hands always rotate clockwise).

## 3. Web & CSS Logical Properties
* Prefer CSS logical properties over physical directions:
  * Use `margin-inline-start` instead of `margin-left`.
  * Use `padding-inline-end` instead of `padding-right`.
  * Use `border-start-start-radius` instead of `border-top-left-radius`.
