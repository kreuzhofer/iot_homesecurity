# Setup
For setting up a fresh device, you need to copy all the files of this folder to a folder on your device, lets call it c:\deploy and log in onto the device with ssh.

Then execute the following commands:

```
cd c:\deploy
.\setup_iot
```

setup_iot.cmd makes sure that the IoT app can execute the limpet.exe for getting the IoT Hub access keys from the TPM chip.
It also sets up a scheduled task, that checks every minute that the app runs.

Do not use the scheduled task when you like to deploy and debug your app with Visual Studio as this might interfere with stopping and starting the app during the VS deployment. It could cause your deployment to fail because the scheduled task could try to launch the app while VS tries to deploy it.
If you already have set up the scheduled task and want to do debugging with VS, just run "remove_iot_task.cmd", which will remove the scheduled task but not the registry entry for allowing limpet.exe to be executed by the IoT app.