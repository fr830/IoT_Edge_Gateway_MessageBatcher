# IoT Edge Gateway MessageBatcher Module
Sample module for batching up OPC UA messages into an IoT Hub message. This module can be used to optimize usage of IoT Hub. For example, OPC UA messages tend to be around 200 bytes. So we can stuff up to 20 messages into one (1) 4KB IoT Hub message. 

Keep in mind IoT Hub messages can be a max of 256KB but billing is based on each message <= 4KB or 4096 bytes. The larger the MaxSizeOfIoTHubMessageBytes, the less 'real time' the data as it will take longer to fill the list before sending it. If your tags update infrequently, using MessageBatcher is probably a bad idea. 

Instructions for building modules can be found at https://github.com/Azure/iot-edge under **Creating Modules using Packages**. 

Here's an example of the gateway config, which shows the OpcUa module sending messages to MessageBatcher, which in turn batches up the messages and send a single message to IoT Hub. 

You will have to unbatch the JSON messages after IoTHub using Azure Stream Analytics or Azure Functions, for example. 

```
{
  "modules": [
    {
      "name": "OpcUa",
      "loader": {
        "name": "dotnetcore",
        "entrypoint": {
          "assembly.name": "Opc.Ua.Publisher.Module",
          "entry.type": "Opc.Ua.Publisher.Module"
        }
      },
      "args": {
        "Configuration": {
          "ApplicationName": "<ReplaceWithYourApplicationName>",
          "ApplicationType": "ClientAndServer",
          "ApplicationUri": "urn:localhost:microsoft:publisher"
        }
      }
    },
    {
      "name": "IoTHub",
      "loader": {
        "name": "native",
        "entrypoint": {
          "module.path": "iothub.dll"
        }
      },
      "args": {
        "IoTHubName": "<ReplaceWithYourIoTHubName>",
        "IoTHubSuffix": "azure-devices.net",
        "Transport": "AMQP"
      }
    },
    {
      "name": "MessageBatcher",
      "loader": {
        "name": "dotnetcore",
        "entrypoint": {
          "assembly.name": "MessageBatcher",
          "entry.type": "MessageBatcher.MessageBatcherModule"
        }
      },
      "args": {
        "SomeArg1": "",
        "SomeArg2": "",
        "SomeArg3": ""
      }
    }
  ],
  "links": [
    {
      "source": "OpcUa",
      "sink": "MessageBatcher"
    },
    {
      "source": "MessageBatcher",
      "sink": "IoTHub"
    }
  ]
}
```
