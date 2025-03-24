# net45.NLog.Extensions.Logging



## Usage

```csharp
builder.Logging.AddNLog();
```



## NLog.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <targets>
    <target xsi:type="File" name="logfile" fileName="${basedir}/logs/${shortdate}.all.log"
            layout="[${time}] [${level:uppercase=true}] [${logger}] ${message}"
            archiveAboveSize="10485760"
            archiveFileName="${basedir}/logs/${shortdate}.all.{#}.log"
            archiveNumbering="Sequence"
            maxArchiveFiles="100" />
    <target xsi:type="File" name="errorfile" fileName="${basedir}/logs/${shortdate}.error.log"
            layout="[${time}] [${level:uppercase=true}] [${logger}] ${exception:format=tostring}"
            archiveAboveSize="10485760"
            archiveFileName="${basedir}/logs/${shortdate}.error.{#}.log"
            archiveNumbering="Sequence"
            maxArchiveFiles="100" />
  </targets>
  <rules>
    <logger name="*" minlevel="Debug" writeTo="logfile" />
    <logger name="*" minlevel="Error" writeTo="errorfile" />
  </rules>
</nlog>
```

