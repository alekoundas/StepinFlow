# StepinFlow

![StepinFlow Logo](https://via.placeholder.com/150) <!-- Replace with your logo URL if you have one -->

**StepinFlow** is a powerful, portable desktop application built with .NET Core, designed to streamline workflows and automate repetitive tasks. Whether you're managing complex processes or simple repetitive tasks, StepinFlow provides an intuitive interface and robust functionality to get the job done efficiently.

## Features

- **Workflow Management**: Create, edit, and execute multi-step flows.
- **Customizable Parameters**: Define and tweak flow with reusable parameters to allow for a quick update in every step that uses them.
- **Execution Tracking**: Monitor progress with detailed execution logs.
- **Database Integration**: Uses an SQLite database to store workflows, steps, and execution history, ensuring data persistence.
- **Export/Import Flows**: Export flows for backup or simply sharing with other users.
- **Screenshot Importer**: Allow for easy screenshot selection and import as a search template.
- **Portable Design**: Run StepinFlow without installation, unzip and go!

## Technologies Used

StepinFlow leverages the following technologies and libraries:

- [.NET 8.0](https://dotnet.microsoft.com/): The core framework for building cross-platform applications.
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/): An object-relational mapper (ORM) for database operations.
- [SQLite](https://www.sqlite.org/): A lightweight, file-based database system.
- [AutoMapper](https://automapper.org/): A convention-based object-to-object mapper for .NET.
- [OpenCvSharp](https://github.com/shimat/opencvsharp): A .NET wrapper for the OpenCV computer vision library.
- [Windows Presentation Foundation (WPF)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/): A UI framework for building Windows desktop applications.
- [MVVM (Model-View-ViewModel)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/mvvm): An architectural pattern for separating UI logic from business logic in WPF applications.

## Getting Started

### Prerequisites
- Windows 10 or later (64-bit).
- No additional runtime installation required—StepinFlow is self-contained!

### Installation
1. Visit the [Releases](https://github.com/alekoundas/StepinFlow/releases) page.
2. Download the latest `StepinFlow-Portable-win-x64-vX.X.X.zip` file (e.g., `v1.0.106`).
3. Extract the ZIP to a folder of your choice.
4. Run `StepinFlow.exe` to launch the app.

### Usage
1. **Create a Flow**: Use the TreeView interface to define a new workflow.
2. **Add Steps**: Add individual steps and parameters to your flow.
3. **Execute**: Run your flow and track its progress in real-time.
4. **Save & Reuse**: Save workflows to the SQLite database for later use.

## Screenshots


![Main Window](https://via.placeholder.com/600x400?text=Main+Window)  
*Main interface showing workflow creation.*

![Execution Log](https://via.placeholder.com/600x400?text=Execution+Log)  
*Execution log displaying step-by-step progress.*

## Building from Source

### Requirements
- .NET 8.0 SDK
- Visual Studio 2022 (or later) with Desktop Development workload

### Steps
1. Clone the repository:
    ```bash
    git clone https://github.com/alekoundas/StepinFlow.git
    cd StepinFlow
    ```

2. Restore dependencies:
    ```bash
    dotnet restore StepinFlow.sln
    ```

3. Build the solution:
    ```bash
    dotnet build StepinFlow.sln --configuration Release
    ```

## Available Flow Step Types

StepinFlow offers a variety of flow step types tailored for automating GUI interactions and screen-based workflows. Below is a list of available step types with their descriptions:

- **WAIT**
  - **Description**: Pauses the workflow for a specified duration, allowing time for external events or conditions to occur.
  - **Use Case**: Wait 5 seconds for a window to load or a process to complete.

- **LOOP**
  - **Description**: Repeats all children steps for a set number of times or not.
  - **Use Case**: Click a button repeatedly or iterate over a list of items.

- **SUB_FLOW_STEP**
  - **Description**: Executes a nested sub-flow, allowing modular and reusable flow segments within the main workflow.
  - **Use Case**: Run a pre-defined subroutine to handle a common process or a repetitive task.

- **WINDOW_MOVE**
  - **Description**: Moves a specified window to a new position on the screen.
  - **Use Case**: Relocate an application window to a specific coordinate (e.g., x:100, y:200).

- **WINDOW_RESIZE**
  - **Description**: Resizes a specified window to a given width and height.
  - **Use Case**: Adjust a window to 800x600 pixels to ensure consistent UI element positioning.

- **TEMPLATE_SEARCH**
  - **Description**: Uses OpenCvSharp to search the screen (optional use of a flow parameter as the search area) for a single image template. If result accuracy is above the set value, the children under "Success" will be executed. Else the "Fail" children will be executed.
  - **TemplateMatchModes**: The following modes are available for template matching:
    | Mode                | Description                                                                 | Best For                          |
    |---------------------|-----------------------------------------------------------------------------|-----------------------------------|
    | `TM_SQDIFF`         | Measures the squared difference;.                                           | Precise matching with exact sizes |
    | `TM_SQDIFF_NORMED`  | Normalized squared difference; accounts for lighting/scale differences.     | Robustness to brightness changes  |
    | `TM_CCORR`          | Computes correlation; higher values indicate better matches.                | Simple pattern matching           |
    | `TM_CCORR_NORMED`   | Normalized correlation; less sensitive to lighting changes.                 | General-purpose matching          |
    | `TM_CCOEFF`         | Correlation coefficient; measures similarity with zero-mean adjustment.     | Matching with contrast variance   |
    | `TM_CCOEFF_NORMED (Recomended)`  | Normalized correlation coefficient; robust to lighting and scale changes.   | High accuracy in varied conditions|
    
  - **Use Case**: Locate a "Reward" button icon on the screen to determine where to click.

- **MULTIPLE_TEMPLATE_SEARCH**
  - **Description**: Same as TEMPLATE_SEARCH but searches for multiple instances of an image template on the screen. "Success" or "Fail" children will be executed on every template image instance.
  - **Use Case**: Find all occurrences of different "Reward" icons in a list to perform actions.

- **WAIT_FOR_TEMPLATE**
  - **Description**: Same as TEMPLATE_SEARCH but executes continius comparisons until the specified image template appears on the screen.
  - **Use Case**: Wait up to 10 seconds for a "Loading" spinner to disappear before proceeding.

- **CURSOR_SCROLL**
  - **Description**: Scrolls the mouse wheel up,down,left,right by a specified amount.
  - **Use Case**: Scroll a webpage or listbox to reveal hidden content.

- **CURSOR_CLICK**
  - **Description**: Simulates a mouse click, click and hold, release for left, right buttons at the current cursor position.
  - **Use Case**: Click left button.

- **CURSOR_RELOCATE**
  - **Description**: Moves the cursor to a new position on the screen. Either manual (x,y coordinates) or by selecting the location of a template search result (step needs to have any TEMPLATE_SEARCH ancestor). 
  - **Use Case**: Position the cursor at the location of a specific template image.

- **GO_TO**
  - **Description**: Redirects the workflow to a previous step, enabling non-linear execution paths (May lead to unexpected infinite loops depending on your logic).
  - **Use Case**: Jump back to a previous step on failure or skip to a cleanup step.








