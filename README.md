# Librarian

Librarian is intended to run in the background e.g. on a server,
where it will frequently query the Mojang servers to see if any new versions of the game are available.
If so, it will donwload the main game files and optionally execute commands in your systems shell
to trigger further actions, like sending a message.

## Settings

To configure Librarian you can use a textfile containing the required settings in JSON format.
Here is an example of what this file might look like:

```JSONiq
{
	refreshRate:30,
	libraryPath:"G:/Library",
	addMissingVersions:true,
	tasks:
	[
		{
			beforeDownload:true,
			dependedOnIds:[],
			type:"Snapshot",
			onLatest:false,
			onAdded:true,
			onChanged:true,
			onRemoved:false,
			commands:
			[
				"echo Snapshot version $id is out!",
				"echo It will be stored in $path"
			],
			params:
			{
				id:"$id",
				path:"$path"
			}
		},
		{
			beforeDownload:false,
			dependedOnIds:[],
			type:"Snapshot",
			onLatest:false,
			onAdded:true,
			onChanged:false,
			onRemoved:false,
			commands:
			[
				"echo It was stored in _"
			],
			params:
			{
				path:"_"
			}
		}
	]
}
```

**Let's have a closer look**
---

```JSONiq
refreshRate:30
```
This is the time in seconds between two queries to the Mojang server, so in our example the Librarian looks for new versions every 30 seconds.
While it is in seconds, remember that updates do not happen all to often. A refresh every few hours might be all you need.

---

```JSONiq
libraryPath:"G:/Library"
```
This is the location of the library the Librarian maintains. All files will be donwloaded into its structure,
organized by `<type>/<name>/<release_date>`. Furthermore, this is the place where the logfile will be written.

---

```JSONiq
addMissingVersions:true
```
Depending on your use case, you might not have the Librarian running whenever a new version comes out, but still want
the library to be complete. Setting this value to true will force the Librarian to update the library before starting its
usual routine. These library updates do NOT trigger any of the tasks! 

As a sidenote: Librarian considers a version to be in the library if its folders exist (down to the <release_date> one),
not whether or not any .jar files are present in it. This way you can freely move them somewhere else into your own archive structure.

---

```JSONiq
tasks:[...]
```
As mentioned above, Librarian can execute shell commands whenever an update occurs. They are combined with filters,
that limit when they should be run, and organized as task objects in this array.

## Tasks

Simply said, a task consists out of a set of filters and a list of shell commands. Whenever the Librarian detects an update, it
runs each task on each new version it finds. If the taks filters apply, the commands are executed.

---

```JSONiq
beforeDownload:true
```
Determines whether this task is to be run before or after Librarian downloads the .jar files.

---

```JSONiq
dependedOnIds:[]
```
A list of tasks that need to be executed succesfully (filters applied, all commands succeeded) before this one can.
The Ids uses here are simply the indices ot the task in the tasks:[] array (starting at 0).

Attention: Tasks before the download can (understandably) not depend on tasks after.
But neither can those after the download depend on those before!

---

```JSONiq
type:"Snapshot"
```
Filters which type of release this task reacts to. Can only be one of these:
- `Snapshot` - Unstable dev builds
- `Release` - Official, stable release of the game
- `Alpha` - The oldest version available. Unlikely to ever be part of an update
- `Beta` - Same as above, unlikely to be used ever again

---

```JSONiq
onLatest:false
```
If this flag is set, the task only reacts to a change of the latest (=newest) release (of the type specified with `type`) 
and will ignore all other versions that might have ben added since the last refresh.

---

```JSONiq
onAdded:true
```
If set, this task will be run against all versions added since the last query of the Mojang server. Ignored if `onLatest` is set.

---

```JSONiq
onChanged:true
```
If set, this task will be run against all versions that existed before, but have received an update since. Ignored if `onLatest` is set.

---

```JSONiq
onRemoved:true
```
If set, this task will be run against all versions that existed before, but not anymore. Ignored if `onLatest` is set.

---

```JSONiq
commands:[...]
```
A simple list of shell commands that are to be executed in the order they are written if all above filters apply.

---

```JSONiq
params:{...}
```
These are necessary to make the comands more dynamic. They are pairs of a key, that links it to a hardcoded function,
and a value, that is used as a variable in the commands. Currently only two parameters are available:
- `id` - The "name" of the version beeing processed
- `path` - The path in which the files are (going to be) stored (filenames are ALWAYS client.jar and server.jar)


## Commandline parameters

RunLibrarian accepts up to two parameters:

The first one is the file containing the required settings.

The second parameter is the flag `-output`, which will prevent the console from displaying any log entries.

