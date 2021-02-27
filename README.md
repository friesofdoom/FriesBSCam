
# FriesBSCam
Beat saber camera for Liv made by fries.\
This plugin will smoothly and randomly blend between any number of pre-defined cameras, both static and orbital.

Feel free to use and/or modify this plugin how ever you want. No credit required. But if you do want to thank me, drop a link to my YouTube channel somewhere in your video description - https://www.youtube.com/channel/UCrQWyvE44R5G6_Ho88OfQYA

Comments and feature requests are welcome.

# Camera Demo
Here you can see a quick demo of the camera plugin in action.\
[![Camera Demo](https://img.youtube.com/vi/YoIeM9ckE90/0.jpg)](https://www.youtube.com/watch?v=YoIeM9ckE90) \
[![Camera Demo](https://img.youtube.com/vi/g-GKGbQPh2k/0.jpg)](https://www.youtube.com/watch?v=g-GKGbQPh2k)

# Installation Instructions 
Download the DLL from the assets in the latest release - https://github.com/friesofdoom/FriesBSCam/releases/ \
Copy the dll file into:\
    <...>\Documents\LIV\Plugins\CameraBehaviours\\\
After you use the camera for the first time, it will create a settings file in:\
    <...>\Documents\LIV\Plugins\CameraBehaviours\FriesBSCam\\\
You can then modify the settings to suit your purpose.

# Use
Select your camera in LIV. Change it to plugin camera. Select FriesBSCam at the bottom.

# Settings
GlobalBias='0.0' 
This rotates the camera around the y-axis in oder to move the avatar slightly to the left or right
This is used if you have something like a first person view on the left or the right of your video feed, then you will want to move the avatar in the opposite direction slightly. I use a value of -15.0 to move my avatar slightly to the right.

Debug='False' or 'True'\
Optional Parameter that will output debug logs from BeatSaberStatus (HTTPStatus)

MenuCamera= { ... }\
Optional MenuCamera can be setup when using HTTPStatus. Uses the same properties as a normal Camera (below).

Camera={ ... }\
This creates a new camera with the following properties:

Name='CameraName'\
The name of the camera.

Type='LookAt' or 'Orbital'\
The type of camera.

Distance='3.0'\
Distance from the avatar for orbital camera only

Speed='1.0'\
Orbital camera speed

PositionBinding='playerWaist' or 'playerRightHand' or 'playerLeftHand' or 'playerRightFoot' or 'playerLeftFoot' or 'playerHead'\
The bone on the avatar to bind the camera position to.

PositionOffset={x='0.0', y='0.0', z='0.0'} \
The position offset from the binding bone.

LookAtBinding='playerWaist' or 'playerRightHand' or 'playerLeftHand' or 'playerRightFoot' or 'playerLeftFoot' or 'playerHead'\
Look-At bone binding target if the camera is an Orbital Type.

LookAt={x='0.0', y='-0.5', z='1.0'} \
Look-At direction if the camera is a LookAt camera Type\
Look-At bone binding offset of the camera is an Obrital Type.

MinTime='4.0' \
Minimum time that this camera is active for

MaxTime='8.0' \
Maximum time that this camera is active for

ActualTime='0.0' \
(Optional) Used with song-specific settings files to give more control over timing of changing to other cameras (see below)

TransitionTime='0.0' \
(Unused) Here for future support of camera-specific blend speed changes
	

# (Optional) HTTPStatus Plugin Support
If you have the [HTTPStatus](https://github.com/opl-/beatsaber-http-status) Plugin installed the following features are enabled:

    - Static Menu Camera automatically when in Beat Saber's menu, automatically switching to dynamic cameras on song start
    - Restarting the song selects a new camera (great if you don't like the camera that it chose at the start)
    - Optional Song-Specific settings files (see below)

# (Optional) Song Specific Settings Files
If you are using [HTTPStatus](https://github.com/opl-/beatsaber-http-status) on song load we check to see if there is a song-specific settings file.\
It searches in the same settings directory `<...>\Documents\LIV\Plugins\CameraBehaviours\FriesBSCam\` for a `settings.[songhash].txt` file.\
If it finds said file, it will clear out the current dynamic cameras for the cameras and build a new linear list of cameras using ActualTime.

ActualTime allows you to specify how long to stay on a specific camera before it moves onto the next, giving you greater control of movement.\
(Note, the first timer does not start until score changes - this is to allow different PCs to load up the song properly to sync multiple players.)\
You can find the songhash in the output.txt file that's generated while in game and a song is loaded.

### Song Specific Demo
[![Song Specific Demo](https://img.youtube.com/vi/BTwlqemL8Ak/0.jpg)](https://www.youtube.com/watch?v=BTwlqemL8Ak)

### Configuring Song Specific files
Example in output.txt as songs load:

```
Transitioning to inGame
Song Name: look at the sky
Song SubName: 
Song AuthorName: porter robinson
Level Author: Reaxt & CyanSnow
Song Hash: 825DBD980EADCEABA54C8E9D8E68F93A1B4CB029
Level ID: custom_level_825DBD980EADCEABA54C8E9D8E68F93A1B4CB029
Checking to see if there are song-specific Settings
No song specific settings file found

Transitioning to inGame
Song Name: Darkside
Song SubName: (ft. Au/Ra & Tomine Harket)
Song AuthorName: Alan Walker
Level Author: Liam, Riz & KyleT
Song Hash: 713E301FC4F774EDF4EA1001A19DD5BF7E3F4CE6
Level ID: custom_level_713E301FC4F774EDF4EA1001A19DD5BF7E3F4CE6
Checking to see if there are song-specific Settings
Song specific settings found!
New Camera Count: 26
```

In this example, there is a `settings.713E301FC4F774EDF4EA1001A19DD5BF7E3F4CE6.txt` found in `<...>\Documents\LIV\Plugins\CameraBehaviours\FriesBSCam\`

Just the first few contents of the file, it's very similar to the normal settings file that's generated:

```
GlobalBias='0.0'
Camera={
	Name='MenuCam'
	Type='LookAt'
	PositionBinding='playerWaist'
	PositionOffset={x='2.0', y='1.0', z='-3.0'} 
	LookAt={x='-2.0', y='0.0', z='5.0'} 	
	ActualTime='3.0' 
	TransitionTime='0.0' 
}
Camera={
	Name='DiagonalFrontLeft'
	Type='LookAt'
	PositionBinding='playerWaist'
	PositionOffset={x='-2.0', y='1.0', z='4.2'} 
	LookAt={x='2.0', y='-0.5', z='-4.0'} 
	ActualTime='2.83' 
	TransitionTime='0.0' 
}
Camera={
	Name='Orbital1'
	Type='Orbital'
	Distance='3.0'
	Speed='1.0'
	PositionBinding='playerWaist'
	PositionOffset={x='0.0', y='0.5', z='0.0'}  
	LookAtBinding='playerWaist'
	LookAt={x='0.0', y='0.0', z='0.0'} 
	ActualTime='11.0' 
	TransitionTime='0.0' 
}
Camera={
	Name='Top'
	Type='LookAt'
	PositionBinding='playerWaist'
	PositionOffset={x='0.0', y='2.0', z='-2.0'}  
	LookAt={x='0.0', y='-0.5', z='1.0'} 
	ActualTime='6.0' 
	TransitionTime='0.0' 
}
...
```
