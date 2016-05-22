# FtApp
With this app you can control the fischertechnik ROBOTICS TXT Controller.
It allows you to control the output ports and read the input ports.
Moreover you can view the camera stream.  
The communication part is written in C# and Xamarin and does not depend on any Android specific apis.
You can use it to write a separate desktop application or an app for Windows Phone or iOS.

## Application architecture
The app is divided into two parts. The first part is responsible for the communication to the
interface and the second part is the Android UI.  

#### Communication part
`FtInterface` is the main class to control a fischertechnik interface. `TxtInterface` is 
derived from it and provides the ROBOTICS TXT Controller protocol implementation.

## Installation
This app is not listed in the play store. You have to download the `de.bennik2000.ftapp.apk` file
and install it manually on your android phone. Note your Android version must be 
Android 4.0.3 (Ice Cream Sandwich) or higher.

## TODO
* Add ROBO TX Controller protocol implementation
* Add German translation (or any other :D)
* Testing on real Android devices
* Add Windows Phone and iOS support (I can't do this because i do not have a WP or Mac)

## Contributing
* Please report any bugs which you can find. 
* If you have an idea please let me know.
* You can try to work on the things which are listed in the TODO list

## License

The MIT License (MIT)
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

