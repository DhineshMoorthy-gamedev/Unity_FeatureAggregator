# Feature Aggregator

**Feature Aggregator** is a Unity Editor tool designed to help you organize your project's scripts and assets into logical "Features". It provides a centralized window to view, access, and manage related files without hunting through the Project view.

## Features

-   **Logical Grouping**: Create "Features" to group related scripts, scenes, prefabs, and other assets.
-   **Tag System**: Categorize features with tags like "Core", "Experimental", "Legacy", and filter them effortlessly.
-   **Dependency Graph**: Visualize your project's architecture with a dynamic graph showing how features depend on each other.
-   **Quick Access**: Open all related scripts in your IDE with a single click.
-   **Context Menu Integration**: Add files to features directly from the Project view right-click menu.
-   **Drag & Drop**: Easily add scripts and assets to a feature by dragging them into the inspector window.

## Installation

This package can be installed from disk or via a scoped registry if configured.

## Usage

1.  Open the tool via **Tools > GameDevTools > Feature Aggregator**.
2.  Click **+ New Feature** to create a named group.
3.  Select a feature from the list.
4.  Drag and drop scripts or assets into the respective drop zones, or use the **Right Click > Feature Aggregator > Add Selected to Feature...** menu in the Project view.
5.  Click **Open** next to an item to verify it, or **Open All Scripts** to open the entire feature's codebase in your default IDE.

## License

MIT
