# Тестовое задание от Umbrella

[Описание задачи](task.md)

## Сборка проекта

Выкачиваем репозиторий в удобное место.
```
root@ubnt-virt:~# cd /opt/git
root@ubnt-virt:/opt/git# git clone https://github.com/google1993/UmbrellaTestTask.git
```
Переходим в папку проекта
```
root@ubnt-virt:/opt/git# cd ./UmbrellaTestTask/
root@ubnt-virt:/opt/git/UmbrellaTestTask# 
```
Проект собираем командами:
```
root@ubnt-virt:/opt/git/UmbrellaTestTask# cd ./Client
root@ubnt-virt:/opt/git/UmbrellaTestTask/Client# dotnet publish -r linux-x64 -c Release -p:PublishAot=true -p:SelfContained=true -p:PublishSingleFile=true -o /opt/Client
root@ubnt-virt:/opt/git/UmbrellaTestTask/Client# cd ../Server
root@ubnt-virt:/opt/git/UmbrellaTestTask/Server# dotnet publish -r linux-x64 -c Release -p:PublishAot=true -p:SelfContained=true -p:PublishSingleFile=true -o /opt/Server
```
Можно не указывать параметры, так как они уже установлены в файле проекта.

В результате сборки будут 2 папки:
```
root@ubnt-virt:/opt# tree ./
./
├── Client
│   ├── Client
│   ├── Client.dbg
│   ├── config_client.json
│   └── data.json
└── Server
    ├── config_server.json
    ├── Server
    └── Server.dbg
```

## Запуск

Необходим `openssl`
Переходим в папку сервера и создаем ключ и сертификат
```
cd /opt/Server
openssl genpkey -algorithm RSA -out private_key.pem -pkeyopt rsa_keygen_bits:2048
openssl req -new -x509 -key private_key.pem -out certificate.pem -days 365
```

Указываем их в файле конфигурации сервера
```
{
  "ListenAddr": "0.0.0.0",
  "ListenPort": 5555,
  "TlsEnabled": true,
  "TlsCrt": "/opt/Server/certificate.pem",
  "TlsKey": "/opt/Server/private_key.pem",
  "Filters": [
    {
      "field": "type",
      "operator": "equals",
      "value": "event"
    },
    {
      "field": "level",
      "operator": "not_equals",
      "value": "debug"
    }
  ]
}

```
Копируем `tcp-tls-server.service` в `/etc/systemd/system/`

Обновляем конфигурацию служб и запускаем сервис
```
systemctl daemon-reload
systemctl start tcp-tls-server
systemctl status tcp-tls-server

● tcp-tls-server.service - TCP/TLS Server
     Loaded: loaded (/etc/systemd/system/tcp-tls-server.service; disabled; preset: enabled)
     Active: active (running) since Fri 2025-08-08 02:41:49 +05; 5s ago
   Main PID: 125689 (Server)
      Tasks: 4 (limit: 9377)
     Memory: 2.9M (peak: 3.2M)
        CPU: 10ms
     CGroup: /system.slice/tcp-tls-server.service
             └─125689 /opt/Server/Server ./config_server.json

авг 08 02:41:49 ubnt-virt systemd[1]: Started tcp-tls-server.service - TCP/TLS Server.
авг 08 02:41:49 ubnt-virt TCPTLSServer[125689]: Listening on 0.0.0.0:5555
```
Провеим порт
```
netstat -tupln

Активные соединения с интернетом (only servers)
Proto Recv-Q Send-Q Local Address Foreign Address State       PID/Program name    
tcp        0      0 0.0.0.0:5555            0.0.0.0:*               LISTEN      125689/Server       
```

Переходим в папку с клиентом и вызываем команду для проверки.
```
cd /opt/Client/
./Client ./config_client.json 

Invalid JSON: not a json line
Invalid JSON: {"type":"log","message":"Missing field 'level'}
Invalid JSON: 1234
```

Смотрим логи сервера
```
root@ubnt-virt:/opt/Client# journalctl -u tcp-tls-server.service -n 20
авг 08 02:41:49 ubnt-virt systemd[1]: Stopping tcp-tls-server.service - TCP/TLS Server...
авг 08 02:41:49 ubnt-virt systemd[1]: tcp-tls-server.service: Deactivated successfully.
авг 08 02:41:49 ubnt-virt systemd[1]: Stopped tcp-tls-server.service - TCP/TLS Server.
авг 08 02:41:49 ubnt-virt systemd[1]: Started tcp-tls-server.service - TCP/TLS Server.
авг 08 02:41:49 ubnt-virt TCPTLSServer[125689]: Listening on 0.0.0.0:5555
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]: {
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "type": "event",
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "level": "info",
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "message": "Service started"
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]: }
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]: {
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "type": "event",
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "level": "error",
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "message": "Something went wrong"
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]: }
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]: {
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "type": "event",
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "level": "info",
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]:   "message": "All systems operational"
авг 08 02:47:24 ubnt-virt TCPTLSServer[125689]: }
```

Всё, вроде, работает.

## Пишите, если требуются доработки.