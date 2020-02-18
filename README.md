# Librarian

Librarian is intended to run in the background e.g. on a server,
where it will frequently query the Mojang servers to see if any new versions of the game are available.
If so, it will download the main game files and optionally execute commands in your systems shell
to trigger further actions.

**This project uses .NET Core 3.1**

To use the [binaries](https://github.com/Wubbi/Librarian/releases) make sure you have at least the [.Net Core Runtime](https://dotnet.microsoft.com/download) installed.  
Afterwards you can start the program with `dotnet Librarian`.  
The Windows binaries contain an executable you can use, though that still requires the runtime to be installed.

**If you've used the old Librarian, you can reuse its library!**  
See the `MigrateOldLibrarianData` setting below for details.

## Settings

When starting, Librarian looks for a "settings.json" next to the executable which you can use to override the default settings.
Here is an example of all available settings with their default values:
```JSONiq
{
	"LibraryRoot":"./Library",
	"Log":true,
	"ValidateLibraryOnStartup":false,
	"Interval":1140,
	"SkipJars":false,
	"MigrateOldLibrarianData":"",
	"AddedVersionCommand":"",
	"NewestOnly":true,
	"UiInputInterval":80,
	"UiRenderReduction":3
}
```
Additionally you can use commandline arguments to override those settings again.  
``Defaults < Settings file < Commandline Arguments``  
 You'll find the arguments below.  


**Let's have a closer look**
---

```JSONiq
"LibraryRoot":"./Library"
-l "./Library"
```
This is the location of the library Librarian maintains. All files will be downloaded into its structure,
organized by `<type>/<name>/<release_date>`.  
Furthermore, this is the place where the logfile will be written.

---

```JSONiq
"Log":true
-n true
```
Whether or not to log events. Logs will be written in "log.txt" next to the executable.  

---

```JSONiq
"ValidateLibraryOnStartup":false
-v false
```
When starting, Librarian will check which versions are already in the Library by searching for folders with meta.json files in them.  
When this settings is `true`, a second check will be performed, which looks for missing .jar files and compares the size and SHA1 hash of exisiting jars against the manifest.

---

```JSONiq
"Interval":1140
-i 1140
```
This is the time in seconds between two queries to the Mojang server, so by default Librarian looks for new versions every 19 minutes.  
While it is in seconds, remember that updates do not happen all to often. A refresh every few hours might be all you need.

---

```JSONiq
"SkipJars":false
-s false
```
By default, Librarian will download the jar files of every version not already in the library.  
Using this setting you can limit the download to the meta.json file.  
Please note that this does not affect downloads triggered by `ValidateLibraryOnStartup`.

---

```JSONiq
"MigrateOldLibrarianData":"example/path/OldLibrary"
-migrate "example/path/OldLibrary"
```
This setting is meant to be used only once, before you start using Librarian effectively.  
If you've used the old Librarian, you need this to migrate the old structure to the new format.
When Librarian starts, all files from the old Library in the given path are copied into the new location
(overwriting existing files). Librarian closes itself right after migration is complete.

---

```JSONiq
"AddedVersionCommand":""
-c ""
```
This is where you can set a command to execute when a new version of the game was detected and downloaded.  
Every occurence of `_AddedVersionPath_` in the command will get replaced by the path of the added game.  
When several versions are downloaded, the command is executed for each of them in order of their release date.

---

```JSONiq
"NewestOnly":true
-e true
```
If this value is `true`, and several versions are downloded, the command in `AddedVersionCommand`
will only be executed for the newest of them instead of all.

---

```JSONiq
"UiInputInterval":80
"UiRenderReduction":3
-ui 80 3
```
These two settings change the rate in which Librarian interacts with the console.  
`UiInputInterval` is the delay in milliseconds inbetween checking for new inputs from the user.
Currently this only refers to the Ctrl+C combination to shut down Librarian.  
`UiRenderReduction` is a counter that reduces the amount of refreshs of the console output based on the input scan interval.
In the above example the output is refreshed once on every third (`3`) scan for inputs (once every ~240ms).  
Reduce these values as you need. Smaller delays can cause flickering.

---