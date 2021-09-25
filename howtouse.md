# Quick Guide
- **Press F10 to open menu**
- **Press F9 to make curve**

1. Select x number of notes **Must not be on the same time**
2. Press F9 to generate a poodle between the selected notes  
![Cool Gif](https://github.com/DavidHulstroem/PaulMapper/blob/main/PaulArrow.gif)  
- if at least two notes are chroma colored it will make a gradient between them  
![Cool Gif](https://github.com/DavidHulstroem/PaulMapper/blob/main/PaulGradient.gif)  
- if all notes are dots it will make the curve using dots  

*also works with bombs*  

## Menu Settings
The menu can be moved around, and will save its settings  
![Imgur](https://i.imgur.com/c9SXMwZ.jpg)
- Precision   
Changes how dense the paul will be, (the distance between each note)
- Vibro *Use at your own risk*  
will make each note in the curve alternate between up and down  
- Rotate  
Will make the poodle automatically find a rotation of each note, if false  
all the notes will have same rotation as the first note
- Transition  
If a dot is selected when generating a poodle, it will make notes around it  
also be dots. How many beats in front and back of dot it should convert.  
![Imgur](https://i.imgur.com/LrL47Sq.gif)
- Keep Rotation  
if set to true it will make the dots in the transition have the normal  
rotation, as if it was a normal poodle

- Find All Pauls  
When pressed, will find every paul in the map, and expand the menu with new buttons  
that will allow you to navigate between all the pauls in the map  
![Imgur](https://imgur.com/aACGl3b.jpg)
- Select Current  
Will select the closest paul and all of its notes, remember to click *Find All Pauls*  
first, or else it might not select the correct one.

## QuickMenu
By selecting only two note a quick menu will appear to the right of the normal menu.  
Contains a few different "presets" for how the curve will look. Most commonly used are SineOut and CubicOut   
If rotate is set to false, both notes must have same cut direction.  
![Imgur](https://i.imgur.com/LHufo9R.jpg)  
To get an idea of how these curves behave take look at:
https://easings.net/

## RealtimeCurves
Press **F12** to create a realtime curve with moveable anchorpoints   
Press **C** to add another anchor point  
Middle mouse click point to remove them   
Finish the curve by deselecting everything with **crtl+A**  
![Imgur](https://imgur.com/bYEHOcy.gif)



## Tips
- By enabling precision placement in the chromapper settings, you can hold down the designated key *(Q by default)*  
to place notes freely, and this will allow you to more precisly maniupulate how the curve looks.
