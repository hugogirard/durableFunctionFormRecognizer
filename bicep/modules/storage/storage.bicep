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
