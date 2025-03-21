# DESAFIO-BT---INOA

Esse repositório contém um programa de console que checa continuamente o preço de um determinado ativo, e envia um email caso o preço do ativo ultrapasse determinados triggers. 

O arquivo JSON de configurações que contém os endereços de email é ConsoleApp1/bin/Debug/net8.0/appsettings.json

Contém um executável em linux em ConsoleApp1/bin/Debug/net8.0/ConsoleApp1

Contém um .exe em ConsoleApp1/bin/Release/net8.0/win-x64/publish/ConsoleApp1.exe

Rodar ele no console e passar os argumentos <ATIVO> <PREÇO-DE-COMPRA> <PREÇO-DE-VENDA>. Caso não informe os dois últimos, ele gera valores de referência baseados no preço inicial ao rodar o programa. Ex:

ConsoleApp1.exe PETR4 35.54 38
./ConsoleApp1 PETR4

