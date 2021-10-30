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
  - [Step 6 - Run the Seeder App](step-6-run-the-seeder-app)

# About this sample

The goal of this sample is to illustrate a Cloud pattern to process multiple documents saved in an Azure Storage with Form Recognizer.

# Architecture

![architecture](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/architecture.png)

Here’s more details for the different parts of this architecture.

1 - The seeder sends X documents with the tag status with the value unprocessed to the container document in Azure Storage.

2a - The train model function gets a SAS from the model container.

2b - The function sends the path of the container and all metadata to Form Recognizer.

3c - Form Recognizer retrieves all the files needed to create the custom model and train the model.

3a - The processor function starts retrieving documents (blobs) from the storage.

3b - The processor function sends the documents to be analyzed to Form Recognizer.

4 - The Blazor viewer app retrieves the status from the processor function.

5 - If needed, the Blazor viewer app can restart or terminate any processor function app or start/stop the whole process.

# Azure Resources deployed in this sample

## Seeder

The seeder is a VM where a console app (C#) is installed.  The goal of this application is to create documents (blobs) in an Azure Storage.

Each document will be marked with a tag **status** with the value **unprocessed**.

We are leveraging the [indexing](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-blob-index-how-to?tabs=azure-portal) feature of Azure Storage to retrieve documents in storage that need to be processed.

The seeder app is leveraging multithreading in C#, one thread will create X number of documents adding the specific tag in the Azure storage.

## Storage (Document)

This storage contains all the documents that need to be processed and analyzed by Form Recognizer. 

It also contains the model to train and the output from the Durable Processor Function.  The data is saved in a Table Storage.

## Storage (Functions)

This storage is used by all Azure Functions in the architecture for their internal working.

## Train Model Function

This Azure function will train the [Custom Model](https://docs.microsoft.com/en-us/azure/applied-ai-services/form-recognizer/concept-custom) in Form Recognizer.  Four endpoints are provided in this Azure Function.

| Endpoint | HTTP Verb |Description
|----------|------------|----------
| /api/TrainCustomModel | POST | This endpoint needs to be called to train the custom model in Form Recognizer.
| /api/GetCustomModel | GET | This endpoint returns the definition of a specific custom model
| /api/GetCustomModels | GET | This endpoint returns all custom models trained in Form Recognizer
| /api/DeleteCustomModel | DELETE | This endpoint deletes a specific custom trained model.

## Form Recognizer

Azure Applied AI Services, uses machine learning technology to identify and extract key-value pairs and table data from form documents.  For more information about Form Recognizer click [here](https://docs.microsoft.com/en-us/azure/applied-ai-services/form-recognizer/).

## Durable Function Processor

This function app uses two types of [eternal durable functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-eternal-orchestrations?tabs=csharp) (Collector and Processor) and a [durable entity](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-entities) (BlobInfoEntity).

### Collector

The Collector is a single-instance eternal function which is responsible of fetching unprocessed blobs. The goal is to maintain a sufficient backlog, so the Processors never run out of blobs tor process. It runs every 10 seconds by default.

#### Flow chart:

![Collector flow chart](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Collector-Flowchart.png)

#### Sequence digram:

![Collector sequence diagram](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Collector-Sequence.png)

## Processor

The Processor consists of multiple instances or an eternal function. It's responsible for sending the blobs to Form Recognizer for processing, saving the results and updating the blobs states.

Each instance has its own partition (or slice) of blobs to process. For example, if batch size is 1000 and there's 10 instances, every instance will process 100 blobs per execution.

It starts by sending the whole partition to Form Recognizer and before starting to query for results. This prevents wasting transactions (which count towards the TPS limit) to query results before Form Recognizer has completed its processing.

The results are saved to table storage, including the form fields and the OCR of the document. In case of a failure, the exception information is also included. If the call is throttled by Form Recognizer (transient failure), the processor will automatically retry.

#### Flow chart:

![Processor flow chart](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Processor-Flowchart.png)

#### Sequence digram:

![Processor sequence diagram](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/src/processor/Diagrams/Processor-Sequence.png)

## Blazor Server Viewer

This Blazor application shows all the processor function progress. From there you can follow each instance of the processor function, terminate the function and see any exceptions or metrics related to the processing flow.

## Monitoring

All Azure functions log their metric in Application Insight and Log Analytics.

# Installation

This section describes all the steps you need to install this sample in your Azure environment.

## Step 1 - Fork the GitHub Repository

Click the button in the top right corner to Fork the git repository.

![function](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/fork.png)

## Step 2 - Create a Service Principal for the GitHub Action

All the creation of the Azure resources and deployment of all applications is done in this sample using a GitHub Action.  You will need to create a service principal that will be used to deploy everything.

To achieve this, please follow this [link](https://github.com/marketplace/actions/azure-login).  Be sure to save the output generated by the command line.  You will need it after to create a **GitHub Secret**.

The output will look like below.  Copy it in your clipboard.

![sp](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/spoutput.png)


## Step 3 - Create needed GitHub Secrets

For the GitHub Action to run properly you need to create 3 secrets.

First go to the Settings tab.

![settings](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/settings.png)

Next click the **Secrets** button in the left menu.

![leftmenu](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/leftmenu.png)

You will need to create three secrets.

![secrets](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/secrets.png)

| Secret Name | Description 
|-------------|------------
| ADMIN_USERNAME | This is the username to login in the Seeder VM
| ADMIN_PASSWORD | This is the secret to login in the Seeder VM
| AZURE_CREDENTIALS | This is the value returned when creating the Service Principal from the previous step

## Step 4 - Run the GitHub Action

Now it is time to deploy and create all resources in Azure.  To do this, go to the GitHub Action tab.

![action](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/actionmenu.png)

Because you **forked** this repo, you won't see by default the GitHub action, you will need to enable it.

![action](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/enablegh.png)

Once the GitHub Action is enabled, you will see on action in the left menu called **Deploy**.  Select it and, on the right, you will see a drop-down list with the action Run workflow.  

Click on it, you can leave the default parameters in place or change them if you want.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/workflow.png)

| Parameter | Description
|-----------|------------
| Name of the resource group | This is the name of the resource group created in Azure that will contain all the resources.
| Location | This is where all resources will be created
| CIDR VNET | This is the CIDR address of the VNET that will contain the Seeder VM
| CIDR Subnet | This is the CIDR that will contain the VM

When you are ready, click the green button **Run workflow**

Now you can follow the GitHub Action running by clicking the execution.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/execution.png)

This should take some time, once everything finishes to run, proceed to next step.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/stepsgh.png)

## Step 5 - Train the model

Now that everything is deployed correctly you need to train the model in Form Recognizer.

Clone the git repository on your machine, you will see a **folder** called **model**.

![workflow](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/stepsgh.png)

Now, go to the Azure Portal in the new resource group that contains all the resources.

You will see two Azure Storage accounts, you need to go to the one that have a tag **description** with the value **Document Storage**.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/tag.png)

