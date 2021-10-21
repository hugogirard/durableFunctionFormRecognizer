- [About this sample](#about-this-sample)
- [Architecture](#architecture)
- [Azure Resources deployed in this sample](azure-resources-deployed-in-this-sample)
  - [Seeder](#seeder)
  - [Storage Document](storage-(document))
  - [Storage Functions](storage-(functions))
  - [Train Model Function](train-model-function)
  - [Form Recognizer](form-recognizer)
  - [Durable Function Processor](durable-function-processor)
    - [Durable Function Flow](durable-function-flow)
  - [Blazor Server Viewer](blazor-server-viewer)
- [Installation](installation)

# About this sample

The goal of this sample it's to illustrate a Cloud Pattern to process multiple documents saved in an Azure Storage to Form Recognizer.

# Architecture

![architecture](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/architecture.png)

Here in more details each part of this architecture.

1 - The seeder sends X document with the tag status with the value unprocessed to the container document in Azure Storage.

2a - The train model function is getting a SAS from the model container.

2b - The function is sending the path of the container and all metadata to form recognizer.

3c - Form recognizer retrieve all the files that are needed to create the custom model and train the model.

3a - The processor function start retrieving documents (blob) from the storage.

3b - The processor function is sending the documents to be analyzed to form recognizer.

4 - The blazor viewer app retrieves the log from the table storage

5 - If needed, the blazor viewer app can terminate any processor function app

# Azure Resources deployed in this sample

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

This Azure function will train the [Custom Model](https://docs.microsoft.com/en-us/azure/applied-ai-services/form-recognizer/concept-custom) in Form Recognizer.  Four endpoints are provided in this Azure Function.

| Endpoint | HTTP Verb |Description
|----------|------------|----------
| /api/TrainCustomModel | POST | This endpoint needs to be called to train the custom model in Form Recognizer.
| /api/GetCustomModel | GET | This endpoint return the definition of a specific custom model
| /api/GetCustomModels | GET | This endpoint return all custom models trained in Form Recognizer
| /api/DeleteCustomModel | DELETE | This endpoint delete a specific custom trained model.

## Form Recognizer

Azure Applied AI Services, uses machine learning technology to identify and extract key-value pairs and table data from form documents.  For more information about Form Recognizer click [here](https://docs.microsoft.com/en-us/azure/applied-ai-services/form-recognizer/).

## Durable Function Processor

This function app start two [eternal durable functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-eternal-orchestrations?tabs=csharp)

![function](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/flow.png)

### Durable Function Flow

TBD

## Blazor Server Viewer

This Blazor application show all the processor function progress. From there you can follow each instance of the processor function, terminate the function and see any exception and metric related to the processing flow.

## Monitoring

All Azure functions log their metric in Application Insight and Log Analytics.

# Installation

This section describe all the steps you need to install this sample in your Azure environment.

## Step 1 - Fork the Github Repository

Click the button in the top right corner to Fork the git repository.

![function](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/fork.png)

## Step 2 - Create a Service Principal needed for the Github Action

All the creation of the Azure resources and deployment of all applications is done in this sample using a Github Action.  You will need to create a service principal that will be used to deploy everything.

To do this, please follow this [link](https://github.com/marketplace/actions/azure-login).  Be sure to save  the output generated by the command line.  You will need it after to create a **Github Secret**.

The ouput will look like this below.  Copy paste it in your clipboard.

![sp](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/spoutput.png)



