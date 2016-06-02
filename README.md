# Kinect 2 Server
C# server streaming features of the Kinect 2 such as speech &amp; gesture recognition, skeleton tracking and original images

First, be sure that you have Windows 8 or later versions otherwise you won't be able to install the SDK of Kinect2.

You have to download and install these following features and SDKs :

Kinect 2 SDK :
https://www.microsoft.com/en-us/download/details.aspx?id=44561

Microsoft Speech platform - SDK (Version 11) :
https://www.microsoft.com/en-us/download/details.aspx?id=27226

Microsoft Speech Platform - Runtime (Version 11) :
Note : Get both x86 and x64 versions if you're running 64bits processor
https://www.microsoft.com/en-us/download/details.aspx?id=27225

Language Pack :
Chose languages that you want to set for the Speech Recognition
https://www.microsoft.com/en-us/download/details.aspx?id=43662

Media feature pack for N and KN version of Windows 8 :
https://www.microsoft.com/en-us/download/details.aspx?id=30685

Media feature pack for N and KN version of Windows 10 :
https://www.microsoft.com/en-us/download/details.aspx?id=48231

## Documentation

### Presentation
The server written in C# uses the Kinect SDK v2 to get the RGBD raw image, skeleton tracking information, recognized speech. It also uses the text-to-speech from Microsoft.
Then it streams JSON data over the network using the Publisher/Subscriber pattern from the ZeroMQ network library.
A Linux client has been written in Python but it can be written in any other language that is compatible with ZeroMQ. Features are controllable through a Graphical User Interface on Windows, or through the code from any Linux/Windows client. The clients can for instance enable features (speech recognition on, skeleton tracking off, …) and parameters (set new speech to recognize, change language, …) from remote.

### Features
#### Speech recognition
The speech recognition allows to recognize words, tree of words that is defined in an [XML grammar file](https://msdn.microsoft.com/en-us/library/office/hh361594%28v=office.14%29.aspx) that is passed as a parameter. When a speech is recognized, the application displays the sentence and/or the semantics depending of the requested output. A semantic value contains information that matches a path through the grammar file and that is more easily usable than the text itself. The application allows to enable or disable the display of those values.
Example of grammar rules:
```XML
<rule id="playAction">
    <one-of>
      <item> play </item>
      <item> start </item>
      <item> begin </item>
    </one-of>
  </rule>
<rule id="fileWords">
    <one-of>
      <item> song </item>
      <item> tune </item>
      <item> track </item>
      <item> item </item>
    </one-of>
  </rule>
```
Here there is two rules, one that defines the play action and the other that defines the word following this action. If we say “play the song” or “start the tune” or “begin the item” the recognized text will be different but the semantic will be the same “play/song”.

How to use client for speech recognition:
```Python
from kinect2.client import Kinect2Client
kinect = Kinect2Client("yourIP")
def callback_speech(msg):
    print msg
kinect.speech.set_callback(callback_speech)
grammar = '''<grammar version="1.0" xml:lang="en-US" root="rootRule"
                      xmlns="http://www.w3.org/2001/06/grammar">
                <rule id="rootRule">
                    <one-of>
                        <item> Hello </item>
                        <item> Bye </item>
                    </one-of>
                </rule>
             </grammar>'''
kinect.speech.params.set_grammar(grammar, "hello_grammar")
kinect.speech.params.sentence_on()
kinect.speech.start()
```
The confidence threshold can be changed (from 0.1 to 1.0).

troubleshooting :
If you can't load the xml file, try to remove comments and the first line that defines the xml file
