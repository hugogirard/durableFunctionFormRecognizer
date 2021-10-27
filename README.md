- [About this sample](#about-this-sample)
- [Architecture](#architecture)
- [Azure Resources deployed in this sample](azure-resources-deployed-in-this-sample)
  - [Seeder](#seeder)
  - [Storage Document](storage-(document))
  - [Storage Functions](storage-(functions))
  - [Train Model Function](train-model-function)
  - [Form Recognizer](form-recognizer)
  - [Durable Function Processor](#durable-function-processor)
  - [Blazor Server Viewer](blazor-server-viewer)
- [Installation](installation)
  - [Step 1 - Github Repository](step-1-fork-the-github-repository)
  - [Step 2 - Create a Service Principal](step-2-create-a-service-principal-needed-for-the-github-action)
  - [Step 3 - Create Github Secrets](step-3-create-needed-github-secrets)
  - [Step 4 - Step 4 - Run the Github Action](step-4-run-the-github-action)
  - [Step 5 - Train the model](step-5-train-the-model)

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

This function app uses two types of [eternal durable functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-eternal-orchestrations?tabs=csharp) (Collector and Processor) and a [durable entity](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-entities) (BlobInfoEntity).

### Collector

#### Flow chart:

![Collector flow chart](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Collector-Flowchart.png)

#### Sequence digram:

![Collector sequence diagram](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Collector-Sequence.png)

## Processor

#### Flow chart:

![Processor flow chart](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Processor-Flowchart.png)

#### Sequence digram:

![Processor sequence diagram](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Processor-Sequence.png)

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


## Step 3 - Create needed Github Secrets

For the Github Action to run properly you need to create 3 secrets.

First go to the Settings tab.

![settings](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/settings.png)

Next click the **Secrets** button in the left menu.

![leftmenu](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/leftmenu.png)

You will need to create three secrets.

![secrets](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/secrets.png)

| Secret Name | Description 
|-------------|------------
| ADMIN_USERNAME | This is the username needed to login in the Seeder VM
| ADMIN_PASSWORD | This is the secret needed to login in the Seeder VM
| AZURE_CREDENTIALS | This is the value returned when creating the Service Principal from the step before

## Step 4 - Run the Github Action

Now it is time to deploy and create all resources in Azure.  To do this, go to the Github Action tab.

![action](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/actionmenu.png)

Because you **forked** this git you won't see by default the Github action, you will need to enable it.

![action](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/enablegh.png)

Once the Github Action is enabled, you will see on action in the left menu called **Deploy**.  Select it and in the right you will see a drop-down list with the action Run workflow.  

Click on it, you can leave the default parameters in place or change them if you want.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/workflow.png)

| Parameter | Description
|-----------|------------
| Name of the resource group | This is the name of the resource group created in Azure that will contain all the resources.
| Location | This is where all resources will be created
| CIDR VNET | This is the CIDR address of the VNET that will contain the Seeder VM
| CIDR Subnet | This is the CIDR that will contain the VM

When you are ready, click the green button **Run workflow**

Now you can follow the Github Action running by clicking the execution.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/execution.png)

This should take some time, once everything finish to run proceed to next step.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/stepsgh.png)

## Step 5 - Train the model

Now that everything is deployed correctly you need to train the model in Form Recognizer.

Clone the git repository on your machine, you will see a **folder** called **model**.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/stepsgh.png)

Now, go to the Azure Portal with the in the new resource group that contains all the resource.

You will see two Azure Storage there, you need to go to the one that have a **tag description** with the value **Document Storage**.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/tag.png)

Go to the left menu and click on Containers

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/containers.png)

Click on the **models** container and upload all the document from the git you cloned from the model folder.  **Don't upload the file from the subfolder empty**.

Your storage should contain those files.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/files.png)

Go back to the resource group in Azure, you will see two function, one starting with fnc-model-trainer and the other called fnc-processor.  **Click** on the function **fnc-model-trainer**.

Now click on the Functions item to the left menu.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/func.png)

Click on the **TrainCustomModel**.

Click in the left menu to **Code + Test**

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/codetest.png)

Now click in the menu at the top **Test/Run** button.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/testrun.png)

In the new window that appear to the left you will see **Query** and a button **+ Add header**, click on it.

From there you need to provide in the field name the value **modelName** and in the field value you can enter **JobModel**.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/postparameters.png)

Now click on the blue button **Run**.

Is important you take note of the modelId returned after the Azure function finished to been executed.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/modelid.png)

If you forget to take note of the modelId you can always retrieve it using the other endpoint of the Azure Function (see section above).

## Step 6 - Run the Seeder App

Now, you need to generate some documents so the processor function can send them to Form Recognizer.  To do so, you will need to connect to the Azure Virtual Machine called **seeder** in your **resource group**.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/seeder.png)

By default you cannot connect to the Virtual Machine, the port 3389 is not open and protected by the [Network Security Group](https://docs.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview).

If you are using Azure Security Defender you can activate the [Just-in-Time access](https://docs.microsoft.com/en-us/azure/security-center/security-center-just-in-time?tabs=jit-config-asc%2Cjit-request-asc).  Be aware, you need to have Azure Security Defender Standard and some cost are associated to this. 

The other option is to modify the NSG called **nsg-seeder**.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/nsg-seeder.png)

You can add a new inbound security rules to give access to the port 3389 but **ONLY** for your **IP address**.  You don't want to open 3389 to Internet, we recommend the Just-In-Time access this is more secure.

If you want to modify the NSG rule it will look like something like this.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/inbound.png)

One you activated the JIT or modified the NSG you can connect to your virtual machine, using the credentials you created in the **GITHUB Secrets**.

Once you are in the virtual machine, open File Explorer and go to the path **C:\git\durableFunctionFormRecognizer\src\consoleSeeder\SeederApp**

You will see a file called **appsettings.json**, open it with Notepad.

You need to add the value for **storageAccountName**, this is the name of your storage account that contains the following tag.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/strtag.png)

In this case the value of **storageAccountName** will be **strdap3y4htuhu44u**.

The next value you need to add is **storageCnxString**, this is the connection string of the storage found before.  To get the connection string you need to click on Access keys in the storage.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/accesskey.png)

From there, you can click the Show keys button at the top and copy your connection string in the appsettings.json file.

The other value that is importat is the **nbrDocuments**, this is the number of document you want to add in the storage.  To be sure to not have a big surprise if you add multiples documents refer to the [Azure Pricing for Form Recognizer](https://azure.microsoft.com/en-us/pricing/details/form-recognizer/).  In our case all documents created contains only one page.

To calculate the pricing you need to use the document type Custom, so if we take this example (pricing change often this picture can be different than your actual pricing).


![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/frmpricing.png)

If you enter the value **1000** for the nbrDocuments because the pricing is 50$ USD for 1000 pages and all documents are 1 page this will cost you around 50 USD.




