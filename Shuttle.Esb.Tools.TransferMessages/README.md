# Shuttle.Esb.Tools.TransferMessages

Transfers ***all*** messages from the source queue to the destination queue:

```
Shuttle.Esb.Tools.TransferMessages.exe 
	/[source|s]={queueUri} 
	/[destination|d]={queueUri}
	/clear
	/copy
	/[count|c]={count}
	/[quiet|q]
```

If an argument is omitted you will be prompted to enter it.

| Argument | Shortened | Description |
| --- | --- | --- |
| `source` | `s` | The queue uri where you are transferring the messages ***from***. |
| `destination` | `d` | The queue uri where you are transferring the messages ***to***. |
| `clear` | `clear` | Clears the `FailureMessage` collection. |
| `copy` | `copy` | Copies all the messages, leaving the original. |
| `count` | `c` | Transfers the given number of messages or all when 0.  Default is 0. |
| `quiet` | `q` | Quiet mode.  You will not receive any prompts. |


