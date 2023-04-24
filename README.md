#  Zomboid Modlist Manager

Zomboid Modlist Manager is a C# Windows Forms application that allows you to manage and organize mods for **Project Zomboid**. It parses mod information from the Steam Workshop, allowing you to add, remove, and rearrange mods easily. The application also provides the ability to read and write mod information to and from a custom mods.txt file.

![enter image description here](https://raw.githubusercontent.com/laci507/Zomboid-Modlist-Manager/main/index.png)

## Features

 - Add and remove mods using Steam Workshop URLs or numeric mod IDs
 - Rearrange the order of mods in the list
 - Display mod details,  including name, map folders, and description
 - Read and write mod information to and from a custom mods.txt file 
 - Open mod page on Steam Workshop when selecting item from list

## Usage

Run the application. 
Add a mod to the list by entering its Steam Workshop URL or numeric mod ID in the text box and clicking the "Add Mod" button.
To rearrange the order of mods in the list, select a mod and click the "Move Up" or "Move Down" button.
To remove a mod from the list, select it and click the "Remove Mod" button.
To write the mod information to a mods.txt file, click the "Write to File" button.
To read mod information from a mods.txt file, click the "Read from File" button.
Double-click a mod in the list to open its Steam Workshop page in your web browser.

## Limitations

 - Mod dependencies are not yet parsed.
 - Currently writing mods data to a server config file is not supported.
   Generate the mods.txt and copy the necessary contents to your config.
  - Multiple 'ModID' from a description is not parsed, only the first entry - this is to prevent stacking ModIDs, that remove functionality of the original mod.
  - Always review all the mods description for details

## Dependencies
[HtmlAgilityPack](https://html-agility-pack.net/)
.NET 4.7.2

Feel free to improve the code any way you want, pull requests are welcome.
