# FtApp
With this app you can control the fischertechnik ROBOTICS TXT Controller and ROBO TX Controller.
It allows you to control the output ports and read the input ports.
Moreover you can view the camera stream of the TXT Controller.  
The communication part is written in C# and Xamarin and does not depend on any Android specific apis.
You can use it to write a separate desktop application or an app for Windows Phone or iOS.

### Screenshots
The screen to select a fischertechnik interface:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_SelectDevice.png" height="300">  
Here you can view the input values:  
  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_Inputs.png" height="300">  
  
You are able to set the output vlues here:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_Outputs.png" height="300">  
  
When you are connected to an ROBOTICS TXT Controller you can see view the camera stream here:  
<img src="https://raw.githubusercontent.com/Bennik2000/FtApp/master/FtApp/FtApp.Droid/Screenshots/Screenshot_Camera.png" height="300">  

## Application architecture
The app is divided into two parts. The first part is responsible for the communication to the
interface and the second part is the Android UI.  

#### Communication part
`IFtInterface` is the main interface to control a fischertechnik interface.   
`TxtInterface` is the implementation for the ROBOTICS TXT Controller protocol.  
`TxInterface` is the implementation for the ROBO TX Controller protocol.

## Installation
This app is not listed in the play store. You have to download the `de.bennik2000.ftapp.apk` file
and install it manually on your android phone. Note your Android version must be 
Android 4.0.3 (Ice Cream Sandwich) or higher.

## TODO
* Upload and play audio file
* Multiple extensions
* Overal status page
* Testing on real Android devices
* Simulation option?
* Add translations for more languages?
* Add Windows Phone and iOS support? (I can't do this because i do not have a WP or Mac)

## Contributing
If you want to work on this app you have to install Visual Studio 2015 Community Edition
with the Xamarin package.  

* Please report any bugs which you can find.
* If you have an idea please let me know.
* You can try to work on the things which are listed in the TODO list

## License

####The MIT License (MIT)
---------------------

Copyright © `2016` `Bennik2000`

Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the “Software”), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.

