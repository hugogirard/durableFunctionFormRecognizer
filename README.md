- [About this sample](#about-this-sample)
- [Architecture](#architecture)
  - [Seeder](#seeder)
  - [Storage Document](storage-(document))
  - [Storage Functions](storage-(functions))

# About this sample

The goal of this sample it's to illustrate a Cloud Pattern to process multiple documents saved in an Azure Storage to Form Recognizer.

# Architecture

![architecture](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/durableFunction.png)

Here in more details each part of this architecture.

## Seeder

The seeder is a VM where a console app (C#) is installed.  The goal of this application is to create documents (blob) in an Azure Storage.

Each document will be marked with a **tag status** with the value **unprocessed**.

We are leveraging the [indexing](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-index-how-to?tabs=azure-portal) feature of Azure Storage to retrieve the document in the storage that need to be processed.

The seeder app is leveraging multithreading in C#, one thread will create X number of documents adding the specific tag in the Azure storage.

## Storage (Document)

This storage contains all the document that need to be processed and analyze by Form Recognizer.  

It contains the model to train too and all logging from the Durable Processor Function.  The logs are saved in a Table Storage.

## Storage (Functions)

This storage is used by all Azure Functions in the architecture for their internal functionalities.

## Train Model Function

This Azure function will train the Custom Model in Form Recognizer.  Four endpoints are provided in this Azure Function.

| Endpoint | HTTP Verb |Description
|----------|------------|----------
| /api/TrainCustomModel | POST | This endpoint needs to be called to train the custom model in Form Recognizer.
| /api/GetCustomModel | GET | This endpoint return the definition of a specific custom model
| /api/GetCustomModels | GET | This endpoint return all custom models trained in Form Recognizer
| /api/DeleteCustomModel | DELETE | This endpoint delete a specific custom trained model.