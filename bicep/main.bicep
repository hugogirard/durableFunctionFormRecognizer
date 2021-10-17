/*
* Notice: Any links, references, or attachments that contain sample scripts, code, or commands comes with the following notification.
*
* This Sample Code is provided for the purpose of illustration only and is not intended to be used in a production environment.
* THIS SAMPLE CODE AND ANY RELATED INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED,
* INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
*
* We grant You a nonexclusive, royalty-free right to use and modify the Sample Code and to reproduce and distribute the object code form of the Sample Code,
* provided that You agree:
*
* (i) to not use Our name, logo, or trademarks to market Your software product in which the Sample Code is embedded;
* (ii) to include a valid copyright notice on Your software product in which the Sample Code is embedded; and
* (iii) to indemnify, hold harmless, and defend Us and Our suppliers from and against any claims or lawsuits,
* including attorneys’ fees, that arise or result from the use or distribution of the Sample Code.
*
* Please note: None of the conditions outlined in the disclaimer above will superseded the terms and conditions contained within the Premier Customer Services Description.
*
* DEMO POC - "AS IS"
*/

param location string
param addressSpaceSubnet string

@description('this is the address space')
param addressSpaceVnet string

@secure()
param adminUsername string

@secure()
param adminPassword string

var suffix = uniqueString(resourceGroup().id)

// module cognitives 'modules/cognitives/form.bicep' = {
//   name: 'cognitives'
//   params: {
//     location: location
//     suffix: suffix
//   }
// }

// module insight 'modules/insights/insights.bicep' = {
//   name: 'insight'
//   params: {
//     location: location
//     suffix: suffix    
//   }
// }

// module storage 'modules/storage/storage.bicep' = {
//   name: 'storage'
//   params: {
//     location: location
//     suffix: suffix
//   }
// }

// module vnet 'modules/network/vnet.bicep' = {
//   name: 'vnet'
//   params: {
//     location: location
//     addressSpaceVnet: addressSpaceVnet
//     addressSpaceSubnet: addressSpaceSubnet
//   }
// }

// module compute 'modules/compute/windows.bicep' = {
//   name: 'compute'
//   params: {
//     location: location
//     suffix: suffix
//     subnetId: vnet.outputs.subnetId
//     adminUsername: adminUsername
//     adminPassword: adminPassword
//   }
// }

module cosmos 'modules/cosmos/cosmos.bicep' = {
  name: 'cosmos'
  params: {
    location: location
    suffix: suffix
  }
}

// module appServicePlan 'modules/functions/appPlan.bicep' = {
//   name: 'appServicePlan'
//   params: {
//     location: location
//     suffix: suffix
//   }
// }

// module functionProcessor 'modules/functions/processor.bicep' = {
//   name: 'functionProcessor'
//   params: {
//     location: location
//     suffix: suffix
//     appInsightCnxString: insight.outputs.appInsightCnxString
//     appInsightKey: insight.outputs.appInsightKey    
//     strAccountApiVersion: storage.outputs.strFunctionApiVersion
//     strAccountId: storage.outputs.strFunctionId
//     strAccountName: storage.outputs.strFunctionName  
//     serverFarmId: appServicePlan.outputs.serverFarmId
//     formRecognizerEndpoint: cognitives.outputs.frmEndpoint
//     formRecognizerKey: cognitives.outputs.frmKey
//     strDocumentName: storage.outputs.strDocumentName
//     strDocumentId: storage.outputs.strDocumentId
//     strDocumentApiVersion: storage.outputs.strDocumentApiVersion
//     cosmosEndpoint: cosmos.outputs.cosmosEndpoint
//     cosmosKey: cosmos.outputs.cosmosKey
//     cosmosDatabaseName: cosmos.outputs.databaseNameOutput    
//   }
// }

// module functionModel 'modules/functions/model.bicep' = {
//   name: 'functionModel'
//   params: {
//     location: location
//     suffix: suffix
//     appInsightCnxString: insight.outputs.appInsightCnxString
//     appInsightKey: insight.outputs.appInsightKey    
//     strAccountApiVersion: storage.outputs.strFunctionApiVersion
//     strAccountId: storage.outputs.strFunctionId
//     strAccountName: storage.outputs.strFunctionName    
//     serverFarmId: appServicePlan.outputs.serverFarmId  
//     strModelId: storage.outputs.strDocumentId
//     strModelName: storage.outputs.strDocumentName
//     strModelApiVersion: storage.outputs.strDocumentApiVersion
//     formRecognizerEndpoint: cognitives.outputs.frmEndpoint
//     formRecognizerKey: cognitives.outputs.frmKey
//   }
// }

// module blazorApp 'modules/web/blazor.bicep' = {
//   name: 'blazorApp'
//   params: {
//     location: location
//     suffix: suffix
//   }
// }


// output vmName string = compute.outputs.vmName
// output functionProcessorName string = functionProcessor.outputs.functionName
// output functionModelName string = functionModel.outputs.functionName
// output webAppName string = blazorApp.outputs.appName