Go to the left menu and click on Containers

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/containers.png)

Click on the **models** container and upload all the documents from the model folder in the repository you cloned.  **Don't upload the files from the subfolder empty**.

Your storage should contain the following files.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/files.png)

Go back to the resource group in Azure, you will see two functions, one starting with fnc-model-trainer and the other called fnc-processor.  **Click** on the function **fnc-model-trainer**.

Now click on the Functions item on the left menu.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/func.png)

Click on the **TrainCustomModel** function.

Click in the left menu to **Code + Test**

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/codetest.png)

Now click on the top **Test/Run** button on the top menu.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/testrun.png)

In the new window that appears to the left, you will see **Query** and a button **+ Add header**, click on it.

From there, you need to enter **modelName** as the Name and **JobModel** as the Value.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/postparameters.png)

Now click on the blue button **Run**.

It’s important you note the modelId returned after the Azure Function finishes.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/modelid.png)

If you forget to note the modelId you can always retrieve it using the other endpoint of the Azure Function (see section above).

## Step 6 - Run the Seeder App

Now, you need to generate some documents so the processor function can send them to Form Recognizer.  To do so, you will need to connect to the Azure Virtual Machine called **seeder** in your **resource group**.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/seeder.png)

By default, you cannot connect to the Virtual Machine, the port 3389 is not open and protected by the [Network Security Group](https://docs.microsoft.com/en-us/azure/virtual-network/network-security-groups-overview).

If you are using Azure Security Defender you can activate the [Just-in-Time access](https://docs.microsoft.com/en-us/azure/security-center/security-center-just-in-time?tabs=jit-config-asc%2Cjit-request-asc).  Be aware, you need to have Azure Security Defender Standard and there’s cost is associated with it. 

The other option is to modify the NSG called **nsg-seeder**.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/nsg-seeder.png)

You can add a new inbound security rule to grant access to the port 3389 but **ONLY** for your **IP address**.  You don't want to open 3389 to Internet, we recommend the Just-In-Time access this is more secure.

If you want to modify the NSG rule, it will look like the following.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/inbound.png)

One you activated the JIT or modified the NSG, you can connect to your virtual machine, using the credentials you created in the **GitHub Secrets**.

Once you are in the virtual machine, open File Explorer and go to the path **C:\git\durableFunctionFormRecognizer\src\consoleSeeder\SeederApp**

You will see a file called **appsettings.json**, open it with Notepad.

You need to add the value for **storageAccountName**. This is the name of your storage account that contains the following tag.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/strtag.png)

In this case, the value of **storageAccountName** will be **strdap3y4htuhu44u**.

The next value you need to add is **storageCnxString**. This is the connection string of the storage found before.  To get the connection string you need to click on Access keys in the storage.

![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/accesskey.png)

From there, you can click the Show keys button at the top and copy your connection string in the appsettings.json file.

The other value that is important is the **nbrDocuments**, this is the number of document you want to add in the storage.  To prevent any surprises, if you add multiples documents refer to the [Azure Pricing for Form Recognizer](https://azure.microsoft.com/en-us/pricing/details/form-recognizer/).  In our case, all documents created contains only one page.

To calculate the pricing, you need to use the document type Custom, so if we take this example (pricing change often, this picture can be different than your actual pricing).


![tag](https://raw.githubusercontent.com/hugogirard/durableFunctionFormRecognizer/main/images/frmpricing.png)

If you enter the value **1000** for the nbrDocuments because the pricing is 50$ USD for 1000 pages and all documents are 1 page this will cost you around 50 USD.

Now in the virtual machine, open a Command Prompt and do this command

```
cd c:\git\durableFunctionFormRecognizer\src\consoleSeeder\SeederApp
```

Now you are ready to run the seeder, to do this execute this command.

```

```


