# FtApp
With this app you can control the fischertechnik ROBOTICS TXT Controller and ROBO TX Controller.
It allows you to control the output ports and read the input ports.
Moreover you can view the camera stream of the TXT Controller.  
The communication part is written in C# and Xamarin and does not depend on any Android specific apis.
You can use it to write a separate desktop application or an app for Windows Phone or iOS.  
If you want to write an own application using this code, you can look at the InterfaceTest project.
It shows how to connect to an interface and how to configure and read the I/O.

## Installation
This app is listed in the [Google Play Store](https://play.google.com/store/apps/details?id=de.bennik2000.ftapp). 
You can install it directly from there.
If you want to install an older version you can download the files from the [Releases page](https://github.com/Bennik2000/FtApp/releases).
The `.apk` and the related souce code of every release is listed there.

Note: Your Android version must be *Android 4.0.3 (Ice Cream Sandwich)* 
or higher.

## Testing
Tested on these devices:  

| Device                    |Android Version| Result|
|---------------------------|---------------|------ |
| Samsung Galaxy J5         |Android 5.1    |Working|
| Samsung Galaxy S4 mini    |Android 4.4    |Working|
| Samsung Galaxy S3 mini    |Android 4.1.2  |Working|
| Samsung Galaxy S2         |Android 4.2    |Working|


Feel free to test on more devices and report your result!


### Screenshots
The screen to select a fischertechnik interface:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_SelectDevice.png" height="500">  
  
Here you can view the input values:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_Inputs.png" height="500">  
  
You are able to set the output values here:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_Outputs.png" height="500">  
  
To control a car it is easier to use a joystick:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/development/FtApp/FtApp.Droid/Screenshots/Screenshot_Joystick.png" width="500">  
  
When you are connected to an ROBOTICS TXT Controller you can view the camera stream here:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_Camera.png" height="500">  

## Application architecture
The app is divided into two parts. The first part is responsible for the communication to the
interface and the second part is the Android UI.  

#### Communication part
There are a few classes which are necessary to connect to an interface:  

* `IFtInterface` is the main interface to control a fischertechnik interface.   
* `TxtInterface` is the implementation for the ROBOTICS TXT Controller protocol.  
* `TxInterface` is the implementation for the ROBO TX Controller protocol.  
* `SimulatedFtInterface` is a simulation of an interface.

#### Android UI
The android app is a simple unser interface for `IFtInterface`.

## TODO
* Adapt for tablets
* Upload and play audio files to TXT Controller
* Multiple extensions
* Overal status page (connection status, firmware version, ...)
* Testing on real Android devices
* Add translations for more languages?
* Add Windows Phone and iOS support? (I can't do this because i do not have a WP or Mac)

## Contributing
If you want to work on this app you have to install Visual Studio 2015 Community Edition
with the Xamarin package.  

* If you have an idea for an awesome new feature let me know!
* Please report any bugs which you can find (Use the *Issues* tab and create a new issue).
* You own a cell phone which is not listed in the list above: Test the app and report the result
* You can try to work on the things which are listed in the TODO list. (Checkout the actual code 
  from the development branch and create a new branch with the name of the feature)  

Note: When you want to contribute you have to follow the coding style of the other code files!  
In order to help you have to fork this project and create a pull request. It is 
desctibed [here](https://help.github.com/articles/using-pull-requests/).


## History
#### Version 1.2
* Added two joysticks
* Rotating the screen is now possible
* Added rating reminder

#### Version 1.1.2
* Fixed display bug where no I/O elements were displayed when not paired before

#### Version 1.1.1
* Fixed wrong build

#### Version 1.1
* Added help page

#### Version 1.0
* First working release version of the app
* Connect to ROBOTICS TXT Controller
* Connect to ROBO TX Controller
* Read the sensor values of each input (TX / TXT)
* Set the output values of each output port (TX / TXT)
* View the camera stream of a connected camera (TXT)
* Simulate an interface


## License

####The MIT License (MIT)

Copyright (C) `2016` `Bennik2000`

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the 'Software'), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

