# Stream Deck Hub

## Introduction
The Elgato Stream Deck is an amazing tool to keep me organized with notifications and shortcuts in my daily life. Because I work from home, notifications constantly get in my way, forgotten, or missed because I'm gaming, in meetings, etc. This "Hub" will let me stay in sync with my notifications by reading the Windows 10 Action Center and highlighting the appropriate icon so I know when there's a notification. I've also added in a chime that will notify me on when to look down. 

I know the windows action center exists and so does that system tray, but what bothers me about those two is that they’re disruptive and always on my primary monitor. There’s been numerous times when I’ve received an email while gaming, and that notification square shows up in the game and I accidently click it. With the Stream Deck, I’ve disabled all of those popups and funnelled all the notifications into my Stream Deck so I can visually see what notifications are pending, and I can click on the buttons to open up the app if needed. This is helpful because I don’t have to alt-tab out of my game and look all over the place for the applications.

## Current State
Right now things are REALLY messy. I wanted to create this quickly so I can use it right away, and it’s working fantastically. It’s replaced the Stream Deck official software, so there are things missing, but the main things that I need are there:

- Hotkeys
- Open urls
- Open programs
- Highlight notifications
- Clear notifications
- Create icon folders

The system is currently reading from a json file, buttons.json.example. I just put in a couple of nodes in that file as an example, but I’m hoping to remove that and have the, currently empty, ui create those as needed. If you are trying to create buttons, you can review the StreamDeckButton.cs file to see the structure. I’ve also hardcoded some things, like google, for MY purposes, so keep that in mind as well. 

## Future
Because it’s working, unless there are bugs, the system is doing what I need it to do. I’ll be slow to updating, but I hope to address the following:

- Allow users to add icons from the UI
- Show unmatched notifications
- Change the Action Center notification into an interface so I can have different types of notifications
- Save my images as resources

## Building and running
This was built in Visual Studio 2019. To build and run
1. Open the project
2. Restore the nuget packages
3. Rename buttons.json.example to buttons.json and move it into the build exe directory
4. edit the buttons.json with the tiles that you want
5. just do a build and run

## Notification System
I wanted to do some other notification things, but to get this done, I had to hardcode some exceptions into the notification system. Because chrome's notification system does not split the different types, calendar notification vs email vs something else, I made exceptions in the notification systems. When using this, I decided to bring this a bit further and have sort of an "integration" with nest. I have my nest sending files to my server, and my server sends me an email when those files have been processed. I've added hardcoded logic to look for this specific email and send it to the Nest tile. This allows me to replace the nest icon with an image when my nest detects movement. 
