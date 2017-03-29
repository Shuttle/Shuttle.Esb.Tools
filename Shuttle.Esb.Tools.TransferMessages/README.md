# Shuttle.Esb.Tools.TransferMessages

Transfers ***all*** messages from the source queue to the destination queue:

```
Shuttle.Esb.Tools.TransferMessages.exe /source:{queueUri} /destination:{queueUri}
```

The `source` argument may be shortened to `s` and the `destination` argument may be shortened to `d`.

If an argument is omitted you will be prompted to enter it.