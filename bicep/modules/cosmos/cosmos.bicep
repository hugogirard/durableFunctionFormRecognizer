param location string
param suffix string

var cosmosAccountName = 'cosmos-${suffix}'
var databaseName = 'db-log'
var containerName = 'logfiles'

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2021-06-15' = {
  name: cosmosAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Eventual'
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
  }
}

resource cosmosdb 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2021-06-15' = {
  parent: cosmos
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

// resource container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2021-06-15' = {
//   parent: cosmosdb
//   name: containerName
//   properties: {
//     resource: {
//       id: containerName
//       partitionKey: {
//         paths: '/id'
//         kind: 'Hash'
//       }
//     }
//     options: {
//       throughput: 400
//     }
//   }
// }
