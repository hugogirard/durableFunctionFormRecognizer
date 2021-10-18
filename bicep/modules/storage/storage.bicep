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
param suffix string

var strAcccountNameFunc = 'strf${suffix}'
var strAccountNameDoc = 'strd${suffix}'

resource storageAccountFunction 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: strAcccountNameFunc
  location: location
  sku: {
    name: 'Standard_LRS'    
  }
  tags: {
    'description': 'Function Storage'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource storageAccountDocument 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: strAccountNameDoc
  location: location
  sku: {
    name: 'Standard_LRS'    
  }
  tags: {
    'description': 'Document Storage'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    encryption: {
      services: {
        file: {
          keyType: 'Account'
          enabled: true
        }
        blob: {
          keyType: 'Account'
          enabled: true
        }
      }
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

//resource table 'Microsoft.Storage/storageAccounts/tableServices@2021-06-01' = {
//  name: '${storageAccountDocument.name}/Documents'  
//}

resource containerDocuments 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = {
  name: '${storageAccountDocument.name}/default/documents'
  properties: {
    publicAccess: 'None'
  }
}

resource containerModel 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = {
  name: '${storageAccountDocument.name}/default/models'
  properties: {
    publicAccess: 'None'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2021-04-01' = {
  name: '${storageAccountDocument.name}/default'
  properties: {
    cors: {
      corsRules: [
        {
          allowedOrigins: [
            '*'
          ]
          allowedMethods: [
            'DELETE'
            'GET'
            'HEAD'
            'MERGE'
            'POST'
            'OPTIONS'
            'PUT'
            'PATCH'
          ]
          exposedHeaders: [
            '*'
          ]
          allowedHeaders: [
            '*'
          ]
          maxAgeInSeconds: 200
        }
      ]
    }
  }
}

output strFunctionName string = storageAccountFunction.name
output strFunctionId string = storageAccountFunction.id
output strFunctionApiVersion string = storageAccountFunction.apiVersion

output strDocumentName string = storageAccountDocument.name
output strDocumentId string = storageAccountDocument.id
output strDocumentApiVersion string = storageAccountDocument.apiVersion

output tableName string = 'test'

//output tableName string = table.name
