# SolarSystemExplorer
A small Unity project that allows users to explore the **Solar System** in VR.

## How to use
Open the repository folder as a project with **Unity 2022.3.4f** or later.  
In the content browser navigate to **'Assets/Scenes'** and open **'SolarSystemScene'**.  
Now press play.

## Controls
![Control Layout](pictures/Controls.png)
- Move: Continuous Movement
- Select:
    - Teleport to the celestial body the controller is currently pointing at
    - Interact with UI and other points of interest
- Go Back: Return to view of the entiere solar system
- Manipulate Time:
    - Move finger clockwise: Go forward in time
    - Move finger counterclockwise: Go back in time
    - Click: Pause/Unpause time
- Faster Time Manipulation: Switch from one day per rotation to one year per rotation
- Switch Tool: Cycle through available tools
- Use Tool: use the currently selected tool

## Tools
Tools are used to extract some kinds information from the currently visited celstial body.  
Currently these tools include:

### Thermometer
Measure the **Average Surface Temparature** by poking a celestial body and pressing the **Use Button**.

### Chemical Flask
Extract the **Atmospheric Composition** by poking a celestial body and pressing the **Use Button**.

### Magnifying Glass
Hide or reveal **Points of Interest** around a celestial body by pressing the **Use Button**.

### Ruler
Measure the **Equatorial Radius** of a celestial body by pressing the **Use Button**.

### Calendar
Displays the **Current Simulation Time** as well as the **Speed** with wich time is currently moving.  
This **Time Speed** can be manipulated by pressing the **Use Button**:
- Single press: speed up time
- Double press: slow down time
