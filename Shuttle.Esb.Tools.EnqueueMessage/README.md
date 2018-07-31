# Shuttle.Esb.Tools.EnqueueMessage

Enqueues the contents of a file as a Shuttle.Esb message into a queue:

```
Shuttle.Esb.Tools.EnqueueMessage.exe 
	/[destination|d]={queueUri}
	/[path|p]={file-path}
	/[quiet|q]
```

If an argument is omitted you will be prompted to enter it.

| Argument | Shortened | Description |
| --- | --- | --- |
| `destination` | `d` | The queue uri where you are enqueuing the message ***to***. |
| `path` | `p` | The path to the file containing the message in a deserialized format.  The default is 'message.esb'. |
| `quiet` | `q` | Quiet mode.  You will not receive any prompts. |


