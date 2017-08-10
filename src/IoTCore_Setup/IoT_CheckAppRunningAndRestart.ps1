try
{
    $result = Invoke-WebRequest -Uri "http://localhost/" -TimeoutSec 10
    $result.StatusCode
}
catch
{
    & "c:\windows\system32\iotstartup" run 30128DanielKreuzhofer.IoTHomesecurity_g35d80h1yseyw!App
}
