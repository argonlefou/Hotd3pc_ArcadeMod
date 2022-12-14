# Hotd3pc ArcadeMod

This mod is a launcher that will start the windows game and modify the memory in the fly to make it look alike the original arcade version.
Amongst other things :
- Credits handling
- Configuration tool (game options and SERVICE MENU options)
- Removed game menus
- Few graphics enhancements (Title Screen, arcade logo)
- Portable game (no more Registry access or AppData saved files)
- etc..

The mod is known to be fully compatible with the following version of hod3pc.exe files ;

| `hod3pc.exe` MD5 Checksum            | Game Version       | 
| -----------------------------------  |--------------------|
| 4bf19dcb7f0182596d93f038189f2301     | SEGA Windows|
| 3a4501d39bbb7271712421fb992ad37b     | RELOADED cracked|
| b8af47f16d5e43cddad8df0a6fdb46f5     | REVELATION No-CD|
| 0228818e9412fc218fcd24bfd829a5a0     | MYTH Release|
| 733da4e3cfe24b015e5f795811d481e6     | Unknown Release #1|
| 51dd72f83c0de5b27c0358ad11e2a036     | Unknown Release #2|
| e4819dcf2105b85a7e7bc9dc66159f5c     | Unknown Release #3|  

<br>

## Game Installation

Just install the game from the CD-ROM / ISO or just unzip the game folder files.
Installation from the installer is not mandatory, as no registry key is needed anymore.

If you already have the game installed, this mod will run on its own without modifying any existing files.<br>
i.e : you can run again the genuine computer game by running the `hod3pc.exe` file directly (or the original game launcher).
<br><br>

## Mod Installation

Simply unzip the release zip somewhere.
<br><br>

## Mod Configuration

* From the previously unzipped folder, run the `Hotd3Arcade_Config.exe` tool.
* Set the path to your `hod3pc.exe` file.
* You can also change ARCADE settings, as well as most of the settings available in the genuine game launcher utility.
* Save once you're done.

The mod is not altering controls in any way.  
You can safely choose to play with `mouse` or `keyboard` option. `Gamepad` buttons are not yet configured but can be changed by editing the config file manually.  
For any advanced needs (multiplayer, lightguns, gamepad, etc....) I highly suggest to use [DemulShooter](https://github.com/argonlefou/DemulShooter)
<br><br>

## Play

Run `Hotd3Arcade_Launcher.exe`.  
The launcher will start the game and close itself, letting the game run on its own.
<br><br>

## Default controls 

Common controls :
| Key            | Action       | 
| -------------  |--------------|
| <kbd>1</kbd>    | P1 Start|
| <kbd>2</kbd>     | P2 Start|
| <kbd>5</kbd>     | Coins|  
| <kbd>ESC</kbd>   | Exit game|  
| <kbd>&uarr;</kbd><kbd>&darr;</kbd><kbd>&larr;</kbd><kbd>&rarr;</kbd>| Navigation (menu, path selection)
<br>  

`mouse` specific controls : 
| Key            | Action       | 
| -------------  |--------------|
| <kbd>Left CLick</kbd></kbd>| P1 Trigger|
| <kbd>Right CLick</kbd>     | P1 Reload|
<br>  

`keyboard`specific controls :
| Key            | Action       | 
| -------------  |--------------|
| <kbd>&uarr;</kbd><kbd>&darr;</kbd><kbd>&larr;</kbd><kbd>&rarr;</kbd>| P1 Aiming|
| <kbd>LCTRL</kbd>|P1 Trigger|
| <kbd>LSHIFT</kbd>|P1 Reload|
| <kbd>NumPad8</kbd><kbd>NumPad2</kbd><kbd>NumPad4</kbd><kbd>NumPad6</kbd>| P2 Aiming|
| <kbd>RCTRL</kbd>|P2 Trigger|
| <kbd>RSHIFT</kbd>|P2 Reload|

Keyboard keys and gamepad buttons can be manually changed in the launcher config file `Hod3Arcade.ini`.  
