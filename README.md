# FriesBSCam
Beat saber camera for Liv made by fries.\
This plugin will smoothly blend between any number of pre-defined cameras, both static and orbital.

Feel free to use and/or modify this plugin how ever you want. No credit required. But if you do want to thank me, drop a link to my YouTube channel somewhere in your video description - https://www.youtube.com/channel/UCrQWyvE44R5G6_Ho88OfQYA

# Installation Instructions 
Download the DLL from the assets in the latest release - https://github.com/friesofdoom/FriesBSCam/releases/\
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
	
	
	
