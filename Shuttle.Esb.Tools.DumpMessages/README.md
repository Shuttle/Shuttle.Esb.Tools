# Shuttle.Esb.Tools.DumpMessages

Dumps messages from the given queue to the destination folder:

```
Shuttle.Esb.Tools.DumpMessages.exe 
	/[source|s]={queueUri} 
	/[folder|f]={path-to-folder}
	/[count|c]={count}
	/[quiet|q]
```

Messages will be released back onto the queue.  This means that if you dump more messages than are in the queue it will loop around.  The `MessageId` is used as the file name and will be overwritten if it exists.

If a required argument is omitted you will be prompted to enter it.

| Argument | Shortened | Description |
| --- | --- | --- |
| `source` | `s` | The queue uri where you are dumping the messages from. |
| `folder` | `f` | The folder where the output messages will be created.  Default is '.\messages'. |
| `count` | `c` | Dumps the given number of messages.  Default is 30. |
| `quiet` | `q` | Quiet mode.  You will not receive any prompts. |


