# Librarian

Librarian is intended to run in the background e.g. on a server,
where it will frequently query the Mojang servers to see if any new versions of the game are available.
If so, it will donwload the main game files and optionally execute commands in your systems shell
to trigger further actions, like sending a message.

**This project uses .NET Core 2.1**

To use the [binaries](https://github.com/Wubbi/Librarian/releases) make sure you have at least the [.Net Core Runtime](https://dotnet.microsoft.com/download) installed.  
Afterwards you can start the program with `dotnet Librarian.dll`.

## Settings

To configure Librarian you can use a textfile containing the required settings in JSON format.
Here is an example of what this file might look like:

```JSONiq
{
	refreshRate:29,
	libraryPath:"G:\\Library",
	addMissingVersions:true,
	checkjarFiles:false,
	tasks:
	[
		{
			beforeDownload:true,
			dependentOnIds:[],
			type:"Snapshot",
			triggerTypes:["Added","Changed"],
			commands:
			[
				"msg \"%username%\" Snapshot version $id is out!",
				"msg \"%username%\" It will be stored in $path"
			],
			params:
			{
				id:"$id",
				path:"$path"
			}
		},
		{
			beforeDownload:false,
			dependentOnIds:[0],
			type:"Snapshot",
			triggerTypes:["Added"],
			commands:
			[
				"msg \"%username%\" It was stored in $path"
			],
			params:
			{
				path:"$path"
			}
		}
	]
}
```

**Let's have a closer look**
---

```JSONiq
refreshRate:29
```
This is the time in seconds between two queries to the Mojang server, so in our example the Librarian looks for new versions every 29 seconds.
While it is in seconds, remember that updates do not happen all to often. A refresh every few hours might be all you need.

---

```JSONiq
libraryPath:"G:\\Library"
```
This is the location of the library the Librarian maintains. All files will be donwloaded into its structure,
organized by `<type>/<name>/<release_date>`. Furthermore, this is the place where the logfile will be written.

---

```JSONiq
addMissingVersions:true
```
Depending on your use case, you might not have the Librarian running whenever a new version comes out, but still want
the tasks to be executed for the updates you've missed. When this flag is set to true Librarian will start by comparing the last manifest it knows to the live one and processes it like an update.

---

```JSONiq
checkjarFiles:true
```
The main purpose of this flag is to make Librarian check whether or not the server/client .jar files are present in the library. 
By default only the existence of the metadata file is checked.  
As a secondary function, setting this flag to false will prevent Librarian to dowload .jar files when it builds its library 
for the very first time (which at the time of this writing prevents ~8 GB of data from beeing downloaded).

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
dependentOnIds:[]
```
A list of tasks that need to be executed succesfully (filters applied, all commands succeeded) before this one can.
The Ids used here are simply the indices ot the task in the tasks:[] array (starting at 0).

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
triggerTypes:[]
```
A list of types of update this task is triggered by. Currently available:
- `Latest` - React to a change of what is considered to be the latest version of the type specified by `type`
- `Added` - React to versions that were not available before
- `Changed` - React to versions that were available before, but have undergone some change
- `Removed` - React to versions that are not available anymore

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


## Commandline arguments

When starting Librarian you can specifiy the following arguments:
- `-s <settings_file>` - This allows you to specify a textfile to read settings from
- `--o` - This turns of the standard output. By default the console displays entries made to the log

