# ZebraPrint

ASP.Net Core server application that serves as the interface between ZLAB and Zebra RFID tag printers. 
- It uses the Link-OS SDK to establish a connection with the printer and obtain current printer status.
- Receives HTTP requests to create and queue print jobs.
- Sends print preview image data to ZLAB.
